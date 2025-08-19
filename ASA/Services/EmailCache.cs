using IndustrialSolutions.Models;
using System.Collections.Concurrent;

namespace IndustrialSolutions.Services;

public class EmailCache
{
    private readonly ConcurrentDictionary<string, EmailDetailDto> _store = new();

    public IReadOnlyCollection<EmailListItemDto> List() => _store.Values
        .OrderByDescending(e => e.ReceivedUtc)
        .Select(e => new EmailListItemDto
        {
            Id = e.Id,
            GmailUid = e.GmailUid,
            Folder = e.Folder,
            FromName = e.FromName,
            FromEmail = e.FromEmail,
            Subject = e.Subject,
            Snippet = e.Snippet,
            ReceivedUtc = e.ReceivedUtc,
            ReceivedLocal = e.ReceivedLocal,
            Unread = e.Unread,
            HasAttachments = e.HasAttachments,
            Labels = e.Labels.ToList()
        })
        .ToList();

    public EmailDetailDto? Get(string id) => _store.TryGetValue(id, out var v) ? v : null;

    public void Upsert(EmailDetailDto item) => _store[item.Id] = item;

    public bool Contains(string id) => _store.ContainsKey(id);
}