using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Net;

namespace TelephoneCallExample
{
    public class AppDbContext : DbContext
    {
        public DbSet<PhoneCall> PhoneCalls { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=phonecalls.db");
        }
    }
}