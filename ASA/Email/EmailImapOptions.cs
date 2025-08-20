namespace IndustrialSolutions.Email
{
    public class EmailImapOptions
    {
        public string Host { get; set; } = "imap.gmail.com";
        public int Port { get; set; } = 993;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public string? FilterLabel { get; set; }
        public int SyncIntervalSeconds { get; set; } = 60;
        public string TimeZoneId { get; set; } = "India Standard Time";
        public int MaxEmailsToFetch { get; set; } = 200;
        public bool EnableDetailedLogging { get; set; } = false;
    }
}