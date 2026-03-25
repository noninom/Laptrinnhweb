using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Laptrinnhweb.Models
{
    public class DatBan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        public string TenKhachHang { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SoDienThoai { get; set; }

        // Đổi tên thành NgayDat để khớp với file Index.cshtml và Create.cshtml của bạn
        public DateTime NgayDat { get; set; }
        public string? GhiChuGopBan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ đến dự kiến")]
        public DateTime GioDenDuyKien { get; set; } // Thời gian khách hẹn đến

        // Thời gian hết hạn giữ bàn (thường bằng GioDenDuyKien + 2 tiếng)
        public DateTime GioHetHan => GioDenDuyKien.AddHours(2);

        // Liên kết với bàn
        public int BanAnId { get; set; }
        public virtual BanAn? BanAn { get; set; }

        // Trạng thái: 0 (Đang chờ), 1 (Đã nhận bàn/Thanh toán)
        public int TrangThai { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public virtual ICollection<ChiTietDatBan> ChiTietDatBans { get; set; } = new List<ChiTietDatBan>();
    }
}