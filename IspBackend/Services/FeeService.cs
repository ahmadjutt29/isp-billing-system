using IspBackend.Data;
using IspBackend.DTOs;
using IspBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace IspBackend.Services;

/// <summary>
/// Service interface for managing fee operations.
/// </summary>
public interface IFeeService
{
    /// <summary>
    /// Gets all fees.
    /// </summary>
    Task<IEnumerable<FeeDto>> GetAllFeesAsync();

    /// <summary>
    /// Gets a fee by ID.
    /// </summary>
    Task<FeeDto?> GetFeeByIdAsync(int id);

    /// <summary>
    /// Gets all fees for a specific user.
    /// </summary>
    Task<IEnumerable<FeeDto>> GetFeesByUserIdAsync(int userId);

    /// <summary>
    /// Creates a new fee.
    /// </summary>
    Task<FeeDto> CreateFeeAsync(CreateFeeDto createFeeDto);

    /// <summary>
    /// Marks a fee as paid.
    /// </summary>
    Task<FeeDto?> MarkFeeAsPaidAsync(int feeId, DateTime? paymentDate = null);

    /// <summary>
    /// Updates an existing fee.
    /// </summary>
    Task<FeeDto?> UpdateFeeAsync(int id, CreateFeeDto updateDto);

    /// <summary>
    /// Deletes a fee.
    /// </summary>
    Task<bool> DeleteFeeAsync(int id);

    /// <summary>
    /// Calculates total income (sum of paid fees).
    /// </summary>
    Task<decimal> GetTotalIncomeAsync();

    /// <summary>
    /// Calculates total unpaid amount.
    /// </summary>
    Task<decimal> GetTotalUnpaidAsync();

    /// <summary>
    /// Gets income summary with paid, unpaid, and overdue amounts.
    /// </summary>
    Task<IncomeSummaryDto> GetIncomeSummaryAsync();

    /// <summary>
    /// Checks if a user exists.
    /// </summary>
    Task<bool> UserExistsAsync(int userId);

    /// <summary>
    /// Checks if a fee belongs to a specific user.
    /// </summary>
    Task<bool> FeeOwnershipAsync(int feeId, int userId);

    /// <summary>
    /// Gets a fee entity with user navigation property for PDF generation.
    /// </summary>
    Task<Fee?> GetFeeWithUserAsync(int feeId);

    /// <summary>
    /// Gets monthly income breakdown for a given year.
    /// </summary>
    Task<IEnumerable<MonthlyIncomeDto>> GetMonthlyIncomeAsync(int year);
}

/// <summary>
/// Implementation of IFeeService for managing fee operations.
/// </summary>
public class FeeService : IFeeService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeeService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public FeeService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeeDto>> GetAllFeesAsync()
    {
        return await _context.Fees
            .Include(f => f.User)
            .Select(f => MapToDto(f))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FeeDto?> GetFeeByIdAsync(int id)
    {
        var fee = await _context.Fees
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id);

        return fee != null ? MapToDto(fee) : null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeeDto>> GetFeesByUserIdAsync(int userId)
    {
        return await _context.Fees
            .Include(f => f.User)
            .Where(f => f.UserId == userId)
            .Select(f => MapToDto(f))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FeeDto> CreateFeeAsync(CreateFeeDto createFeeDto)
    {
        var fee = new Fee
        {
            UserId = createFeeDto.UserId,
            Amount = createFeeDto.Amount,
            DueDate = createFeeDto.DueDate,
            Description = createFeeDto.Description,
            Paid = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Fees.Add(fee);
        await _context.SaveChangesAsync();

        // Load user for response
        await _context.Entry(fee).Reference(f => f.User).LoadAsync();

        return MapToDto(fee);
    }

    /// <inheritdoc />
    public async Task<FeeDto?> MarkFeeAsPaidAsync(int feeId, DateTime? paymentDate = null)
    {
        var fee = await _context.Fees
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == feeId);

        if (fee == null)
        {
            return null;
        }

        fee.Paid = true;
        fee.PaymentDate = paymentDate ?? DateTime.UtcNow;
        fee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(fee);
    }

    /// <inheritdoc />
    public async Task<FeeDto?> UpdateFeeAsync(int id, CreateFeeDto updateDto)
    {
        var fee = await _context.Fees
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fee == null)
        {
            return null;
        }

        fee.Amount = updateDto.Amount;
        fee.DueDate = updateDto.DueDate;
        fee.Description = updateDto.Description;
        fee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(fee);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFeeAsync(int id)
    {
        var fee = await _context.Fees.FindAsync(id);

        if (fee == null)
        {
            return false;
        }

        _context.Fees.Remove(fee);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalIncomeAsync()
    {
        return await _context.Fees
            .Where(f => f.Paid)
            .SumAsync(f => f.Amount);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalUnpaidAsync()
    {
        return await _context.Fees
            .Where(f => !f.Paid)
            .SumAsync(f => f.Amount);
    }

    /// <inheritdoc />
    public async Task<IncomeSummaryDto> GetIncomeSummaryAsync()
    {
        var fees = await _context.Fees.ToListAsync();

        var totalPaid = fees.Where(f => f.Paid).Sum(f => f.Amount);
        var totalUnpaid = fees.Where(f => !f.Paid).Sum(f => f.Amount);
        var paidCount = fees.Count(f => f.Paid);
        var unpaidCount = fees.Count(f => !f.Paid);
        var overdueCount = fees.Count(f => !f.Paid && f.DueDate < DateTime.UtcNow);
        var overdueAmount = fees.Where(f => !f.Paid && f.DueDate < DateTime.UtcNow).Sum(f => f.Amount);

        return new IncomeSummaryDto
        {
            TotalPaidAmount = totalPaid,
            TotalUnpaidAmount = totalUnpaid,
            TotalAmount = totalPaid + totalUnpaid,
            PaidFeesCount = paidCount,
            UnpaidFeesCount = unpaidCount,
            TotalFeesCount = paidCount + unpaidCount,
            OverdueFeesCount = overdueCount,
            OverdueAmount = overdueAmount,
            CollectionRate = (paidCount + unpaidCount) > 0
                ? Math.Round((decimal)paidCount / (paidCount + unpaidCount) * 100, 2)
                : 0,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<bool> UserExistsAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    /// <inheritdoc />
    public async Task<bool> FeeOwnershipAsync(int feeId, int userId)
    {
        return await _context.Fees.AnyAsync(f => f.Id == feeId && f.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<Fee?> GetFeeWithUserAsync(int feeId)
    {
        return await _context.Fees
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == feeId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MonthlyIncomeDto>> GetMonthlyIncomeAsync(int year)
    {
        var paidFees = await _context.Fees
            .Where(f => f.Paid && f.PaymentDate.HasValue && f.PaymentDate.Value.Year == year)
            .ToListAsync();

        var monthlyIncome = paidFees
            .GroupBy(f => f.PaymentDate!.Value.Month)
            .Select(g => new MonthlyIncomeDto
            {
                Year = year,
                Month = g.Key,
                MonthName = new DateTime(year, g.Key, 1).ToString("MMMM"),
                TotalAmount = g.Sum(f => f.Amount),
                FeesCount = g.Count()
            })
            .OrderBy(m => m.Month)
            .ToList();

        // Fill in missing months with zero
        var allMonths = Enumerable.Range(1, 12)
            .Select(m => monthlyIncome.FirstOrDefault(mi => mi.Month == m) ?? new MonthlyIncomeDto
            {
                Year = year,
                Month = m,
                MonthName = new DateTime(year, m, 1).ToString("MMMM"),
                TotalAmount = 0,
                FeesCount = 0
            })
            .ToList();

        return allMonths;
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Fee entity to FeeDto.
    /// </summary>
    /// <param name="fee">The fee entity.</param>
    /// <returns>The fee DTO.</returns>
    private static FeeDto MapToDto(Fee fee)
    {
        return new FeeDto
        {
            Id = fee.Id,
            UserId = fee.UserId,
            Username = fee.User?.Username,
            Amount = fee.Amount,
            DueDate = fee.DueDate,
            Paid = fee.Paid,
            PaymentDate = fee.PaymentDate,
            Description = fee.Description,
            CreatedAt = fee.CreatedAt
        };
    }

    #endregion
}
