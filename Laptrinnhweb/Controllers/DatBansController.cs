using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class DatBansController : Controller
{
    private readonly ApplicationDbContext _context;

    public DatBansController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. Xem danh sách khách đặt bàn
    public async Task<IActionResult> Index()
    {
        var danhSachDat = await _context.DatBans
            .Include(d => d.BanAn)
            .OrderByDescending(d => d.NgayDat)
            .ToListAsync();
        return View(danhSachDat);
    }

    // 2. Trang nhập thông tin khách khi bắt đầu đặt
    public async Task<IActionResult> Create(int tableId)
    {
        var ban = await _context.BanAns.FindAsync(tableId);
        if (ban == null) return NotFound();

        ViewBag.TableId = tableId;
        ViewBag.TenBan = ban.SoBan;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DatBan datBan)
    {
        datBan.TrangThai = 0; // Mặc định là mới đặt (Đợi nhận bàn)

        if (ModelState.IsValid)
        {
            _context.Add(datBan);

            // Khóa bàn ngay khi có khách đặt (Chuyển sang màu đỏ/vàng trên sơ đồ)
            var ban = await _context.BanAns.FindAsync(datBan.BanAnId);
            if (ban != null)
            {
                ban.TrangThai = 1;
                _context.Update(ban);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(datBan);
    }

    // 3. Action xử lý nhận khách vào bàn (Bắt đầu phục vụ)
    public async Task<IActionResult> NhanBan(int id)
    {
        var datBan = await _context.DatBans.Include(d => d.BanAn).FirstOrDefaultAsync(d => d.Id == id);
        if (datBan != null)
        {
            datBan.TrangThai = 1; // Đang phục vụ
            if (datBan.BanAn != null)
            {
                datBan.BanAn.TrangThai = 1;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Khách đã vào bàn thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    // 4. TRANG THANH TOÁN (GET): Hiển thị hóa đơn cho nhân viên kiểm tra
    public async Task<IActionResult> Checkout(int? id)
    {
        if (id == null) return NotFound();

        var donDat = await _context.DatBans
            .Include(d => d.BanAn)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (donDat == null) return NotFound();

        // Lấy danh sách món ăn từ bảng ChiTietDatMons dựa trên BanAnId
        var danhSachMon = await _context.ChiTietDatMons
            .Include(c => c.MonAn)
            .Where(c => c.BanAnId == donDat.BanAnId)
            .ToListAsync();

        ViewBag.DanhSachMon = danhSachMon;

        return View(donDat);
    }

    // 5. XÁC NHẬN THANH TOÁN (POST): Chốt đơn và làm trống bàn
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
}