namespace Laptrinnhweb.Models
{
    public class ChiTietDatMon
    {
        public int Id { get; set; }

        // Liên kết với Bàn ăn
        public int BanAnId { get; set; }
        public virtual BanAn? BanAn { get; set; }

        // Liên kết với Món ăn
        public int MonAnId { get; set; }
        public virtual MonAn? MonAn { get; set; }

        public int? DatBanId { get; set; }
        public virtual DatBan? DatBan { get; set; }
        public int SoLuong { get; set; }

    }
}
