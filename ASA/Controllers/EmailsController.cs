using Microsoft.AspNetCore.Mvc;
using IndustrialSolutions.Services;
using Microsoft.AspNetCore.Authorization;

namespace IndustrialSolutions.Controllers;

// Uncomment when you have authentication set up
// [Authorize(Roles = "Admin")]
public class EmailsController : Controller
{
    private readonly EmailCache _cache;
    private readonly ImapEmailReader _reader;
    private readonly ILogger<EmailsController> _logger;

    public EmailsController(EmailCache cache, ImapEmailReader reader, ILogger<EmailsController> logger)
    {
        _cache = cache;
        _reader = reader;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet("api/emails/test-fetch")]
    public async Task<IActionResult> TestFetch()
    {
        try
        {
            var emails = await _reader.FetchInboxAsync(CancellationToken.None);
            return Json(new
            {
                success = true,
                count = emails.Count,
                emails = emails.Take(3).Select(e => new {
                    e.Subject,
                    e.FromEmail,
                    e.ReceivedLocal
                })
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("api/emails")]
    public IActionResult List()
    {
        try
        {
            var items = _cache.List().Where(x => x.FromName == "Contact from Website").ToList();
            _logger.LogInformation($"API: Returning {items.Count} emails");
            return Json(new { data = items, success = true, count = items.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in List API");
            return Json(new { data = new List<object>(), success = false, error = ex.Message });
        }
    }

    [HttpGet("api/emails/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("ID is required");
            if (!uint.TryParse(id.Split('@')[0], out var uid)) return BadRequest("Invalid ID format");

            _logger.LogInformation($"Loading email detail for UID: {uid}");
            var (dto, _) = await _reader.LoadFullAsync(uid, HttpContext.RequestAborted);
            return Json(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading email detail for ID: {id}");
            return Json(new { error = ex.Message });
        }
    }

    [HttpGet("api/emails/{id}/attachment")]
    public async Task<IActionResult> Download(string id, string fileName)
    {
        try
        {
            if (!uint.TryParse(id.Split('@')[0], out var uid)) return BadRequest();

            var (stream, contentType, name) = await _reader.DownloadAttachmentAsync(uid, fileName, HttpContext.RequestAborted);
            return File(stream, contentType, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading attachment: {fileName} for email: {id}");
            return NotFound();
        }
    }
}