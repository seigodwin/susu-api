using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Susu.Infrastructure.Data;

#nullable disable

namespace Susu.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Susu.Domain.Entities.Group", b =>
            {
                b.Property<Guid>("Id");
                b.Property<string>("Name").IsRequired();
                b.Property<string>("Description");
                b.Property<decimal>("WeeklyContribution").HasColumnType("numeric(18,2)");
                b.Property<DateTime>("CreatedAt");
                b.Property<DateTime>("UpdatedAt");
                b.Property<bool>("IsDeleted");
                b.HasKey("Id");
                b.ToTable("Groups");
            });

            modelBuilder.Entity("Susu.Domain.Entities.Member", b =>
            {
                b.Property<Guid>("Id");
                b.Property<string>("Name").IsRequired();
                b.Property<string>("Email");
                b.Property<Guid?>("GroupId");
                b.Property<DateTime>("CreatedAt");
                b.Property<DateTime>("UpdatedAt");
                b.Property<bool>("IsDeleted");
                b.HasKey("Id");
                b.HasIndex("GroupId");
                b.ToTable("Members");
            });

            modelBuilder.Entity("Susu.Domain.Entities.Contribution", b =>
            {
                b.Property<Guid>("Id");
                b.Property<Guid>("GroupId");
                b.Property<Guid>("MemberId");
                b.Property<int>("Week");
                b.Property<decimal>("Amount").HasColumnType("numeric(18,2)");
                b.Property<DateTime>("PaidAt");
                b.HasKey("Id");
                b.HasIndex("GroupId");
                b.HasIndex("MemberId");
                b.ToTable("Contributions");
            });

            modelBuilder.Entity("Susu.Domain.Entities.PayoutEntry", b =>
            {
                b.Property<Guid>("Id");
                b.Property<Guid>("GroupId");
                b.Property<int>("Week");
                b.Property<Guid>("RecipientId");
                b.Property<string>("RecipientName");
                b.Property<decimal>("Amount").HasColumnType("numeric(18,2)");
                b.Property<DateTime>("PaidAt");
                b.HasKey("Id");
                b.HasIndex("GroupId");
                b.ToTable("Payouts");
            });

            modelBuilder.Entity("Susu.Domain.Entities.Member", b =>
            {
                b.HasOne("Susu.Domain.Entities.Group")
                    .WithMany("Members")
                    .HasForeignKey("GroupId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity("Susu.Domain.Entities.Contribution", b =>
            {
                b.HasOne("Susu.Domain.Entities.Group")
                    .WithMany()
                    .HasForeignKey("GroupId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne("Susu.Domain.Entities.Member")
                    .WithMany()
                    .HasForeignKey("MemberId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity("Susu.Domain.Entities.PayoutEntry", b =>
            {
                b.HasOne("Susu.Domain.Entities.Group")
                    .WithMany()
                    .HasForeignKey("GroupId")
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
