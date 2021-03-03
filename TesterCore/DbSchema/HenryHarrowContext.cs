using System;
using HenryHarrow.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Test.DbSchema
{
    public partial class HenryHarrowContext : DbContextWithSoftDelete
    {
        public HenryHarrowContext()
        {
        }

        public HenryHarrowContext(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Order> Order { get; set; }
        public virtual DbSet<Orderdetail> Orderdetail { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "C");

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("order", "ref");

                entity.Property(e => e.OrderId)
                    .HasColumnName("order_id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.Lcv).HasColumnName("lcv");

                entity.Property(e => e.Ordernumber)
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnName("ordernumber");
            });

            modelBuilder.Entity<Orderdetail>(entity =>
            {
                entity.ToTable("orderdetail", "ref");

                entity.HasIndex(e => e.OrderId, "ndx_orderdetail_order_id");

                entity.Property(e => e.OrderdetailId)
                    .HasColumnName("orderdetail_id")
                    .UseIdentityAlwaysColumn();

                entity.Property(e => e.Description)
                    .HasMaxLength(32)
                    .HasColumnName("description");

                entity.Property(e => e.Lcv).HasColumnName("lcv");

                entity.Property(e => e.OrderId).HasColumnName("order_id");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.Orderdetail)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_orderdetail_order");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
