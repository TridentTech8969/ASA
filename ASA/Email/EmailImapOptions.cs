namespace IndustrialSolutions.Email
{
    public class EmailImapOptions
    {
        public string Host { get; set; } = "imap.gmail.com";
        public int Port { get; set; } = 993; // SSL
        public bool UseSsl { get; set; } = true;


        public string Username { get; set; } = "youremail@gmail.com"; // TODO: set yours
        public string AppPassword { get; set; } = "xxxxxxxxxxxxxxxx"; // TODO: Gmail App Password


        // Optional Gmail label to further filter INBOX (e.g., "Contact Form"). Set null/empty to ignore.
        public string? FilterLabel { get; set; } = "Contact Form";


        public int SyncIntervalSeconds { get; set; } = 120; // every 2 minutes
        public string TimeZoneId { get; set; } = "Asia/Kolkata";
    }
}
