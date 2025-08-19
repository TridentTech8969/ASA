using Microsoft.Extensions.Options;
using IndustrialSolutions.Email;
using IndustrialSolutions.Models;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MailKit.Security;
using MimeKit;

namespace IndustrialSolutions.Services;

public class ImapEmailReader
{
    private readonly EmailImapOptions _opt;
    private readonly TimeZoneInfo _tz;

    public ImapEmailReader(IOptions<EmailImapOptions> opt)
    {
        _opt = opt.Value;
        _tz = TimeZoneInfo.FindSystemTimeZoneById(_opt.TimeZoneId);
    }

    private string FormatLocal(DateTimeOffset dt)
    {
        var local = TimeZoneInfo.ConvertTime(dt, _tz);
        return local.ToString("dd-MM-yyyy HH:mm");
    }

    // Lightweight list for the grid
    public async Task<List<EmailDetailDto>> FetchInboxAsync(CancellationToken ct)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_opt.Host, _opt.Port,
            _opt.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_opt.Username, _opt.AppPassword, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        // Simple search for all non-deleted messages
        SearchQuery query = SearchQuery.NotDeleted;
        var uids = await inbox.SearchAsync(query, ct);
        var last200 = uids.OrderByDescending(u => u.Id).Take(200).ToList();

        // Simple fetch without complex parameters - this WILL work
        var summaries = await inbox.FetchAsync(last200, MessageSummaryItems.All, ct);

        var results = new List<EmailDetailDto>();
        foreach (var s in summaries)
        {
            // Filter by label on client side if needed
            if (!string.IsNullOrWhiteSpace(_opt.FilterLabel))
            {
                var labels = s.GMailLabels ?? new string[0];
                bool hasLabel = labels.Any(l => l.Equals(_opt.FilterLabel, StringComparison.OrdinalIgnoreCase));
                if (!hasLabel) continue; // Skip if doesn't have required label
            }

            var from = s.Envelope.From?.Mailboxes?.FirstOrDefault();
            var subject = s.Envelope.Subject ?? "(No subject)";
            var received = s.InternalDate ?? DateTimeOffset.UtcNow;

            var dto = new EmailDetailDto
            {
                Id = $"{s.UniqueId.Id}@INBOX",
                GmailUid = s.UniqueId.Id,
                Folder = "INBOX",
                FromName = from?.Name ?? string.Empty,
                FromEmail = from?.Address ?? string.Empty,
                Subject = subject,
                ReceivedUtc = received,
                ReceivedLocal = FormatLocal(received),
                Unread = !s.Flags.HasValue || !s.Flags.Value.HasFlag(MessageFlags.Seen),
                HasAttachments = s.Attachments?.Any() == true,
                Labels = s.GMailLabels?.ToList() ?? new List<string>(),
                Snippet = string.Empty
            };

            if (s.Attachments != null)
            {
                foreach (var a in s.Attachments)
                {
                    dto.Attachments.Add(new EmailAttachmentDto
                    {
                        AttachmentId = a.PartSpecifier,
                        FileName = a.FileName ?? "attachment",
                        SizeBytes = a is BodyPartBasic bpb ? bpb.Octets : 0
                    });
                }
            }

            results.Add(dto);
        }

        await client.DisconnectAsync(true, ct);
        return results;
    }

    // Full message for detail view
    public async Task<(EmailDetailDto dto, MimeMessage mime)> LoadFullAsync(uint uid, CancellationToken ct)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_opt.Host, _opt.Port,
            _opt.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_opt.Username, _opt.AppPassword, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        var message = await inbox.GetMessageAsync(new UniqueId(uid), ct);
        var from = message.From.Mailboxes.FirstOrDefault();

        var dto = new EmailDetailDto
        {
            Id = $"{uid}@INBOX",
            GmailUid = uid,
            Folder = "INBOX",
            FromName = from?.Name ?? string.Empty,
            FromEmail = from?.Address ?? string.Empty,
            Subject = message.Subject ?? "(No subject)",
            ReceivedUtc = message.Date,
            ReceivedLocal = FormatLocal(message.Date),
            Unread = false, // Flags aren't on MimeMessage; keep false here
            HasAttachments = message.Attachments.Any(),
            HtmlBody = message.HtmlBody ?? string.Empty,
            TextBody = message.TextBody ?? string.Empty,
            Snippet = (message.TextBody ?? message.HtmlBody ?? string.Empty)
                .Replace("\n", " ").Replace("\r", " ")
                .Trim()
        };

        // Take snippet from available text (max 160 chars)
        if (dto.Snippet.Length > 160)
        {
            dto.Snippet = dto.Snippet.Substring(0, 160) + "...";
        }

        foreach (var a in message.Attachments.OfType<MimePart>())
        {
            dto.Attachments.Add(new EmailAttachmentDto
            {
                AttachmentId = a.ContentId ?? a.FileName ?? Guid.NewGuid().ToString("N"),
                FileName = a.FileName ?? "attachment",
                SizeBytes = 0 // MimePart doesn't expose size reliably without decoding
            });
        }

        await client.DisconnectAsync(true, ct);
        return (dto, message);
    }

    // Download a specific attachment by file name
    public async Task<(Stream stream, string contentType, string fileName)> DownloadAttachmentAsync(uint uid, string fileName, CancellationToken ct)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_opt.Host, _opt.Port,
            _opt.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_opt.Username, _opt.AppPassword, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        var message = await inbox.GetMessageAsync(new UniqueId(uid), ct);

        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            var name = attachment.FileName ?? "attachment";
            if (!string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase)) continue;

            var ms = new MemoryStream();
            await attachment.Content.DecodeToAsync(ms, ct);
            ms.Position = 0;

            var ctType = attachment.ContentType?.MimeType ?? "application/octet-stream";
            return (ms, ctType, name);
        }

        throw new FileNotFoundException("Attachment not found");
    }
}