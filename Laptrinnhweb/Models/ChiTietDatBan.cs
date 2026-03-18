using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laptrinnhweb.Models // Đảm bảo namespace này khớp với các file khác của bạn
{
    public class ChiTietDatBan
    {
        [Key]
        public int Id { get; set; }

        public int DatBanId { get; set; }
        [ForeignKey("DatBanId")]
        public virtual DatBan? DatBan { get; set; }

        public int MonAnId { get; set; }
        [ForeignKey("MonAnId")]
        public virtual MonAn? MonAn { get; set; }

        public int SoLuong { get; set; }
    }
}