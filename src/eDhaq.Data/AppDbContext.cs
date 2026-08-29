using eDhaq.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Domain sets
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Village> Villages => Set<Village>();
    public DbSet<SubVillage> SubVillages => Set<SubVillage>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<LaundryService> LaundryServices => Set<LaundryService>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderTracking> OrderTrackings => Set<OrderTracking>();
    public DbSet<DriverAssignment> DriverAssignments => Set<DriverAssignment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables to cleaner names
        builder.Entity<ApplicationUser>().ToTable("Users");

        // Customer → User (1:1)
        builder.Entity<Customer>()
            .HasOne(c => c.User)
            .WithOne(u => u.Customer)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Driver → User (1:1)
        builder.Entity<Driver>()
            .HasOne(d => d.User)
            .WithOne(u => u.Driver)
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Employee → User (1:1)
        builder.Entity<Employee>()
            .HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Address → Customer
        builder.Entity<Address>()
            .HasOne(a => a.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Village hierarchy
        builder.Entity<Village>()
            .HasOne(v => v.City)
            .WithMany(c => c.Villages)
            .HasForeignKey(v => v.CityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SubVillage>()
            .HasOne(sv => sv.Village)
            .WithMany(v => v.SubVillages)
            .HasForeignKey(sv => sv.VillageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Address>()
            .HasOne(a => a.Village)
            .WithMany(v => v.Addresses)
            .HasForeignKey(a => a.VillageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Address>()
            .HasOne(a => a.SubVillage)
            .WithMany(sv => sv.Addresses)
            .HasForeignKey(a => a.SubVillageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Order → Customer
        builder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order → PickupAddress (no cascade to avoid multiple cascade paths)
        builder.Entity<Order>()
            .HasOne(o => o.PickupAddress)
            .WithMany()
            .HasForeignKey(o => o.PickupAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order → DeliveryAddress
        builder.Entity<Order>()
            .HasOne(o => o.DeliveryAddress)
            .WithMany()
            .HasForeignKey(o => o.DeliveryAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order → Payment (1:1)
        builder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order → Invoice (1:1)
        builder.Entity<Invoice>()
            .HasOne(i => i.Order)
            .WithOne(o => o.Invoice)
            .HasForeignKey<Invoice>(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order → Review (1:1)
        builder.Entity<Review>()
            .HasOne(r => r.Order)
            .WithOne(o => o.Review)
            .HasForeignKey<Review>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Review → Customer
        builder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Wallet → Customer (1:1)
        builder.Entity<Wallet>()
            .HasOne(w => w.Customer)
            .WithOne()
            .HasForeignKey<Wallet>(w => w.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
        builder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        builder.Entity<AppSetting>().HasIndex(s => s.Key).IsUnique();
        builder.Entity<AuditLog>().HasIndex(a => a.CreatedAt);
        builder.Entity<Village>().HasIndex(v => new { v.CityId, v.Name }).IsUnique();
        builder.Entity<SubVillage>().HasIndex(sv => new { sv.VillageId, sv.Name }).IsUnique();

        // Soft-delete / value conversions (enums stored as ints by default – fine)
    }
}
