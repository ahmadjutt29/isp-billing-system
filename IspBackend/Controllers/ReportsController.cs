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
/// Controller for generating reports and invoices. Admin only.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IFeeService _feeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class.
    /// </summary>
    /// <param name="feeService">The fee service.</param>
    public ReportsController(IFeeService feeService)
    {
        _feeService = feeService;
    }

    /// <summary>
    /// Gets income summary report with total paid and unpaid fees.
    /// </summary>
    /// <returns>Income summary object.</returns>
    [HttpGet("income")]
    [ProducesResponseType(typeof(IncomeSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IncomeSummaryDto>> GetIncomeSummary()
    {
        var summary = await _feeService.GetIncomeSummaryAsync();
        return Ok(summary);
    }

    /// <summary>
    /// Gets monthly income breakdown.
    /// </summary>
    /// <param name="year">The year to get report for. Defaults to current year.</param>
    /// <returns>Monthly income breakdown.</returns>
    [HttpGet("income/monthly")]
    [ProducesResponseType(typeof(IEnumerable<MonthlyIncomeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MonthlyIncomeDto>>> GetMonthlyIncome([FromQuery] int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var monthlyIncome = await _feeService.GetMonthlyIncomeAsync(targetYear);
        return Ok(monthlyIncome);
    }

    /// <summary>
    /// Generates a PDF invoice for a specific fee.
    /// Clients can only download their own invoices. Admins can download any invoice.
    /// </summary>
    /// <param name="feeId">The fee ID.</param>
    /// <returns>PDF file download.</returns>
    [HttpGet("invoice/{feeId}")]
    [Authorize(Roles = "Admin,Client")] // Override class-level Admin-only to allow clients
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateInvoice(int feeId)
    {
        var fee = await _feeService.GetFeeWithUserAsync(feeId);

        if (fee == null)
        {
            return NotFound(new { message = "Fee not found" });
        }

        // Check if client is trying to access someone else's invoice
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (userRole == "Client" && int.TryParse(userIdClaim, out int userId))
        {
            if (fee.UserId != userId)
            {
                return Forbid();
            }
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
        var textColor = XColors.Black;
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
}
