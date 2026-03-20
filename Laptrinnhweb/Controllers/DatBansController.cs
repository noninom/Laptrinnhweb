using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Authorization; // Cần thiết để phân quyền
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Laptrinnhweb.Controllers
{
    [Authorize] // Tất cả nhân viên và Admin phải đăng nhập mới được vào
    public class DatBansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatBansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // DÀNH CHO CẢ ADMIN & USER (NHÂN VIÊN)
        // ==========================================

        // 1. Xem sơ đồ/danh sách bàn đang phục vụ
        public async Task<IActionResult> Index()
        {
            var danhSachDat = await _context.DatBans
                .Include(d => d.BanAn)
                .Where(d => d.TrangThai < 2) // Chỉ hiện khách chưa thanh toán
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();
            return View(danhSachDat);
        }

        // 2. Nhận khách vào bàn
        public async Task<IActionResult> NhanBan(int id)
        {
            var datBan = await _context.DatBans.Include(d => d.BanAn).FirstOrDefaultAsync(d => d.Id == id);
            if (datBan != null)
            {
                datBan.TrangThai = 1; // Đang phục vụ
                if (datBan.BanAn != null) datBan.BanAn.TrangThai = 1;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Khách đã vào bàn!";
            }
            return RedirectToAction(nameof(Index));
        }

        // 3. Xem chi tiết hóa đơn (Trước khi thanh toán)
        public async Task<IActionResult> Checkout(int? id)
        {
            if (id == null) return NotFound();

            var donDat = await _context.DatBans
                .Include(d => d.BanAn)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (donDat == null) return NotFound();

            var danhSachMon = await _context.ChiTietDatMons
                .Include(c => c.MonAn)
                .Where(c => c.DatBanId == id) // Lọc chuẩn theo mã đơn
                .ToListAsync();

            ViewBag.DanhSachMon = danhSachMon;
            return View(donDat);
        }

        // ==========================================
        // DÀNH RIÊNG CHO ADMIN
        // ==========================================

        // 4. Báo cáo doanh thu Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;

            // Lấy danh sách ID đơn của hôm nay
            var danhSachIdDonHang = await _context.DatBans
                .Where(d => d.NgayDat.Date == today && (d.TrangThai == 1 || d.TrangThai == 2))
                .Select(d => d.Id)
                .ToListAsync();

            // Tính tổng tiền chuẩn xác
            var tongTien = await _context.ChiTietDatMons
                .Include(ct => ct.MonAn)
                .Where(ct => danhSachIdDonHang.Contains(ct.Id))
                .SumAsync(ct => (decimal?)(ct.SoLuong * (ct.MonAn != null ? ct.MonAn.Gia : 0))) ?? 0;

            ViewBag.TongDoanhThu = tongTien;
            ViewBag.DonMoi = danhSachIdDonHang.Count;
            ViewBag.BanDangDung = await _context.BanAns.CountAsync(b => b.TrangThai == 1);

            // Top 5 món chạy nhất
            ViewBag.TopMons = await _context.ChiTietDatMons
                .Where(ct => danhSachIdDonHang.Contains(ct.Id))
                .Include(c => c.MonAn)
                .GroupBy(c => c.MonAn.TenMon)
                .Select(g => new { TenMon = g.Key, SoLuong = g.Sum(c => c.SoLuong) })
                .OrderByDescending(x => x.SoLuong)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // 5. Xác nhận thanh toán (Chốt doanh thu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Thường chỉ quản lý mới được chốt tiền
        public async Task<IActionResult> ConfirmPayment(int id, string paymentMethod)
        {
            var donDat = await _context.DatBans.Include(d => d.BanAn).FirstOrDefaultAsync(m => m.Id == id);

            if (donDat != null)
            {
                if (donDat.BanAn != null)
                {
                    donDat.BanAn.TrangThai = 0; // Trả bàn về màu xanh
                    _context.Update(donDat.BanAn);
                }

                donDat.TrangThai = 2; // Đánh dấu đã thanh toán (để Dashboard hiện tiền)
                _context.Update(donDat);

                await _context.SaveChangesAsync();
                TempData["Success"] = "Thanh toán thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}