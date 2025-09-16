using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");
        if (date.Year < 1900 || date.Year > DateTime.UtcNow.Year + 50)
        {
            throw new ArgumentException("The provided date is outside the acceptable range.");
        }


        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
                         p.EndDate >= date  
        );
    }

    public async Task<ClaimDto> RegisterClaimAsync(long carId, ClaimDto request)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");



        var newClaim = new InsuranceClaim
        {
            CarId = (int)carId,
            ClaimDate = request.ClaimDate.GetValueOrDefault(),
            Description = request.Description,
            Amount = request.Amount.GetValueOrDefault()
        };

        await _db.Claim.AddAsync(newClaim);
        await _db.SaveChangesAsync();

        return new ClaimDto(newClaim.Id, newClaim.CarId, newClaim.ClaimDate, newClaim.Description, newClaim.Amount);
    }

    public async Task<CarHistoryResponse> GetCarHistoryAsync(long carId)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        var policies = await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new HistoryEvent
            {
                EventDate = p.StartDate,
                EventType = "Policy Started",
                Description = $"Policy with {p.Provider} started. Valid until {p.EndDate:yyyy-MM-dd}."
            })
            .ToListAsync();

        var claims = await _db.Claim
            .Where(c => c.CarId == carId)
            .Select(c => new HistoryEvent
            {
                EventDate = c.ClaimDate,
                EventType = "Claim Filed",
                Description = $"Claim for ${c.Amount}: {c.Description}"
            })
            .ToListAsync();

        var timeline = policies
            .Concat(claims)
            .OrderBy(e => e.EventDate)
            .ToList();

        return new CarHistoryResponse { Timeline = timeline };
    }
}
