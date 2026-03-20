using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            // Tự động nạp 10 bàn nếu cơ sở dữ liệu trống
            if (!_context.BanAns.Any())
            {
                var danhSach10Ban = new List<BanAn>();
                for (int i = 1; i <= 10; i++)
                {
                    danhSach10Ban.Add(new BanAn
                    {
                        SoBan = i < 10 ? $"0{i}" : $"{i}",
                        SoChoNgoi = (i <= 5) ? 4 : 6,
                        TrangThai = 0 // Mặc định là bàn trống (Xanh)
                    });
                }
                _context.BanAns.AddRange(danhSach10Ban);
                await _context.SaveChangesAsync();
            }

            // QUAN TRỌNG: Phải dùng Include(b => b.DatBans) để View kiểm tra được TrangThai đơn hàng
            // giúp hiện nút "GỌI THÊM" hoặc "ĐỢI NHẬN BÀN" chính xác
            var listBanAn = await _context.BanAns
                .Include(b => b.DatBans)
                .ToListAsync();

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