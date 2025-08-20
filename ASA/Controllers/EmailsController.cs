using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using IndustrialSolutions.Models.Entities;
using EmailEntity = IndustrialSolutions.Models.Entities.Email;

namespace IndustrialSolutions.Controllers;

// Uncomment when you have authentication set up
// [Authorize(Roles = "Admin")]
public class EmailsController : Controller
{
    private readonly ILogger<EmailsController> _logger;
    private readonly IndustrialSolutionsEmailsContext _context;

    public EmailsController(
        ILogger<EmailsController> logger,
        IndustrialSolutionsEmailsContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet("api/emails")]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string source = "all")
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            IQueryable<EmailEntity> query = _context.Emails.AsQueryable();

            // Filter by source
            switch (source.ToLower())
            {
                case "contactform":
                    query = query.Where(e => e.IsContactForm == true);
                    break;
                case "gmail":
                    query = query.Where(e => e.IsContactForm == false && e.GmailUid > 0);
                    break;
                case "all":
                default:
                    // Show all emails
                    break;
            }

            var totalCount = await query.CountAsync();

            var emails = await query
                .OrderByDescending(e => e.ReceivedUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    Id = e.UniqueEmailId,
                    Subject = e.Subject,
                    FromEmail = e.FromEmail,
                    FromName = e.FromName,
                    Company = e.Company,
                    Phone = e.Phone,
                    GstNumber = e.GstNumber,
                    ReceivedLocal = e.ReceivedLocal,
                    ReceivedUtc = e.ReceivedUtc,
                    Unread = e.Unread,
                    HasAttachments = e.HasAttachments,
                    Source = e.IsContactForm ? "ContactForm" : "Gmail",
                    Message = e.IsContactForm ? e.Message : e.Snippet,
                    Snippet = e.Snippet,
                    LabelsJson = e.LabelsJson // Get the raw JSON string
                })
                .ToListAsync();

            // Process the labels after the data is retrieved from database
            var processedEmails = emails.Select(e => new
            {
                e.Id,
                e.Subject,
                e.FromEmail,
                e.FromName,
                e.Company,
                e.Phone,
                e.GstNumber,
                e.ReceivedLocal,
                e.ReceivedUtc,
                e.Unread,
                e.HasAttachments,
                e.Source,
                e.Message,
                e.Snippet,
                Labels = !string.IsNullOrEmpty(e.LabelsJson)
                    ? JsonSerializer.Deserialize<string[]>(e.LabelsJson)
                    : new string[0]
            }).ToList();

            _logger.LogInformation("API: Returning {Count} of {Total} emails (page {Page})",
                processedEmails.Count, totalCount, page);

            return Json(new
            {
                data = processedEmails,
                success = true,
                count = processedEmails.Count,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in List API");
            return Json(new
            {
                data = new List<object>(),
                success = false,
                error = "Failed to retrieve emails"
            });
        }
    }

    [HttpGet("api/emails/{id}")]
    public async Task<IActionResult> Detail(
        [Required] string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Email ID is required" });

            // Find the email in the database
            var email = await _context.Emails
                .Include(e => e.EmailAttachments)
                .FirstOrDefaultAsync(e => e.UniqueEmailId == id);

            if (email == null)
                return NotFound(new { error = "Email not found" });

            // Mark as read
            if (email.Unread)
            {
                email.Unread = false;
                email.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var labels = !string.IsNullOrEmpty(email.LabelsJson)
                ? JsonSerializer.Deserialize<string[]>(email.LabelsJson)
                : new string[0];

            var dto = new
            {
                Id = email.UniqueEmailId,
                Subject = email.Subject,
                FromEmail = email.FromEmail,
                FromName = email.FromName,
                Company = email.Company,
                Phone = email.Phone,
                GstNumber = email.GstNumber,
                Message = email.Message,
                HtmlBody = email.HtmlBody,
                TextBody = email.TextBody,
                ReceivedLocal = email.ReceivedLocal,
                ReceivedUtc = email.ReceivedUtc,
                HasAttachments = email.HasAttachments,
                Source = email.IsContactForm ? "ContactForm" : "Gmail",
                IsContactForm = email.IsContactForm,
                Labels = labels,
                Attachments = email.EmailAttachments.Select(a => new
                {
                    FileName = a.FileName,
                    SizeBytes = a.SizeBytes,
                    ContentType = a.ContentType
                }).ToList(),
                CanReply = true,
                ReplyToEmail = email.FromEmail,
                ReplyToName = email.FromName
            };

            return Json(dto);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email detail load timed out for ID: {Id}", id);
            return Json(new { error = "Request timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading email detail for ID: {Id}", id);
            return Json(new { error = "Failed to load email details" });
        }
    }

    // Get dashboard statistics
    [HttpGet("api/emails/stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);

            var contactFormStats = new
            {
                todayCount = await _context.Emails
                    .CountAsync(e => e.ReceivedUtc >= today && e.IsContactForm),
                weekCount = await _context.Emails
                    .CountAsync(e => e.ReceivedUtc >= thisWeek && e.IsContactForm),
                unreadCount = await _context.Emails
                    .CountAsync(e => e.Unread && e.IsContactForm)
            };

            var gmailStats = new
            {
                count = await _context.Emails
                    .CountAsync(e => !e.IsContactForm && e.GmailUid > 0),
                unreadCount = await _context.Emails
                    .CountAsync(e => e.Unread && !e.IsContactForm && e.GmailUid > 0)
            };

            return Json(new
            {
                contactForm = contactFormStats,
                gmail = gmailStats,
                total = new
                {
                    count = contactFormStats.todayCount + gmailStats.count,
                    unread = contactFormStats.unreadCount + gmailStats.unreadCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email statistics");
            return Json(new { error = "Failed to load statistics" });
        }
    }

    // Mark email as read
    [HttpPost("api/emails/{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        try
        {
            var email = await _context.Emails
                .FirstOrDefaultAsync(e => e.UniqueEmailId == id);

            if (email == null)
                return NotFound(new { error = "Email not found" });

            email.Unread = false;
            email.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking email as read: {Id}", id);
            return Json(new { success = false, error = "Failed to update email" });
        }
    }
}