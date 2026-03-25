using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                // --- BỔ SUNG: Giải phóng tất cả các bàn đã gộp ---
                if (!string.IsNullOrEmpty(donDat.GhiChuGopBan))
                {
                    // Tách chuỗi "1,2" thành danh sách ID [1, 2]
                    var danhSachIdBan = donDat.GhiChuGopBan.Split(',')
                                             .Select(int.Parse)
                                             .ToList();

                    var tatCaBanLienQuan = await _context.BanAns
                                             .Where(b => danhSachIdBan.Contains(b.Id))
                                             .ToListAsync();

                    foreach (var ban in tatCaBanLienQuan)
                    {
                        ban.TrangThai = 0; // Trả tất cả về Trống
                    }
                    _context.UpdateRange(tatCaBanLienQuan);
                }
                else if (donDat.BanAn != null) // Trường hợp đơn bình thường không gộp
                {
                    donDat.BanAn.TrangThai = 0;
                    _context.Update(donDat.BanAn);
                }

                // Bước B: Xóa chi tiết món (Giữ nguyên của bạn)
                var chiTietMon = _context.ChiTietDatMons.Where(c => c.DatBanId == donDat.Id);
                _context.ChiTietDatMons.RemoveRange(chiTietMon);

                // Bước C: Xóa đơn hoặc lưu lịch sử (Nên đổi sang trạng thái 2 để lưu doanh thu)
                _context.DatBans.Remove(donDat);

                await _context.SaveChangesAsync();
                TempData["Success"] = "Thanh toán thành công và đã giải phóng toàn bộ bàn gộp!";
            }

            return RedirectToAction(nameof(Index), "BanAns");
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
        public async Task<IActionResult> ConfirmOrder(int tableId, string cartJson, string tenKhach, string soDienThoai, DateTime gioDenDuyKien)
        {
            // 1. Kiểm tra giỏ hàng
            if (string.IsNullOrEmpty(cartJson) || cartJson == "[]")
            {
                return RedirectToAction("Index", "MonAns", new { banId = tableId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

            // ==========================================================
            // ✅ BƯỚC 1: KIỂM TRA TRÙNG LỊCH (LOGIC GIỮ BÀN 2 TIẾNG)
            // ==========================================================

            // Tìm các đơn đặt bàn đang ở trạng thái "Đợi nhận bàn" (TrangThai = 0) của bàn này
            // Kiểm tra xem giờ đến mới có nằm trong khoảng 2 tiếng của các đơn cũ không
            var thoiGianKetThucDuKien = gioDenDuyKien.AddMinutes(1);

            var isOverlap = await _context.DatBans.AnyAsync(d =>
                d.BanAnId == tableId &&
                d.TrangThai == 0 && // Chỉ check với các đơn đang giữ bàn (chưa nhận bàn)
                ((gioDenDuyKien >= d.GioDenDuyKien && gioDenDuyKien < d.GioDenDuyKien.AddHours(2)) ||
                 (thoiGianKetThucDuKien > d.GioDenDuyKien && thoiGianKetThucDuKien <= d.GioDenDuyKien.AddHours(2))));

            if (isOverlap)
            {
                TempData["Error"] = "Bàn này đã được khách khác đặt trong khung giờ bạn chọn (nhà hàng giữ bàn 2 tiếng). Vui lòng chọn giờ khác hoặc bàn khác!";
                return RedirectToAction("Index", "BanAns");
            }

            // ==========================================================
            // ✅ BƯỚC 2: KIỂM TRA ĐƠN ĐANG ĂN (TrangThai = 1)
            // ==========================================================
            var activeBooking = await _context.DatBans
                .FirstOrDefaultAsync(d => d.BanAnId == tableId && d.TrangThai == 1 && (isAdmin || d.UserId == userId));

            // ==========================================================
            // ✅ THỰC THI: GỌI THÊM MÓN (Nếu khách đang ngồi ăn tại bàn)
            // ==========================================================
            if (activeBooking != null)
            {
                foreach (var item in items)
                {
                    var existingDetail = await _context.ChiTietDatMons
                        .FirstOrDefaultAsync(c => c.DatBanId == activeBooking.Id && c.MonAnId == item.Id);

                    if (existingDetail != null)
                    {
                        existingDetail.SoLuong += item.Qty;
                        _context.Update(existingDetail);
                    }
                    else
                    {
                        _context.ChiTietDatMons.Add(new ChiTietDatMon
                        {
                            DatBanId = activeBooking.Id,
                            MonAnId = item.Id,
                            SoLuong = item.Qty,
                            BanAnId = tableId
                        });
                    }
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật thêm món vào hóa đơn đang sử dụng!";
                return RedirectToAction("Index", "BanAns");
            }

            // ==========================================================
            // ✅ THỰC THI: TẠO ĐƠN ĐẶT MỚI (GIỮ BÀN)
            // ==========================================================
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
                GioDenDuyKien = gioDenDuyKien, // Lưu giờ khách hẹn
                TrangThai = 0, // 0: Đặt trước/Giữ bàn
                UserId = userId
            };

            _context.DatBans.Add(datBan);

            // Cập nhật trạng thái bàn thành 2 (Giả sử 2 là màu vàng - Đã đặt)
            var banAn = await _context.BanAns.FindAsync(tableId);
            if (banAn != null)
            {
                banAn.TrangThai = 2;
                _context.Update(banAn);
            }

            await _context.SaveChangesAsync();

            // Lưu món ăn vào chi tiết
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
            TempData["Success"] = $"Đặt bàn {banAn?.SoBan} thành công! Nhà hàng sẽ giữ bàn cho bạn đến {gioDenDuyKien.AddMinutes(1):HH:mm}.";

            return RedirectToAction("Index", "BanAns");
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

        [HttpPost]
        public async Task<IActionResult> GopBan(int banChinhId, int banPhuId)
        {
            var banChinh = await _context.BanAns.FindAsync(banChinhId);
            var banPhu = await _context.BanAns.FindAsync(banPhuId);

            if (banChinh == null || banPhu == null) return NotFound();

            // Kiểm tra bàn phụ phải đang trống (TrangThai == 0) mới cho gộp
            if (banPhu.TrangThai != 0)
            {
                TempData["Error"] = "Bàn phụ đang có khách hoặc đã được đặt, không thể gộp!";
                return RedirectToAction("Index", "BanAns");
            }

            var hienTai = await _context.DatBans
                .FirstOrDefaultAsync(d => d.BanAnId == banChinhId && d.TrangThai == 1);

            if (hienTai != null)
            {
                // Lưu vết: "ID_Ban_Chinh, ID_Ban_Phu"
                hienTai.GhiChuGopBan = (hienTai.GhiChuGopBan ?? banChinhId.ToString()) + "," + banPhuId;
                banPhu.TrangThai = 1; // Bàn phụ cũng chuyển sang màu đỏ

                _context.Update(hienTai);
                _context.Update(banPhu);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã gộp bàn {banPhu.SoBan} vào bàn {banChinh.SoBan}";
            }
            return RedirectToAction("Index", "BanAns");
        }
    }
}