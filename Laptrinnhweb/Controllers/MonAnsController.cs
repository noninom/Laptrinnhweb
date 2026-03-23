using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Laptrinnhweb.Controllers
{
    public class MonAnsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Khởi tạo kết nối Database
        public MonAnsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // BƯỚC 1: KIỂM TRA VÀ LƯU VÀO SQL (Chỉ chạy 1 lần duy nhất khi DB trống)
            if (!_context.MonAns.Any())
            {
                var danhSachMon = new List<MonAn>
                {
                    // LƯU Ý: Không ghi "Id = ..." ở đây để SQL tự tăng số ID
                    new MonAn { TenMon = "Tôm Hùm Nướng Phô Mai", Gia = 450000, Loai = "Food", HinhAnh = "tomphomai.jpg", MoTa = "Tôm hùm tươi sống nướng cùng lớp phô mai Mozzarella tan chảy." },
                    new MonAn { TenMon = "Cua Rang Me", Gia = 350000, Loai = "Food", HinhAnh = "cua.jpg", MoTa = "Cua thịt chắc ngọt quyện cùng sốt me chua cay đậm đà." },
                    new MonAn { TenMon = "Hàu Sữa Nướng Mỡ Hành", Gia = 120000, Loai = "Food", HinhAnh = "hau.jpg", MoTa = "Hàu sữa béo ngậy nướng cùng mỡ hành và đậu phộng rang." },
                    new MonAn { TenMon = "Mực Trứng Hấp Gừng", Gia = 180000, Loai = "Food", HinhAnh = "muc.jpg", MoTa = "Mực trứng tươi rói hấp cùng gừng sợi và hành lá thanh mát." },
                    new MonAn { TenMon = "Lẩu Hải Sản Thập Cẩm", Gia = 550000, Loai = "Food", HinhAnh = "lau.jpg", MoTa = "Nước dùng lẩu Thái chua cay cùng đủ loại tôm, mực, ngao, cá." },
                    new MonAn { TenMon = "Ốc Hương Rang Muối", Gia = 150000, Loai = "Food", HinhAnh = "oc.jpg", MoTa = "Ốc hương giòn sần sật rang cùng muối ớt cay nồng." },
                    new MonAn { TenMon = "Bào Ngư Sốt Dầu Hào", Gia = 320000, Loai = "Food", HinhAnh = "baongu.jpg", MoTa = "Bào Ngư thượng hạng hầm mềm cùng nước sốt dầu hào đậm vị." },
                    new MonAn { TenMon = "Sashimi Cá Hồi Tươi", Gia = 220000, Loai = "Food", HinhAnh = "sashimi.jpg", MoTa = "Những lát cá hồi tươi rói tan ngay trong miệng kèm mù tạt." },
                    new MonAn { TenMon = "Coca Cola", Gia = 15000, Loai = "Drink", HinhAnh = "coca.jpg", MoTa = "Nước giải khát có ga." },
                    new MonAn { TenMon = "Bia Tiger Bạc", Gia = 22000, Loai = "Drink", HinhAnh = "tiger.jpg", MoTa = "Bia ướp lạnh, giải nhiệt tức thì." },
                    new MonAn { TenMon = "Nước Ép Cam Tươi", Gia = 35000, Loai = "Drink", HinhAnh = "nuoccam.jpg", MoTa = "Nước cam nguyên chất giàu vitamin C." },
                    new MonAn { TenMon = "Bia Heineken", Gia = 25000, Loai = "Drink", HinhAnh = "ken.jpg", MoTa = "Bia nhập khẩu cao cấp." },
                    new MonAn { TenMon = "Nước suối Aquafina", Gia = 10000, Loai = "Drink", HinhAnh = "aquafina.jpg", MoTa = "Nước uống tinh khiết." }
                };

                _context.MonAns.AddRange(danhSachMon);
                await _context.SaveChangesAsync(); // Lệnh quan trọng: Đẩy dữ liệu vào SQL Server
            }

            // BƯỚC 2: LẤY DỮ LIỆU TỪ SQL RA ĐỂ HIỂN THỊ
            var query = from m in _context.MonAns select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.TenMon.Contains(searchString));
            }

            return View(await query.ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> Order(int banId, int monId, int soLuong)
        {
            if (soLuong < 1) soLuong = 1;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var activeBooking = await _context.DatBans
                .FirstOrDefaultAsync(d =>
                    d.BanAnId == banId
                    && d.UserId == userId   // 🔥 CHẶN USER
                    && d.TrangThai != 2
                );

            if (activeBooking == null)
            {
                TempData["Error"] = "Bàn này hiện chưa có khách đặt hoặc đã thanh toán.";
                return RedirectToAction("Index", "BanAns");
            }

            // 2. Tìm xem món này đã có trong đơn đặt bàn HIỆN TẠI chưa
            // Lọc theo DatBanId là chính xác nhất để không lẫn với khách cũ
            var existingItem = await _context.ChiTietDatMons
                .FirstOrDefaultAsync(o => o.DatBanId == activeBooking.Id && o.MonAnId == monId);

            if (existingItem != null)
            {
                // Nếu có rồi thì tăng số lượng
                existingItem.SoLuong += soLuong;
                _context.Update(existingItem);
            }
            else
            {
                // Nếu chưa có thì tạo mới và BẮT BUỘC gán DatBanId
                var newItem = new ChiTietDatMon
                {
                    BanAnId = banId,
                    MonAnId = monId,
                    SoLuong = soLuong,
                    DatBanId = activeBooking.Id // Không dùng ?. nữa, bắt buộc phải có ID này
                };
                _context.ChiTietDatMons.Add(newItem);
            }

            await _context.SaveChangesAsync();

            var ban = await _context.BanAns.FindAsync(banId);
            TempData["Success"] = "Đã chọn món thành công!";

            return RedirectToAction("Index", new { banId = banId, tenBan = ban?.SoBan });
        }

        [HttpGet]
        [AllowAnonymous] // Cho phép khách xem danh sách món đã đặt mà không cần đăng nhập Admin
        public async Task<IActionResult> GetTableDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var activeBooking = await _context.DatBans
                .FirstOrDefaultAsync(d =>
                    d.BanAnId == id
                    && d.UserId == userId   // 🔥 CHẶN
                    && d.TrangThai != 2
                );

            if (activeBooking == null)
            {
                return Json(new { monDaDat = new List<object>() });
            }

            // 2. Lấy chi tiết các món đã đặt gắn liền với đơn đặt bàn này
            var monDaDat = await _context.ChiTietDatMons
                .Where(ct => ct.DatBanId == activeBooking.Id)
                .Select(ct => new
                {
                    tenMon = ct.MonAn.TenMon,
                    soLuong = ct.SoLuong,
                    thanhTien = ct.SoLuong * ct.MonAn.Gia
                })
                .ToListAsync();

            return Json(new { monDaDat });
        }

    }
}