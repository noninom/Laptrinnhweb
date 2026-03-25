using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Laptrinnhweb.Controllers
{
    public class BanAnsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BanAnsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Giao diện sơ đồ bàn thực tế
        public async Task<IActionResult> Index()
        {
            // Tự động tạo 10 bàn nếu database trống (giữ nguyên của bạn)
            if (!_context.BanAns.Any())
            {
                var danhSach10Ban = new List<BanAn>();
                for (int i = 1; i <= 10; i++)
                {
                    danhSach10Ban.Add(new BanAn
                    {
                        SoBan = i < 10 ? $"0{i}" : $"{i}",
                        SoChoNgoi = (i <= 5) ? 4 : 6,
                        TrangThai = 0
                    });
                }
                _context.BanAns.AddRange(danhSach10Ban);
                await _context.SaveChangesAsync();
            }

            // --- LOGIC GIẢI PHÓNG BÀN QUÁ HẠN (THÊM MỚI) ---
            var bayGio = DateTime.Now;
            var expiredBookings = await _context.DatBans
                .Include(d => d.BanAn)
                .Where(d => d.TrangThai == 0 && bayGio > d.GioDenDuyKien.AddMinutes(1))
                .ToListAsync();

            foreach (var booking in expiredBookings)
            {
                booking.TrangThai = 3; // Trạng thái Hủy/Quá hạn
                if (booking.BanAn != null) booking.BanAn.TrangThai = 0; // Trả bàn về Trống
            }
            if (expiredBookings.Any()) await _context.SaveChangesAsync();
            // ----------------------------------------------

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy danh sách tất cả các đơn đặt bàn đang hoạt động (chưa thanh toán) để kiểm tra gộp bàn
            var allActiveBookings = await _context.DatBans
                .Where(d => d.TrangThai == 0 || d.TrangThai == 1)
                .ToListAsync();

            var listBanAn = await _context.BanAns.ToListAsync();

            foreach (var ban in listBanAn)
            {
                // 1. Tìm đơn đặt bàn ACTIVE liên quan đến bàn này
                // Kiểm tra xem bàn này là bàn chính (BanAnId) 
                // HOẶC là bàn phụ (nằm trong chuỗi GhiChuGopBan, ví dụ: "02,03")
                var active = allActiveBookings
                    .FirstOrDefault(d => d.BanAnId == ban.Id ||
                                        (d.GhiChuGopBan != null && d.GhiChuGopBan.Split(',').Contains(ban.Id.ToString())));

                ban.ActiveDatBan = active;

                // 2. Kiểm tra quyền sở hữu (IsOwner)
                // Nếu tìm thấy đơn hàng liên quan VÀ UserId của đơn đó trùng với User đang đăng nhập
                ban.IsOwner = active != null && active.UserId == userId;
            }

            return View(listBanAn);
        }

            // 2. Trang tạo bàn mới
            [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanAn banAn)
        {
            if (ModelState.IsValid)
            {
                _context.Add(banAn);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(banAn);
        }

        // 3. Thay đổi trạng thái bàn nhanh
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var banAn = await _context.BanAns.FindAsync(id);
            if (banAn != null)
            {
                // Đảo trạng thái giữa Trống (0) và Có khách (1)
                banAn.TrangThai = (banAn.TrangThai == 0) ? 1 : 0;
                _context.Update(banAn);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. Xóa bàn
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var banAn = await _context.BanAns.FindAsync(id);
            if (banAn != null)
            {
                _context.BanAns.Remove(banAn);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. Chỉnh sửa thông tin bàn
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var banAn = await _context.BanAns.FindAsync(id);
            if (banAn == null) return NotFound();
            return View(banAn);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BanAn banAn)
        {
            if (id != banAn.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(banAn);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.BanAns.Any(e => e.Id == banAn.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(banAn);
        }
    }
}