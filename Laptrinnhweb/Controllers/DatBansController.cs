using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class DatBansController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DatBansController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        ViewBag.TableId = tableId;
        ViewBag.TenBan = ban.SoBan;
        ViewBag.CartJson = cartJson;

        // If blocked, show error on the same page using ModelState
        if (user.IsBlocked)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đang bị chặn, không thể tạo đặt bàn mới. Vui lòng liên hệ quản trị viên.");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DatBan datBan, string cartJson)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        // If blocked → show error on the same form
        if (user.IsBlocked)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đang bị chặn, không thể tạo đặt bàn mới. Vui lòng liên hệ quản trị viên.");
            // Ensure view data is preserved
            var banForView = await _context.BanAns.FindAsync(datBan.BanAnId);
            ViewBag.TableId = datBan.BanAnId;
            ViewBag.TenBan = banForView?.SoBan;
            ViewBag.CartJson = cartJson;
            return View(datBan);
        }

        string userId = user.Id;

        // check number of reservations for this user (exclude soft-deleted if any; adjust predicate if needed)
        var count = await _context.DatBans
            .CountAsync(d => d.UserId == userId && d.TrangThai != -1);

        if (count >= 2)
        {
            // block user
            user.IsBlocked = true;
            await _userManager.UpdateAsync(user);

            ModelState.AddModelError(string.Empty, "Bạn đã vượt quá 2 lần đặt bàn và đã bị chặn!");

            // Preserve view data and return same view so user sees the error immediately
            var banForView = await _context.BanAns.FindAsync(datBan.BanAnId);
            ViewBag.TableId = datBan.BanAnId;
            ViewBag.TenBan = banForView?.SoBan;
            ViewBag.CartJson = cartJson;

            return View(datBan);
        }

        // validate form
        if (!ModelState.IsValid)
        {
            var banForView = await _context.BanAns.FindAsync(datBan.BanAnId);
            ViewBag.TableId = datBan.BanAnId;
            ViewBag.TenBan = banForView?.SoBan;
            ViewBag.CartJson = cartJson;
            return View(datBan);
        }

        // create reservation
        datBan.TrangThai = 0;
        datBan.UserId = userId;
        datBan.NgayDat = DateTime.Now;

        _context.DatBans.Add(datBan);
        await _context.SaveChangesAsync();

        // update table status
        var ban = await _context.BanAns.FindAsync(datBan.BanAnId);
        if (ban != null)
        {
            ban.TrangThai = 2;
            _context.Update(ban);
        }

        // save cart items
        if (!string.IsNullOrEmpty(cartJson))
        {
            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

            foreach (var item in items)
            {
                _context.ChiTietDatMons.Add(new ChiTietDatMon
                {
                    DatBanId = datBan.Id,
                    MonAnId = item.Id,
                    SoLuong = item.Qty,
                    BanAnId = datBan.BanAnId
                });
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Đặt bàn và gọi món thành công!";
        return RedirectToAction("Index", "BanAns");
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
            .Where(c => c.DatBanId == donDat.Id)
            .ToListAsync();

        ViewBag.DanhSachMon = danhSachMon;
        return View(donDat);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmOrder(int tableId, string cartJson, string tenKhach, string soDienThoai, DateTime gioDenDuyKien, string gopIds)
    {
        // 1. Kiểm tra giỏ hàng
        if (string.IsNullOrEmpty(cartJson) || cartJson == "[]")
        {
            return RedirectToAction("Index", "MonAns", new { banId = tableId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

        // 2. Kiểm tra trùng lịch
        var thoiGianKetThucDuKien = gioDenDuyKien.AddMinutes(1);
        var isOverlap = await _context.DatBans.AnyAsync(d =>
            d.BanAnId == tableId &&
            d.TrangThai == 0 &&
            ((gioDenDuyKien >= d.GioDenDuyKien && gioDenDuyKien < d.GioDenDuyKien.AddHours(2)) ||
             (thoiGianKetThucDuKien > d.GioDenDuyKien && thoiGianKetThucDuKien <= d.GioDenDuyKien.AddHours(2))));

        if (isOverlap)
        {
            TempData["Error"] = "Bàn này đã được khách khác đặt!";
            return RedirectToAction("Index", "BanAns");
        }

        // 3. TÌM HÓA ĐƠN MÀ USER NÀY ĐANG NGỒI (Trạng thái 1 - Đang phục vụ)
        var currentActiveBooking = await _context.DatBans
            .FirstOrDefaultAsync(d => d.UserId == userId && d.TrangThai == 1);

        if (currentActiveBooking != null)
        {
            // A. Cập nhật danh sách gộp bàn
            var currentGop = currentActiveBooking.GhiChuGopBan ?? currentActiveBooking.BanAnId.ToString();
            var listGop = currentGop.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!listGop.Contains(tableId.ToString()))
            {
                listGop.Add(tableId.ToString());
                currentActiveBooking.GhiChuGopBan = string.Join(",", listGop);
                _context.Update(currentActiveBooking); // QUAN TRỌNG: Phải cập nhật lại đơn cũ
            }

            // B. Cập nhật món ăn mới vào đơn hiện có
            foreach (var item in items)
            {
                var existingDetail = await _context.ChiTietDatMons
                    .FirstOrDefaultAsync(c => c.DatBanId == currentActiveBooking.Id && c.MonAnId == item.Id);

                if (existingDetail != null)
                {
                    existingDetail.SoLuong += item.Qty;
                    _context.Update(existingDetail);
                }
                else
                {
                    _context.ChiTietDatMons.Add(new ChiTietDatMon
                    {
                        DatBanId = currentActiveBooking.Id,
                        MonAnId = item.Id,
                        SoLuong = item.Qty,
                        BanAnId = tableId
                    });
                }
            }

            // C. KHÓA BÀN MỚI (Chuyển thẳng sang màu đỏ - TrangThai = 1)
            var banMoi = await _context.BanAns.FindAsync(tableId);
            if (banMoi != null)
            {
                banMoi.TrangThai = 1; // Khách đang ngồi nên chuyển sang "Đang phục vụ"
                _context.Update(banMoi);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã gộp bàn {banMoi?.SoBan} vào đơn ID #{currentActiveBooking.Id}!";
            return RedirectToAction("Index", "BanAns");
        }

        // 4. Nếu chưa có tên khách (bắt đầu đặt mới hoàn toàn), quay về Create
        if (string.IsNullOrEmpty(tenKhach))
        {
            return RedirectToAction("Create", new { tableId = tableId, cartJson = cartJson, gopIds = gopIds });
        }

        // 5. TẠO ĐƠN ĐẶT MỚI (Dành cho khách mới đến lần đầu)
        var datBan = new DatBan
        {
            BanAnId = tableId,
            TenKhachHang = tenKhach,
            SoDienThoai = soDienThoai,
            NgayDat = DateTime.Now,
            GioDenDuyKien = gioDenDuyKien,
            TrangThai = 0,
            UserId = userId,
            GhiChuGopBan = gopIds
        };

        _context.DatBans.Add(datBan);
        await _context.SaveChangesAsync(); 

        var idsToBlock = new List<int> { tableId };
        if (!string.IsNullOrEmpty(gopIds))
        {
            var extraIds = gopIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
            idsToBlock.AddRange(extraIds);
        }

        var listBans = await _context.BanAns.Where(b => idsToBlock.Distinct().Contains(b.Id)).ToListAsync();
        foreach (var ban in listBans)
        {
            ban.TrangThai = 2; // Khóa màu vàng
        }
        _context.UpdateRange(listBans);

        // 7. Lưu món ăn vào chi tiết
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
        TempData["Success"] = "Đặt gộp bàn thành công!";
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var monDaDat = await _context.ChiTietDatMons
            .AsNoTracking()
            .Where(c => c.BanAnId == id && c.DatBan.UserId == userId && (c.DatBan.TrangThai == 0 || c.DatBan.TrangThai == 1))
            .Select(c => new {
                tenMon = c.MonAn.TenMon,
                soLuong = c.SoLuong,
                thanhTien = c.SoLuong * c.MonAn.Gia
            })
            .ToListAsync();
        return Json(new { monDaDat });
    }

    public async Task ReleaseExpiredBookings()
    {
        var bayGio = DateTime.Now;

        // Tìm các đơn đặt bàn quá 2 tiếng mà khách chưa đến (TrangThai vẫn là 0)
        var expiredBookings = await _context.DatBans
            .Include(d => d.BanAn)
            .Where(d => d.TrangThai == 0 && bayGio > d.GioDenDuyKien.AddHours(2))
            .ToListAsync();

        foreach (var booking in expiredBookings)
        {
            // 1. Chuyển đơn đặt sang trạng thái "Hủy/Quá hạn" (ví dụ: 3)
            booking.TrangThai = 3;

            // 2. Trả bàn về trạng thái Trống (0)
            if (booking.BanAn != null)
            {
                booking.BanAn.TrangThai = 0;
            }
        }
        await _context.SaveChangesAsync();
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
    private async Task<bool> CheckAndBlockUserAsync(ApplicationUser user)
    {
        if (user == null) return false;

        // 1. Nếu đã bị chặn từ trước thì không cho đặt tiếp
        if (user.IsBlocked) return true;

        // 2. Đếm số đơn đặt chưa hoàn thành (Trạng thái 0: Đợi, 1: Đang phục vụ)
        var count = await _context.DatBans
            .CountAsync(d => d.UserId == user.Id && (d.TrangThai == 0 || d.TrangThai == 1));

        if (count >= 2)
        {
            user.IsBlocked = true;
            await _userManager.UpdateAsync(user);
            return true;
        }

        return false;
    }
}