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
    public DateTime ReceivedUtc { get; set; }
    public string ReceivedLocal { get; set; } = string.Empty; // dd-MM-yyyy HH:mm
    public bool Unread { get; set; }
    public bool HasAttachments { get; set; }
    public List<string> Labels { get; set; } = new();

    // Contact form specific fields for list view
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? GST { get; set; }
    public bool IsContactForm { get; set; }
}

public class EmailDetailDto : EmailListItemDto
{
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public string? Message { get; set; }
    public List<EmailAttachmentDto> Attachments { get; set; } = new();

    // Contact form details for parsing
    public ContactFormDetails? ContactDetails { get; set; }
}

public class EmailAttachmentDto
{
    public string AttachmentId { get; set; } = string.Empty; // part id or content-id
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? ContentType { get; set; }
}

public class ContactFormDetails
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? GST { get; set; }
    public string? Message { get; set; }
    public string? Subject { get; set; }
    public DateTime? SubmittedAt { get; set; }
}