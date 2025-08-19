using Microsoft.Extensions.Options;
using IndustrialSolutions.Email;
using IndustrialSolutions.Models;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Mail;

namespace IndustrialSolutions.Services
{
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
        public async Task<List<EmailDetailDto>> FetchInboxAsync(CancellationToken ct)
        {
            using var client = new ImapClient();
            await client.ConnectAsync(_opt.Host, _opt.Port, _opt.UseSsl ?
            SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_opt.Username, _opt.AppPassword, ct);
            // Select INBOX
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, ct);
            // Build search: UNDELETED + (all) and optionally Gmail label
            SearchQuery query = SearchQuery.NotDeleted;
            if (!string.IsNullOrWhiteSpace(_opt.FilterLabel))
            {
                // Gmail raw query to only include messages with label
                query = query.And(SearchQuery.GMailRawSearch($"label:\"{_opt.FilterLabel}\""));

            }
            // Pull the most recent N (e.g., 200) to keep memory small
            var uids = await inbox.SearchAsync(query, ct);
            var last200 = uids.OrderByDescending(u => u.Id).Take(200).ToList();
            var summaries = await inbox.FetchAsync(last200,
            MessageSummaryItems.Envelope | MessageSummaryItems.Flags |
            MessageSummaryItems.UniqueId | MessageSummaryItems.InternalDate |
            MessageSummaryItems.BodyStructure | MessageSummaryItems.GMailLabels, ct);
            var results = new List<EmailDetailDto>();
            foreach (var s in summaries)

            {
                var from = s.Envelope.From?.Mailboxes?.FirstOrDefault();
                var subject = s.Envelope.Subject ?? "(No subject)";
                var received = s.InternalDate ?? DateTimeOffset.UtcNow;
                // Load full message body only when needed (detail view). For cache,
                //we can create a small preview by fetching text parts quickly.
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
                    Unread = !s.Flags.HasValue || !
                s.Flags.Value.HasFlag(MessageFlags.Seen),
                    HasAttachments = s.Attachments?.Any() == true,
                    Labels = s.GMailLabels?.ToList() ?? new List<string>()
                };
                // Build a tiny snippet from available bodystructure (no full
               // download yet)
               dto.Snippet = ""; // we’ll fill on detail request
                  // Attachments list (metadata)
                if (s.Attachments != null)
                {
                    foreach (var a in s.Attachments)
                    {
                        var fileName = a.FileName ?? "attachment";
                        dto.Attachments.Add(new EmailAttachmentDto
                        {
                            AttachmentId = a.PartSpecifier,
                            FileName = fileName,
                            SizeBytes = a is BodyPartBasic bpb ? bpb.Octets : 0
                        });
                    }
                }
                results.Add(dto);
            }
            await client.DisconnectAsync(true, ct);
            return results;
        }

        public async Task<(EmailDetailDto dto, MimeMessage mime)>
        LoadFullAsync(uint uid, CancellationToken ct)
        {
            using var client = new ImapClient();
            await client.ConnectAsync(_opt.Host, _opt.Port, _opt.UseSsl ?
            SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_opt.Username, _opt.AppPassword, ct);
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, ct);
            var u = new UniqueId(uid);
            var message = await inbox.GetMessageAsync(u, ct);
            var from = message.From.Mailboxes.FirstOrDefault();
            var received = message.Date;
            var dto = new EmailDetailDto
            {
                Id = $"{uid}@INBOX",
                GmailUid = uid,
                Folder = "INBOX",
                FromName = from?.Name ?? string.Empty,
                FromEmail = from?.Address ?? string.Empty,
                Subject = message.Subject ?? "(No subject)",
                ReceivedUtc = received,
                ReceivedLocal = FormatLocal(received),
                Unread = !message.Headers.Contains("Seen"), // best-effort, readonly anyway
                HasAttachments = message.Attachments.Any(),
                Labels = message.Headers.Where(h => h.Field.Equals("X-GM-LABELS",
                StringComparison.OrdinalIgnoreCase)).Select(h => h.Value).ToList(),
                HtmlBody = message.HtmlBody ?? string.Empty,
                TextBody = message.TextBody ?? string.Empty,
                Snippet = (message.TextBody ?? message.HtmlBody ?? string.Empty)
            .Replace("\n", " ").Replace("\r", " ")
            .Trim()
            .Substring(0, Math.Min(160, (message.TextBody ??
            message.HtmlBody ?? string.Empty).Length))
            };
            // Attachments metadata
            foreach (var a in message.Attachments)
            {
                if (a is MimePart part)
                {
                    dto.Attachments.Add(new EmailAttachmentDto

                    {
                        AttachmentId = part.ContentId ??
                        part.ContentLocation?.ToString() ?? part.FileName, // best-effort id
                        FileName = part.FileName ?? "attachment",
                        SizeBytes = a.ContentDisposition?.Size ?? 0
                    });
                }
            }
            await client.DisconnectAsync(true, ct);
            return (dto, message);
        }
        public async Task<(Stream stream, string contentType, string fileName)>
        DownloadAttachmentAsync(uint uid, string fileName, CancellationToken ct)
        {
            using var client = new ImapClient();
            await client.ConnectAsync(_opt.Host, _opt.Port, _opt.UseSsl ?
            SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_opt.Username, _opt.AppPassword, ct);
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, ct);
            var message = await inbox.GetMessageAsync(new UniqueId(uid), ct);
            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart part)
                {
                    var name = part.FileName ?? "attachment";
                    if (!string.Equals(name, fileName,
                    StringComparison.OrdinalIgnoreCase)) continue;
                    var ms = new MemoryStream();
                    await part.Content.DecodeToAsync(ms, ct);
                    ms.Position = 0;
                    var ctType = part.ContentType?.MimeType ?? "application/octetstream";
                    return (ms, ctType, name);
                }
            }
            throw new FileNotFoundException("Attachment not found");
        }
    }

}
