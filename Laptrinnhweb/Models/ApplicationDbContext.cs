using Microsoft.EntityFrameworkCore;

namespace Laptrinnhweb.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<BanAn> BanAns { get; set; }
        public DbSet<DatBan> DatBans { get; set; }
        public DbSet<MonAn> MonAns { get; set; }
        public DbSet<ChiTietDatMon> ChiTietDatMons { get; set; }
    }
}
