namespace IndustrialSolutions.Email;

public class EmailImapOptions
{
    public string Host { get; set; } = "imap.gmail.com";
    public int Port { get; set; } = 993; // SSL
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = "eternalvision2025@gmail.com"; // Your Gmail
    public string AppPassword { get; set; } = "gvvy enkz fjjo iccp"; // Gmail App Password

    // Optional Gmail label to further filter INBOX (e.g., "Contact Form"). Set null/empty to ignore.
    public string? FilterLabel { get; set; } = null; //"Contact Form";
    public int SyncIntervalSeconds { get; set; } = 120; // every 2 minutes
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
}