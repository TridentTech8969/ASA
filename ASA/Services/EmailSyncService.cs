using IndustrialSolutions.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using IndustrialSolutions.Email;
using IndustrialSolutions.Hubs;


namespace IndustrialSolutions.Services
{

public class EmailSyncService : BackgroundService
{
    private readonly ImapEmailReader _reader;
    private readonly EmailCache _cache;
    private readonly IHubContext<EmailHub> _hub;
    private readonly EmailImapOptions _opt;


    public EmailSyncService(ImapEmailReader reader, EmailCache cache, IHubContext<EmailHub> hub, IOptions<EmailImapOptions> opt)
    { _reader = reader; _cache = cache; _hub = hub; _opt = opt.Value; }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var list = await _reader.FetchInboxAsync(stoppingToken);
                var newlyAdded = new List<EmailListItemDto>();


                foreach (var d in list)
                {
                    var existed = _cache.Contains(d.Id);
                    _cache.Upsert(new EmailDetailDto
                    {
                        Id = d.Id,
                        GmailUid = d.GmailUid,
                        Folder = d.Folder,
                        FromName = d.FromName,
                        FromEmail = d.FromEmail,
                        Subject = d.Subject,
                        Snippet = d.Snippet,
                        ReceivedUtc = d.ReceivedUtc,
                        ReceivedLocal = d.ReceivedLocal,
                        Unread = d.Unread,
                        HasAttachments = d.HasAttachments,
                        Labels = d.Labels
                    });
                    if (!existed) newlyAdded.Add(new EmailListItemDto
                    {
                        Id = d.Id,
                        FromName = d.FromName,
                        FromEmail = d.FromEmail,
                        Subject = d.Subject,
                        ReceivedLocal = d.ReceivedLocal
                    });
                }


                if (newlyAdded.Count > 0)
                    await _hub.Clients.All.SendAsync("NewEmails", newlyAdded, cancellationToken: stoppingToken);
            }
            catch
            {
                // TODO: log
            }


            await Task.Delay(TimeSpan.FromSeconds(_opt.SyncIntervalSeconds), stoppingToken);
        }
    }
}
}