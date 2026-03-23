using System.ComponentModel.DataAnnotations.Schema;

namespace Laptrinnhweb.Models
{
    public class BanAn
    {
        public int Id { get; set; }
        public string SoBan { get; set; } // Ví dụ: Bàn 01, Bàn 02
        public int SoChoNgoi { get; set; }
        public int TrangThai { get; set; } // 0: Trống (Xanh), 1: Đã đặt/Có khách (Đỏ)
        public virtual ICollection<DatBan> DatBans { get; set; } = new List<DatBan>();
        [NotMapped]
        public bool IsOwner { get; set; }

        [NotMapped]
        public DatBan? ActiveDatBan { get; set; }
    }
}
