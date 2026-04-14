using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TriaYazarKasaRestApi.Data.Acces.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        private const string ConnectionString = "Data Source=C:\\TriaYazarKasaDataBase\\TriaPos.db";

        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite(ConnectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
