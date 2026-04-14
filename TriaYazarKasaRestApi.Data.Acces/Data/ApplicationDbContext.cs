// using Microsoft.EntityFrameworkCore;
// using TriaYazarKasaRestApi.Data.Acces.Models;

// namespace TriaYazarKasaRestApi.Data.Acces.Data
// {
//         public class ApplicationDbContext : DbContext
//     {
//         public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
//         {
//         }

//         public DbSet<HuginOperationLog> HuginOperationLogs { get; set; }
//     }
// }


using Microsoft.EntityFrameworkCore;
using TriaYazarKasaRestApi.Data.Acces.Models;
namespace TriaYazarKasaRestApi.Data.Acces.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<HuginOperationLog> HuginOperationLogs { get; set; }
        public DbSet<BekoBasketOperationRecord> BekoBasketOperations { get; set; }
    }
}