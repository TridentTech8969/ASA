namespace IndustrialSolutions.Models;

public class EmailListItemDto
{
    public string Id { get; set; } = string.Empty; // our app id (uid@folder)
    public uint GmailUid { get; set; }
    public string Folder { get; set; } = "INBOX";
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public DateTimeOffset ReceivedUtc { get; set; }
    public string ReceivedLocal { get; set; } = string.Empty; // dd-MM-yyyy HH:mm
    public bool Unread { get; set; }
    public bool HasAttachments { get; set; }
    public List<string> Labels { get; set; } = new();
}

public class EmailDetailDto : EmailListItemDto
{
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public List<EmailAttachmentDto> Attachments { get; set; } = new();
}

public class EmailAttachmentDto
{
    public string AttachmentId { get; set; } = string.Empty; // part id or content-id
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}