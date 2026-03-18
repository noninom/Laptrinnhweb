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

    // 1. Xem danh sách khách (Đã sửa lỗi hiển thị tên bàn)
    public async Task<IActionResult> Index()
    {
        var danhSachDat = await _context.DatBans
            .Include(d => d.BanAn) // Nạp thông tin bàn để hiện "Bàn 01", "Bàn 02"
            .OrderByDescending(d => d.NgayDat)
            .ToListAsync();
        return View(danhSachDat);
    }

    // 2. Trang nhập thông tin khách
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
        // Gán trạng thái mặc định cho đơn đặt
        datBan.TrangThai = 0;

        if (ModelState.IsValid)
        {
            _context.Add(datBan);

            // BƯỚC QUAN TRỌNG: Lúc này mới chính thức khóa bàn (Màu đỏ)
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

    // 3. Thanh toán / Trả bàn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int id)
    {
        var donDat = await _context.DatBans.FindAsync(id);
        if (donDat != null)
        {
            var ban = await _context.BanAns.FindAsync(donDat.BanAnId);
            if (ban != null)
            {
                ban.TrangThai = 0; // Mở khóa bàn (Màu xanh)
                _context.Update(ban);

                // Xóa cả món ăn đã gọi của bàn này để sạch dữ liệu cho khách sau
                var chiTietMon = _context.ChiTietDatMons.Where(c => c.BanAnId == ban.Id);
                _context.ChiTietDatMons.RemoveRange(chiTietMon);
            }

            _context.DatBans.Remove(donDat);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    // 4. Xem chi tiết đơn đặt (Thông tin khách + Món ăn đã gọi)
    public async Task<IActionResult> Details(int id)
    {
        // Tìm đơn đặt bàn theo ID
        var donDat = await _context.DatBans
            .Include(d => d.BanAn)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (donDat == null) return NotFound();

        // Lấy danh sách món ăn mà bàn này đã đặt
        var danhSachMon = await _context.ChiTietDatMons
            .Include(c => c.MonAn)
            .Where(c => c.BanAnId == donDat.BanAnId)
            .ToListAsync();

        // Truyền danh sách món qua ViewBag để hiển thị ở View
        ViewBag.DanhSachMon = danhSachMon;

        return View(donDat);
    }
    
    // Action xử lý nhận khách vào bàn
    public async Task<IActionResult> NhanBan(int id)
    {
        var datBan = await _context.DatBans.Include(d => d.BanAn).FirstOrDefaultAsync(d => d.Id == id);
        if (datBan != null)
        {
            datBan.TrangThai = 1; // 1: Đang phục vụ

            if (datBan.BanAn != null)
            {
                // Quan trọng: Đổi trạng thái bàn thành 1 (Đã có khách) 
                // nhưng vẫn phải để link "Chọn bàn" hoạt động hoặc dẫn thẳng vào Menu
                datBan.BanAn.TrangThai = 1;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Khách đã vào bàn. Bạn có thể gọi thêm món!";
        }
        return RedirectToAction(nameof(Index));
    }
}