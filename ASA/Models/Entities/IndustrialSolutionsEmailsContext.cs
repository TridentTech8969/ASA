using Microsoft.EntityFrameworkCore;
// Use aliases to avoid namespace conflict
using EmailEntity = IndustrialSolutions.Models.Entities.Email;
using EmailAttachmentEntity = IndustrialSolutions.Models.Entities.EmailAttachment;

namespace IndustrialSolutions.Models.Entities;

public partial class IndustrialSolutionsEmailsContext : DbContext
{
    public IndustrialSolutionsEmailsContext()
    {
    }

    public IndustrialSolutionsEmailsContext(DbContextOptions<IndustrialSolutionsEmailsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EmailEntity> Emails { get; set; } = null!;

    public virtual DbSet<EmailAttachmentEntity> EmailAttachments { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=IndustrialSolutionsEmails;Trusted_Connection=true;MultipleActiveResultSets=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Emails");

            entity.HasIndex(e => e.FromEmail, "IX_Emails_FromEmail");

            entity.HasIndex(e => e.GmailUid, "IX_Emails_GmailUid");

            entity.HasIndex(e => e.IsContactForm, "IX_Emails_IsContactForm");

            entity.HasIndex(e => e.ReceivedUtc, "IX_Emails_ReceivedUtc");

            entity.HasIndex(e => e.UniqueEmailId, "IX_Emails_UniqueEmailId").IsUnique();

            entity.Property(e => e.Company).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2(7)");
            entity.Property(e => e.Folder)
                .HasMaxLength(50)
                .HasDefaultValue("INBOX");
            entity.Property(e => e.FromEmail)
                .HasMaxLength(500)
                .HasDefaultValue("");
            entity.Property(e => e.FromName)
                .HasMaxLength(500)
                .HasDefaultValue("");
            entity.Property(e => e.GstNumber).HasMaxLength(50);
            entity.Property(e => e.LabelsJson).HasMaxLength(2000);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.ReceivedLocal)
                .HasMaxLength(50)
                .HasDefaultValue("");
            entity.Property(e => e.ReceivedUtc).HasColumnType("datetime2(7)");
            entity.Property(e => e.Snippet)
                .HasMaxLength(500)
                .HasDefaultValue("");
            entity.Property(e => e.Subject)
                .HasMaxLength(1000)
                .HasDefaultValue("");
            entity.Property(e => e.UniqueEmailId).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2(7)");
        });

        modelBuilder.Entity<EmailAttachmentEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_EmailAttachments");

            entity.HasIndex(e => e.EmailId, "IX_EmailAttachments_EmailId");

            entity.Property(e => e.AttachmentId)
                .HasMaxLength(500)
                .HasDefaultValue("");
            entity.Property(e => e.ContentType).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2(7)");
            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .HasDefaultValue("");

            entity.HasOne(d => d.Email).WithMany(p => p.EmailAttachments)
                .HasForeignKey(d => d.EmailId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmailAttachments_Emails_EmailId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}