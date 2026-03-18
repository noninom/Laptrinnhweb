namespace Laptrinnhweb.Models
{
    public class MonAn
    {
        public int Id { get; set; }

        public string? TenMon { get; set; } // Dấu ? giúp hết lỗi cảnh báo

        public string? MoTa { get; set; }

        public decimal Gia { get; set; }

        public string? HinhAnh { get; set; }

        public string? Loai { get; set; }
    }
}
