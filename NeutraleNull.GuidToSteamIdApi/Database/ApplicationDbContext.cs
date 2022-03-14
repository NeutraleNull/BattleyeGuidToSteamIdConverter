using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NeutraleNull.GuidToSteamIdApi.Database
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string dbLocation;

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<BattleyeGuidSteamIdTuple> BattleyeGuidSteamIdLookupTable { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
        }

    }

    public class BattleyeGuidSteamIdTuple
    {
        [Key]
        public string Guid { get; set; }
        public long SteamId64 { get; set; }
    }
}
