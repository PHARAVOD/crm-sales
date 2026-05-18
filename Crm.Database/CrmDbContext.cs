using Microsoft.EntityFrameworkCore;
using Crm.Database.Entities;

namespace Crm.Database;

public class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }
    
    public DbSet<Product> Products { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<CrmTask> Tasks { get; set; }  // ← ИЗМЕНЕНО
    public DbSet<Report> Reports { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<OrderLine>()
            .Property(ol => ol.UnitPrice)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Name);
        
        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.Email)
            .IsUnique();
        
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.CreatedAt);
    }
}