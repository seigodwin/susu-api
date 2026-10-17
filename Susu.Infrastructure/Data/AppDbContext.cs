using Microsoft.EntityFrameworkCore;
using Susu.Domain.Entities;

namespace Susu.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<Member> Members { get; set; } = null!;
        public DbSet<Contribution> Contributions { get; set; } = null!;
        public DbSet<PayoutEntry> Payouts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Group>(b =>
            {
                b.HasKey(g => g.Id);
                b.Property(g => g.Name).IsRequired();
                b.Property(g => g.Description).HasMaxLength(1000);
                b.Property(g => g.WeeklyContribution).HasColumnType("numeric(18,2)");
                b.HasMany(g => g.Members).WithOne(m => m.Group).HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Member>(b =>
            {
                b.HasKey(m => m.Id);
                b.Property(m => m.Name).IsRequired();
                b.Property(m => m.Email).HasMaxLength(256);
            });

            modelBuilder.Entity<Contribution>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Amount).HasColumnType("numeric(18,2)");
                b.Property(c => c.PaidAt).IsRequired();
            });

            modelBuilder.Entity<PayoutEntry>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Amount).HasColumnType("numeric(18,2)");
                b.Property(p => p.RecipientName).HasMaxLength(256);
            });
        }
    }
}
