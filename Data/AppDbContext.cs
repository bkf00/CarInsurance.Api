using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<InsurancePolicy> Policies => Set<InsurancePolicy>();
    public DbSet<InsuranceClaim> Claim => Set<InsuranceClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>()
            .HasIndex(c => c.Vin)
            .IsUnique(true);

        modelBuilder.Entity<InsurancePolicy>()
            .Property(p => p.StartDate)
            .IsRequired();

        modelBuilder.Entity<InsuranceClaim>()
             .HasOne(c => c.Car)
             .WithMany()
             .HasForeignKey(c => c.CarId)
             .IsRequired();

    }
}

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Owners.Any()) return;

        var ana = new Owner { Name = "Ana Pop", Email = "ana.pop@example.com" };
        var bogdan = new Owner { Name = "Bogdan Ionescu", Email = "bogdan.ionescu@example.com" };
        var valentin = new Owner { Name = "Valentin Florescu", Email = "valentin.florescu@example.com" };
        db.Owners.AddRange(ana, bogdan, valentin);
        db.SaveChanges();

        var car1 = new Car { Vin = "VIN12345", Make = "Dacia", Model = "Logan", YearOfManufacture = 2018, OwnerId = ana.Id };
        var car2 = new Car { Vin = "VIN67890", Make = "VW", Model = "Golf", YearOfManufacture = 2021, OwnerId = bogdan.Id };
        var car3 = new Car { Vin = "VIN53490", Make = "VWw", Model = "Passat", YearOfManufacture = 2023, OwnerId = valentin.Id };
        db.Cars.AddRange(car1, car2, car3);
        db.SaveChanges();

        db.Policies.AddRange(
            new InsurancePolicy { CarId = car1.Id, Provider = "Allianz", StartDate = new DateOnly(2024,1,1), EndDate = new DateOnly(2024,12,31) , IsExpirationNotified = true },
            new InsurancePolicy { CarId = car1.Id, Provider = "Groupama", StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025, 12, 31), IsExpirationNotified = false },
            new InsurancePolicy { CarId = car2.Id, Provider = "Allianz", StartDate = new DateOnly(2025,3,1), EndDate = new DateOnly(2025,9,30), IsExpirationNotified = false }
        );
        db.SaveChanges();

        db.Claim.AddRange(
            new InsuranceClaim { CarId = car1.Id, ClaimDate = new DateOnly(2025, 1, 1), Description = "Accident", Amount = 2000 },
            new InsuranceClaim { CarId = car1.Id, ClaimDate = new DateOnly(2025, 2, 1), Description = "Casco", Amount = 3000 }
        );
        db.SaveChanges();
    }
}
