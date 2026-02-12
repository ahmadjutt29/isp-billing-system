using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IspBackend.Data;
using IspBackend.Models;

namespace IspBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class PayRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PayRequestsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/payrequests
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _db.PayRequests
            .Include(p => p.Fee)
            .ThenInclude(f => f.User)
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync();
        return Ok(requests.Select(r => new {
            r.Id,
            r.FeeId,
            r.TransactionId,
            r.PayeeName,
            r.Amount,
            r.Approved,
            r.RequestedAt,
            r.ApprovedAt,
            User = r.Fee != null && r.Fee.User != null ? new { r.Fee.User.Id, r.Fee.User.Username, r.Fee.User.Email } : null,
            FeeDescription = r.Fee?.Description,
            FeePaid = r.Fee?.Paid
        }));
    }

    // POST: api/payrequests/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await _db.PayRequests.Include(p => p.Fee).FirstOrDefaultAsync(p => p.Id == id);
        if (request == null)
            return NotFound(new { message = "Pay request not found" });
        if (request.Approved)
            return BadRequest(new { message = "Already approved" });
        if (request.Fee == null)
            return BadRequest(new { message = "Fee not found" });
        // Mark as approved
        request.Approved = true;
        request.ApprovedAt = DateTime.UtcNow;
        // Mark fee as paid
        request.Fee.Paid = true;
        request.Fee.PaymentDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Payment approved and fee marked as paid" });
    }
}
