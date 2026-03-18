using Microsoft.EntityFrameworkCore;
using Laptrinnhweb.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace Laptrinnhweb.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<BanAn> BanAns { get; set; }
        public DbSet<DatBan> DatBans { get; set; }
        public DbSet<MonAn> MonAns { get; set; }
        public DbSet<ChiTietDatMon> ChiTietDatMons { get; set; }
    }
}
