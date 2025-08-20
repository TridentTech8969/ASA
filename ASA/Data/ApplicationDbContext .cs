// Add this to your ApplicationDbContext class

using IndustrialSolutions.Models.Entities;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Your auto-generated DbSets
    public DbSet<Email> Emails { get; set; }
    public DbSet<EmailAttachment> EmailAttachments { get; set; }

    // Your other DbSets...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Email entity configuration (if needed beyond auto-generated)
        modelBuilder.Entity<Email>(entity =>
        {
            // Add indexes for better query performance
            entity.HasIndex(e => e.ReceivedUtc);
            entity.HasIndex(e => e.UniqueEmailId).IsUnique();
            entity.HasIndex(e => e.Unread);
            entity.HasIndex(e => e.GmailUid);
            entity.HasIndex(e => e.IsContactForm);
            entity.HasIndex(e => new { e.IsContactForm, e.ReceivedUtc });
            entity.HasIndex(e => new { e.Unread, e.IsContactForm });
        });

        // EmailAttachment entity configuration (if needed)
        modelBuilder.Entity<EmailAttachment>(entity =>
        {
            entity.HasIndex(e => e.EmailId);
            entity.HasIndex(e => e.FileName);
        });

        // Your other entity configurations...
        base.OnModelCreating(modelBuilder);
    }
}