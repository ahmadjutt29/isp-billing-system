using System.Security.Claims;
using IspBackend.DTOs;
using IspBackend.Models;
using IspBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace IspBackend.Controllers;

/// <summary>
/// Controller for managing fees/billing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]


public class FeesController : ControllerBase
{


    /// <summary>
    /// Submits a payment request for a fee (client-side, requires approval).
    /// </summary>
    /// <param name="id">The fee ID.</param>
    /// <param name="dto">The payment request data.</param>
    /// <returns>Action result.</returns>
    [HttpPost("{id}/pay-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitPayRequest(int id, [FromBody] DTOs.PayRequestDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(new { message = "User not authenticated" });

        var fee = await _feeService.GetFeeByIdAsync(id);
        if (fee == null)
            return NotFound(new { message = "Fee not found" });

        // Only owner can submit pay request
        if (fee.UserId != currentUserId && !IsAdminOrOwner(fee.UserId))
            return Forbid();

        // Validate
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Save pay request
        using (var scope = HttpContext.RequestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var payRequest = new Models.PayRequest
            {
                FeeId = id,
                TransactionId = dto.TransactionId,
                PayeeName = dto.PayeeName,
                Amount = dto.Amount,
                RequestedAt = DateTime.UtcNow,
                Approved = false
            };
            db.PayRequests.Add(payRequest);
            await db.SaveChangesAsync();
        }
        return Ok(new { message = "Payment request submitted for approval" });
    }
    private readonly IFeeService _feeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeesController"/> class.
    /// </summary>
    /// <param name="feeService">The fee service.</param>
    public FeesController(IFeeService feeService)
    {
        _feeService = feeService;
    }

    /// <summary>
    /// Gets all fees. Admin only.
    /// </summary>
    /// <returns>List of all fees.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<FeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetAllFees()
    {
        var fees = await _feeService.GetAllFeesAsync();
        return Ok(fees);
    }

    /// <summary>
    /// Gets a specific fee by ID.
    /// Clients can only view their own fees; Admins can view any fee.
    /// </summary>
    /// <param name="id">The fee ID.</param>
    /// <returns>The fee if found and authorized.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FeeDto>> GetFee(int id)
    {
        var fee = await _feeService.GetFeeByIdAsync(id);

        if (fee == null)
        {
            return NotFound(new { message = "Fee not found" });
        }

        // Check authorization: Admin can see all, Client can only see their own
        if (!IsAdminOrOwner(fee.UserId))
        {
            return Forbid();
        }

        return Ok(fee);
    }

    /// <summary>
    /// Gets all fees for a specific user.
    /// Clients can only view their own fees; Admins can view any user's fees.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of fees for the user.</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<FeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetFeesByUser(int userId)
    {
        // Check authorization: Admin can see all, Client can only see their own
        if (!IsAdminOrOwner(userId))
        {
            return Forbid();
        }

        var fees = await _feeService.GetFeesByUserIdAsync(userId);
        return Ok(fees);
    }

    /// <summary>
    /// Gets fees for the currently authenticated user.
    /// </summary>
    /// <returns>List of fees for the current user.</returns>
    [HttpGet("my-fees")]
    [ProducesResponseType(typeof(IEnumerable<FeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FeeDto>>> GetMyFees()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var fees = await _feeService.GetFeesByUserIdAsync(currentUserId.Value);
        return Ok(fees);
    }

    /// <summary>
    /// Creates a new fee. Admin only.
    /// </summary>
    /// <param name="createFeeDto">The fee creation data.</param>
    /// <returns>The created fee.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeeDto>> CreateFee([FromBody] CreateFeeDto createFeeDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verify user exists
        var userExists = await _feeService.UserExistsAsync(createFeeDto.UserId);
        if (!userExists)
        {
            return NotFound(new { message = "User not found" });
        }

        var feeDto = await _feeService.CreateFeeAsync(createFeeDto);
        return CreatedAtAction(nameof(GetFee), new { id = feeDto.Id }, feeDto);
    }

    /// <summary>
    /// Marks a fee as paid.
    /// Clients can pay their own fees; Admins can pay any fee.
    /// </summary>
    /// <param name="id">The fee ID.</param>
    /// <param name="payFeeDto">Optional payment details.</param>
    /// <returns>The updated fee.</returns>
    [HttpPut("{id}/pay")]
    [ProducesResponseType(typeof(FeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeeDto>> PayFee(int id, [FromBody] PayFeeDto? payFeeDto = null)
    {
        var fee = await _feeService.GetFeeByIdAsync(id);

        if (fee == null)
        {
            return NotFound(new { message = "Fee not found" });
        }

        // Check authorization: Admin can pay any, Client can only pay their own
        if (!IsAdminOrOwner(fee.UserId))
        {
            return Forbid();
        }

        if (fee.Paid)
        {
            return BadRequest(new { message = "Fee is already paid" });
        }

        var updatedFee = await _feeService.MarkFeeAsPaidAsync(id, payFeeDto?.PaymentDate);

        if (updatedFee == null)
        {
            return NotFound(new { message = "Fee not found" });
        }

        return Ok(updatedFee);
    }

    /// <summary>
    /// Updates a fee. Admin only.
    /// </summary>
    /// <param name="id">The fee ID.</param>
    /// <param name="updateDto">The update data.</param>
    /// <returns>The updated fee.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeeDto>> UpdateFee(int id, [FromBody] CreateFeeDto updateDto)
    {
        var updatedFee = await _feeService.UpdateFeeAsync(id, updateDto);

        if (updatedFee == null)
        {
            return NotFound(new { message = "Fee not found" });
        }

        return Ok(updatedFee);
    }

    /// <summary>
    /// Deletes a fee. Admin only.
    /// </summary>
    /// <param name="id">The fee ID.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFee(int id)
    {
        var deleted = await _feeService.DeleteFeeAsync(id);

        if (!deleted)
        {
            return NotFound(new { message = "Fee not found" });
        }

        return NoContent();
    }

    #region Helper Methods

    /// <summary>
    /// Gets the current user's ID from claims.
    /// </summary>
    /// <returns>The current user's ID or null.</returns>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Gets the current user's role from claims.
    /// </summary>
    /// <returns>The current user's role or null.</returns>
    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Checks if the current user is an Admin or the owner of the resource.
    /// </summary>
    /// <param name="resourceUserId">The user ID associated with the resource.</param>
    /// <returns>True if authorized.</returns>
    private bool IsAdminOrOwner(int resourceUserId)
    {
        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();

        if (currentRole == "Admin")
        {
            return true;
        }

        return currentUserId.HasValue && currentUserId.Value == resourceUserId;
    }

    #endregion

    #region Invoice Generation

    /// <summary>
    /// Generates a PDF invoice for a specific fee.
    /// Clients can only download their own invoices. Admins can download any invoice.
    /// </summary>
    /// <param name="feeId">The fee ID.</param>
    /// <returns>PDF file download.</returns>
    [HttpGet("{feeId}/invoice")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadInvoice(int feeId)
    {
        var fee = await _feeService.GetFeeWithUserAsync(feeId);

        if (fee == null)
        {
            return NotFound(new { message = "Fee not found" });
        }

        // Check authorization: Admin can download any, Client can only download their own
        if (!IsAdminOrOwner(fee.UserId))
        {
            return Forbid();
        }

        var pdfBytes = GenerateInvoicePdf(fee);

        var fileName = $"Invoice_{feeId}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Generates PDF invoice bytes for a fee.
    /// </summary>
    /// <param name="fee">The fee to generate invoice for.</param>
    /// <returns>PDF file as byte array.</returns>
    private static byte[] GenerateInvoicePdf(Fee fee)
    {
        // Create a new PDF document
        var document = new PdfDocument();
        document.Info.Title = $"Invoice #{fee.Id}";
        document.Info.Author = "ISP Billing System";

        // Add a page
        var page = document.AddPage();
        page.Width = XUnit.FromInch(8.5);
        page.Height = XUnit.FromInch(11);

        var gfx = XGraphics.FromPdfPage(page);

        // Define fonts - using Liberation Sans (installed in container, similar to Helvetica)
        const string fontFamily = "Liberation Sans";
        var titleFont = new XFont(fontFamily, 24, XFontStyle.Bold);
        var headerFont = new XFont(fontFamily, 14, XFontStyle.Bold);
        var normalFont = new XFont(fontFamily, 12, XFontStyle.Regular);
        var smallFont = new XFont(fontFamily, 10, XFontStyle.Regular);

        // Define colors
        var primaryColor = XColor.FromArgb(41, 128, 185); // Blue
        var lightGray = XColor.FromArgb(245, 245, 245);
        var paidColor = XColor.FromArgb(39, 174, 96); // Green
        var unpaidColor = XColor.FromArgb(231, 76, 60); // Red

        double yPosition = 50;
        double leftMargin = 50;
        double rightMargin = page.Width.Point - 50;

        // Header - Company Name
        gfx.DrawString("ISP BILLING SYSTEM", titleFont, new XSolidBrush(primaryColor),
            new XRect(leftMargin, yPosition, page.Width.Point - 100, 30), XStringFormats.TopLeft);

        yPosition += 40;

        // Invoice Title
        gfx.DrawString("INVOICE", new XFont(fontFamily, 18, XFontStyle.Bold), XBrushes.Black,
            new XRect(leftMargin, yPosition, page.Width.Point - 100, 25), XStringFormats.TopLeft);

        // Invoice Number on the right
        gfx.DrawString($"#{fee.Id.ToString().PadLeft(6, '0')}", new XFont(fontFamily, 18, XFontStyle.Bold), XBrushes.Black,
            new XRect(leftMargin, yPosition, page.Width.Point - 100, 25), XStringFormats.TopRight);

        yPosition += 40;

        // Horizontal line
        gfx.DrawLine(new XPen(primaryColor, 2), leftMargin, yPosition, rightMargin, yPosition);

        yPosition += 20;

        // Invoice Date
        gfx.DrawString("Invoice Date:", headerFont, XBrushes.Black,
            new XPoint(leftMargin, yPosition));
        gfx.DrawString(fee.CreatedAt.ToString("MMMM dd, yyyy"), normalFont, XBrushes.Black,
            new XPoint(leftMargin + 120, yPosition));

        yPosition += 25;

        // Due Date
        gfx.DrawString("Due Date:", headerFont, XBrushes.Black,
            new XPoint(leftMargin, yPosition));
        gfx.DrawString(fee.DueDate.ToString("MMMM dd, yyyy"), normalFont, XBrushes.Black,
            new XPoint(leftMargin + 120, yPosition));

        yPosition += 40;

        // Bill To Section
        gfx.DrawString("BILL TO:", headerFont, new XSolidBrush(primaryColor),
            new XPoint(leftMargin, yPosition));

        yPosition += 25;

        // Client Name
        var clientName = fee.User != null 
            ? $"{fee.User.FirstName ?? ""} {fee.User.LastName ?? ""}".Trim() 
            : "Unknown";
        if (string.IsNullOrWhiteSpace(clientName))
            clientName = fee.User?.Username ?? "Unknown";

        gfx.DrawString(clientName, normalFont, XBrushes.Black,
            new XPoint(leftMargin, yPosition));

        yPosition += 20;

        // Client Email
        if (fee.User?.Email != null)
        {
            gfx.DrawString(fee.User.Email, smallFont, XBrushes.Gray,
                new XPoint(leftMargin, yPosition));
            yPosition += 20;
        }

        // Client Username
        if (fee.User?.Username != null)
        {
            gfx.DrawString($"Username: {fee.User.Username}", smallFont, XBrushes.Gray,
                new XPoint(leftMargin, yPosition));
        }

        yPosition += 50;

        // Description Section Header Background
        gfx.DrawRectangle(new XSolidBrush(primaryColor), leftMargin, yPosition, rightMargin - leftMargin, 30);

        // Description Header Text
        gfx.DrawString("Description", new XFont(fontFamily, 12, XFontStyle.Bold), XBrushes.White,
            new XPoint(leftMargin + 10, yPosition + 20));
        gfx.DrawString("Amount", new XFont(fontFamily, 12, XFontStyle.Bold), XBrushes.White,
            new XPoint(rightMargin - 100, yPosition + 20));

        yPosition += 40;

        // Description Content Background
        gfx.DrawRectangle(new XSolidBrush(lightGray), leftMargin, yPosition, rightMargin - leftMargin, 35);

        // Description Content
        var description = string.IsNullOrEmpty(fee.Description) ? "Monthly Service Fee" : fee.Description;
        gfx.DrawString(description, normalFont, XBrushes.Black,
            new XPoint(leftMargin + 10, yPosition + 22));
        gfx.DrawString($"${fee.Amount:N2}", normalFont, XBrushes.Black,
            new XPoint(rightMargin - 100, yPosition + 22));

        yPosition += 55;

        // Total Section
        gfx.DrawLine(new XPen(XColors.LightGray, 1), rightMargin - 200, yPosition, rightMargin, yPosition);

        yPosition += 20;

        gfx.DrawString("Total:", headerFont, XBrushes.Black,
            new XPoint(rightMargin - 200, yPosition));
        gfx.DrawString($"${fee.Amount:N2}", new XFont(fontFamily, 16, XFontStyle.Bold), XBrushes.Black,
            new XPoint(rightMargin - 100, yPosition));

        yPosition += 50;

        // Payment Status
        var statusColor = fee.Paid ? paidColor : unpaidColor;
        var statusText = fee.Paid ? "PAID" : "UNPAID";

        gfx.DrawRectangle(new XSolidBrush(statusColor), leftMargin, yPosition, 100, 35);
        gfx.DrawString(statusText, new XFont(fontFamily, 14, XFontStyle.Bold), XBrushes.White,
            new XRect(leftMargin, yPosition, 100, 35), XStringFormats.Center);

        if (fee.Paid && fee.PaymentDate.HasValue)
        {
            gfx.DrawString($"Payment Date: {fee.PaymentDate.Value:MMMM dd, yyyy}", normalFont, XBrushes.Black,
                new XPoint(leftMargin + 120, yPosition + 22));
        }
        else if (!fee.Paid && fee.DueDate < DateTime.UtcNow)
        {
            gfx.DrawString("OVERDUE", new XFont(fontFamily, 12, XFontStyle.Bold), new XSolidBrush(unpaidColor),
                new XPoint(leftMargin + 120, yPosition + 22));
        }

        // Footer
        yPosition = page.Height.Point - 80;

        gfx.DrawLine(new XPen(XColors.LightGray, 1), leftMargin, yPosition, rightMargin, yPosition);

        yPosition += 20;

        gfx.DrawString("Thank you for your business!", smallFont, XBrushes.Gray,
            new XRect(leftMargin, yPosition, page.Width.Point - 100, 20), XStringFormats.TopCenter);

        yPosition += 15;

        gfx.DrawString($"Generated on {DateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC", smallFont, XBrushes.Gray,
            new XRect(leftMargin, yPosition, page.Width.Point - 100, 20), XStringFormats.TopCenter);

        // Save to memory stream
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    #endregion
}
