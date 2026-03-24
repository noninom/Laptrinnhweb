using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Laptrinnhweb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DatBansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatBansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Xem danh sách khách đặt bàn
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var danhSachDat = await _context.DatBans
                .Include(d => d.BanAn)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            return View(danhSachDat);
        }

        // 2. Trang nhập thông tin khách khi bắt đầu đặt

        // Sửa Action GET Create
        public async Task<IActionResult> Create(int tableId, string cartJson)
        {
            var ban = await _context.BanAns.FindAsync(tableId);
            if (ban == null) return NotFound();

            ViewBag.TableId = tableId;
            ViewBag.TenBan = ban.SoBan;
            ViewBag.CartJson = cartJson; // Gửi chuỗi món ăn sang View nhập thông tin khách
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DatBan datBan, string cartJson)
        {
            if (ModelState.IsValid)
            {
                // BƯỚC 1: Thiết lập trạng thái là 0 (Đợi nhận bàn) thay vì 1
                datBan.TrangThai = 0;
                datBan.NgayDat = DateTime.Now;
                _context.Add(datBan);
                await _context.SaveChangesAsync();

                // BƯỚC 2: Cập nhật trạng thái Bàn ăn thành 2 (Tương ứng với màu vàng "Đợi nhận bàn" trên sơ đồ)
                var ban = await _context.BanAns.FindAsync(datBan.BanAnId);
                if (ban != null)
                {
                    ban.TrangThai = 2; // Giả sử sơ đồ bàn của bạn quy định 2 là màu vàng
                    _context.Update(ban);
                }

                // BƯỚC 3: Lưu món ăn (Vẫn lưu bình thường nhưng khách chưa ăn ngay)
                if (!string.IsNullOrEmpty(cartJson))
                {
                    var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CartItem>>(cartJson);
                    foreach (var item in items)
                    {
                        var chiTiet = new ChiTietDatMon
                        {
                            DatBanId = datBan.Id,
                            MonAnId = item.Id,
                            SoLuong = item.Qty,
                            BanAnId = datBan.BanAnId
                        };
                        _context.ChiTietDatMons.Add(chiTiet);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "BanAns"); // Quay lại sơ đồ bàn
            }
            return View(datBan);
        }

        // 3. Action xử lý nhận khách vào bàn (Bắt đầu phục vụ)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> NhanBan(int id)
        {
            var datBan = await _context.DatBans.Include(d => d.BanAn).FirstOrDefaultAsync(d => d.Id == id);
            if (datBan != null)
            {
                datBan.TrangThai = 1; // Chuyển đơn đặt sang "Đang phục vụ"

                if (datBan.BanAn != null)
                {
                    datBan.BanAn.TrangThai = 1; // Chuyển bàn sang màu Đỏ "Đã có khách"
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Khách đã vào bàn, bắt đầu phục vụ!";
            }
            // Sau khi nhận bàn, dẫn đến trang Chi tiết để xem món/in phiếu
            return RedirectToAction("Details", new { id = id });
        }
        // 4. TRANG THANH TOÁN (GET): Hiển thị hóa đơn cho nhân viên kiểm tra

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Checkout(int? id)
        {
            if (id == null) return NotFound();

            var datBan = await _context.DatBans
                .Include(d => d.BanAn)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (datBan == null) return NotFound();

            var danhSachMon = await _context.ChiTietDatMons
                .Include(c => c.MonAn)
                .Where(c => c.DatBanId == id)
                .ToListAsync();

            decimal tongTienMon = danhSachMon.Sum(c => c.SoLuong * c.MonAn.Gia);

            double tongCong = (double)tongTienMon * 1.1;

            ViewBag.DanhSachMon = danhSachMon;
            ViewBag.TongCong = tongCong;

            return View(datBan);
        }

        // 5. XÁC NHẬN THANH TOÁN (POST): Chốt đơn và làm trống bàn
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id, string paymentMethod)
        {
            var donDat = await _context.DatBans.Include(d => d.BanAn).FirstOrDefaultAsync(m => m.Id == id);

            if (donDat != null)
            {
                // Bước A: Giải phóng bàn ăn về trạng thái Trống (0)
                if (donDat.BanAn != null)
                {
                    donDat.BanAn.TrangThai = 0;
                    _context.Update(donDat.BanAn);

                    // Bước B: Xóa danh sách món đã gọi của bàn này (Vì khách sau sẽ gọi món mới)
                    var chiTietMon = _context.ChiTietDatMons.Where(c => c.BanAnId == donDat.BanAnId);
                    _context.ChiTietDatMons.RemoveRange(chiTietMon);
                }

                // Bước C: Xóa đơn đặt bàn (Hoặc đổi trạng thái thành 2 nếu bạn muốn lưu doanh thu)
                // donDat.TrangThai = 2; _context.Update(donDat); <-- Dùng cái này nếu muốn lưu lịch sử
                _context.DatBans.Remove(donDat);

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã thanh toán và trả Bàn {donDat.BanAn?.SoBan} thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. Xem chi tiết đơn đặt (Dành cho quản lý xem nhanh)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var donDat = await _context.DatBans
                .Include(d => d.BanAn)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (donDat == null) return NotFound();

            var danhSachMon = await _context.ChiTietDatMons
                .Include(c => c.MonAn)
                .Where(c => c.BanAnId == donDat.BanAnId)
                .ToListAsync();

            ViewBag.DanhSachMon = danhSachMon;
            return View(donDat);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(int tableId, string cartJson, string? tenKhach, string? soDienThoai)
        {
            // 1. Kiểm tra giỏ hàng trống
            if (string.IsNullOrEmpty(cartJson) || cartJson == "[]")
            {
                return RedirectToAction("Index", "MonAns", new { banId = tableId });
            }

            // 2. TÌM ĐƠN ĐẶT HIỆN TẠI CỦA BÀN (Trạng thái 0 hoặc 1)
            var currentDatBan = await _context.DatBans
                .FirstOrDefaultAsync(d => d.BanAnId == tableId && (d.TrangThai == 0 || d.TrangThai == 1));

            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

            if (currentDatBan != null)
            {
                // --- TRƯỜNG HỢP GỌI THÊM ---
                foreach (var item in items)
                {
                    var existingDetail = await _context.ChiTietDatMons
                        .FirstOrDefaultAsync(c => c.DatBanId == currentDatBan.Id && c.MonAnId == item.Id);

                    if (existingDetail != null)
                    {
                        existingDetail.SoLuong += item.Qty;
                        _context.Update(existingDetail);
                    }
                    else
                    {
                        _context.ChiTietDatMons.Add(new ChiTietDatMon
                        {
                            DatBanId = currentDatBan.Id,
                            MonAnId = item.Id,
                            SoLuong = item.Qty,
                            BanAnId = tableId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật thêm món vào bàn!";

                // SỬA TẠI ĐÂY: Quay về sơ đồ bàn thay vì vào trang Details bị chặn quyền
                return RedirectToAction("Index", "BanAns");
            }
            else
            {
                // --- TRƯỜNG HỢP ĐẶT MỚI ---
                if (string.IsNullOrEmpty(tenKhach))
                {
                    return RedirectToAction("Create", new { tableId = tableId, cartJson = cartJson });
                }

                var datBan = new DatBan
                {
                    BanAnId = tableId,
                    TenKhachHang = tenKhach,
                    SoDienThoai = soDienThoai,
                    NgayDat = DateTime.Now,
                    TrangThai = 1
                };

                _context.DatBans.Add(datBan);

                var banAn = await _context.BanAns.FindAsync(tableId);
                if (banAn != null) banAn.TrangThai = 1;

                await _context.SaveChangesAsync();

                foreach (var item in items)
                {
                    _context.ChiTietDatMons.Add(new ChiTietDatMon
                    {
                        DatBanId = datBan.Id,
                        MonAnId = item.Id,
                        SoLuong = item.Qty,
                        BanAnId = tableId
                    });
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đặt bàn và gọi món thành công!";

                // SỬA TẠI ĐÂY: Quay về sơ đồ bàn
                return RedirectToAction("Index", "BanAns");
            }
        }
        // Class phụ để hứng dữ liệu JSON
        public class CartItem
        {
            public int Id { get; set; }
            public int Qty { get; set; }
        }
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetTableDetails(int id)
        {
            var monDaDat = await _context.ChiTietDatMons
                .AsNoTracking()
                .Where(c => c.BanAnId == id && (c.DatBan.TrangThai == 0 || c.DatBan.TrangThai == 1))
                .Select(c => new
                {
                    tenMon = c.MonAn.TenMon,
                    soLuong = c.SoLuong,
                    thanhTien = c.SoLuong * c.MonAn.Gia
                })
                .ToListAsync();
            return Json(new { monDaDat });
        }
    }
}