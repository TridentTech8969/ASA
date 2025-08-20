using IndustrialSolutions.Hubs;
using IndustrialSolutions.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
// Use aliases to avoid namespace conflicts
using EmailConfig = IndustrialSolutions.Email.EmailImapOptions;

namespace IndustrialSolutions.Services;

public class EmailSyncService : BackgroundService
{
    private readonly ImapEmailReader _reader;
    private readonly IHubContext<EmailHub> _hub;
    private readonly EmailConfig _opt;
    private readonly ILogger<EmailSyncService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private CancellationToken _serviceCancellationToken;

    public EmailSyncService(
        ImapEmailReader reader,
        IHubContext<EmailHub> hub,
        IOptions<EmailConfig> opt,
        ILogger<EmailSyncService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _reader = reader;
        _hub = hub;
        _opt = opt.Value;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _serviceCancellationToken = stoppingToken;

        // Wait a bit before starting the first sync
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email sync");
            }

            // Wait for the next sync interval
            await Task.Delay(TimeSpan.FromSeconds(_opt.SyncIntervalSeconds), stoppingToken);
        }
    }

    private async Task SyncEmailsAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting email sync...");

            // Fetch emails from IMAP
            var fetchedEmails = await _reader.FetchInboxAsync(stoppingToken);
            _logger.LogInformation($"Fetched {fetchedEmails.Count} emails from IMAP");

            var newlyAdded = new List<EmailListItemDto>();
            var updatedCount = 0;

            // Create a new scope for database operations
            using var scope = _serviceScopeFactory.CreateScope();
            var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

            foreach (var fetchedEmail in fetchedEmails)
            {
                try
                {
                    // Check if email already exists in database
                    var exists = await emailRepository.EmailExistsAsync(fetchedEmail.Id);

                    if (!exists)
                    {
                        // Save new email to database
                        await emailRepository.SaveEmailAsync(fetchedEmail);

                        // Add to notification list
                        newlyAdded.Add(new EmailListItemDto
                        {
                            Id = fetchedEmail.Id,
                            GmailUid = fetchedEmail.GmailUid,
                            FromName = fetchedEmail.FromName,
                            FromEmail = fetchedEmail.FromEmail,
                            Subject = fetchedEmail.Subject,
                            ReceivedLocal = fetchedEmail.ReceivedLocal,
                            Unread = fetchedEmail.Unread,
                            HasAttachments = fetchedEmail.HasAttachments,
                            IsContactForm = fetchedEmail.FromName == "Contact from Website" ||
                                          fetchedEmail.FromEmail.Contains("contact@") ||
                                          fetchedEmail.Subject.ToLower().Contains("contact form")
                        });

                        _logger.LogInformation($"New email saved: {fetchedEmail.Subject} from {fetchedEmail.FromEmail}");
                    }
                    else
                    {
                        // Update existing email (in case flags changed)
                        await emailRepository.UpdateEmailAsync(fetchedEmail);
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing email {fetchedEmail.Id}: {fetchedEmail.Subject}");
                }
            }

            _logger.LogInformation($"Sync completed. New: {newlyAdded.Count}, Updated: {updatedCount}");

            // Notify clients about new emails
            if (newlyAdded.Count > 0)
            {
                await _hub.Clients.All.SendAsync("NewEmails", newlyAdded, cancellationToken: stoppingToken);
                _logger.LogInformation($"Notified clients about {newlyAdded.Count} new emails");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SyncEmailsAsync");
            throw;
        }
    }

    public async Task<SyncStatusDto> GetSyncStatusAsync()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

            var totalEmails = await emailRepository.GetEmailCountAsync();
            var recentEmails = await emailRepository.GetRecentEmailsAsync(5);

            return new SyncStatusDto
            {
                TotalEmails = totalEmails,
                LastSyncTime = DateTime.UtcNow,
                RecentEmails = recentEmails,
                IsRunning = !_serviceCancellationToken.IsCancellationRequested
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status");
            return new SyncStatusDto
            {
                TotalEmails = 0,
                LastSyncTime = DateTime.UtcNow,
                RecentEmails = new List<EmailListItemDto>(),
                IsRunning = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Manual sync trigger for testing or immediate sync
    /// </summary>
    public async Task TriggerManualSyncAsync()
    {
        try
        {
            _logger.LogInformation("Manual sync triggered");
            await SyncEmailsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in manual sync");
            throw;
        }
    }
}

public class SyncStatusDto
{
    public int TotalEmails { get; set; }
    public DateTime LastSyncTime { get; set; }
    public List<EmailListItemDto> RecentEmails { get; set; } = new();
    public bool IsRunning { get; set; }
    public string? ErrorMessage { get; set; }
}