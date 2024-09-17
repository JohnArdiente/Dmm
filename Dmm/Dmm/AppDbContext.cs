using Dmm.Models;
using Microsoft.EntityFrameworkCore;

namespace Dmm
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)

        {
        }

        public DbSet<Entry> Entry { get; set; }
        public DbSet<EntryData> EntryData { get; set; }
        public DbSet<GiveAndTake> GiveAndTake { get; set; }
        public DbSet<NoFightRequest> NoFightRequests { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Title> Title { get; set; }
        public DbSet<Token> Token { get; set; }
        public DbSet<ManualMatches> ManualMatches { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Match>()
            .HasOne(m => m.Token)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TokenId);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GiveAndTake>().HasData(
                new GiveAndTake { GiveAndTakeId = 1, EventId = "TestEventId", GtValue = 3, PmValue = 25 }
            );

            modelBuilder.Entity<Title>().HasData(
                new Title { Id = 1, TitleName = "ADD EVENT TITLE" }
            );
        }
    }
}
