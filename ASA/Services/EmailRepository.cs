using IndustrialSolutions.Models;
using IndustrialSolutions.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
// Use aliases to avoid namespace conflicts
using EmailEntity = IndustrialSolutions.Models.Entities.Email;
using EmailAttachmentEntity = IndustrialSolutions.Models.Entities.EmailAttachment;

namespace IndustrialSolutions.Services
{
    public interface IEmailRepository
    {
        Task<List<EmailListItemDto>> GetEmailListAsync(bool contactFormsOnly = false);
        Task<EmailDetailDto?> GetEmailDetailAsync(string uniqueEmailId);
        Task<EmailDetailDto?> GetEmailDetailByUidAsync(uint uid);
        Task<EmailEntity?> GetEmailEntityAsync(string uniqueEmailId);
        Task<bool> EmailExistsAsync(string uniqueEmailId);
        Task SaveEmailAsync(EmailDetailDto emailDto);
        Task UpdateEmailAsync(EmailDetailDto emailDto);
        Task<int> GetEmailCountAsync();
        Task<List<EmailListItemDto>> GetRecentEmailsAsync(int count = 10);
    }

    public class EmailRepository : IEmailRepository
    {
        private readonly IndustrialSolutionsEmailsContext _context;
        private readonly ILogger<EmailRepository> _logger;

        public EmailRepository(IndustrialSolutionsEmailsContext context, ILogger<EmailRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<EmailListItemDto>> GetEmailListAsync(bool contactFormsOnly = false)
        {
            try
            {
                var query = _context.Emails.AsQueryable();

                if (contactFormsOnly)
                {
                    query = query.Where(e => e.IsContactForm);
                }

                // First, get the raw data without JSON deserialization
                var rawEmails = await query
                    .OrderByDescending(e => e.ReceivedUtc)
                    .Select(e => new
                    {
                        e.UniqueEmailId,
                        e.GmailUid,
                        e.Folder,
                        e.FromName,
                        e.FromEmail,
                        e.Subject,
                        e.Snippet,
                        e.ReceivedUtc,
                        e.ReceivedLocal,
                        e.Unread,
                        e.HasAttachments,
                        e.LabelsJson,
                        e.Company,
                        e.Phone,
                        e.GstNumber,
                        e.IsContactForm
                    })
                    .ToListAsync();

                // Then process the JSON deserialization in memory
                var emails = rawEmails.Select(e => new EmailListItemDto
                {
                    Id = e.UniqueEmailId,
                    GmailUid = (uint)e.GmailUid,
                    Folder = e.Folder,
                    FromName = e.FromName,
                    FromEmail = e.FromEmail,
                    Subject = e.Subject,
                    Snippet = e.Snippet,
                    ReceivedUtc = e.ReceivedUtc,
                    ReceivedLocal = e.ReceivedLocal,
                    Unread = e.Unread,
                    HasAttachments = e.HasAttachments,
                    Labels = ParseLabelsJson(e.LabelsJson),
                    Company = e.Company,
                    Phone = e.Phone,
                    GST = e.GstNumber,
                    IsContactForm = e.IsContactForm
                }).ToList();

                return emails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email list");
                return new List<EmailListItemDto>();
            }
        }

        public async Task<EmailDetailDto?> GetEmailDetailAsync(string uniqueEmailId)
        {
            try
            {
                var email = await _context.Emails
                    .Include(e => e.EmailAttachments)
                    .FirstOrDefaultAsync(e => e.UniqueEmailId == uniqueEmailId);

                if (email == null) return null;

                return MapToDetailDto(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email detail for ID: {EmailId}", uniqueEmailId);
                return null;
            }
        }

        public async Task<EmailDetailDto?> GetEmailDetailByUidAsync(uint uid)
        {
            try
            {
                var email = await _context.Emails
                    .Include(e => e.EmailAttachments)
                    .FirstOrDefaultAsync(e => e.GmailUid == uid);

                if (email == null) return null;

                return MapToDetailDto(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email detail for UID: {Uid}", uid);
                return null;
            }
        }

        public async Task<EmailEntity?> GetEmailEntityAsync(string uniqueEmailId)
        {
            return await _context.Emails
                .Include(e => e.EmailAttachments)
                .FirstOrDefaultAsync(e => e.UniqueEmailId == uniqueEmailId);
        }

        public async Task<bool> EmailExistsAsync(string uniqueEmailId)
        {
            return await _context.Emails.AnyAsync(e => e.UniqueEmailId == uniqueEmailId);
        }

        public async Task SaveEmailAsync(EmailDetailDto emailDto)
        {
            try
            {
                var email = MapToEntity(emailDto);
                email.CreatedAt = DateTime.UtcNow;
                email.UpdatedAt = DateTime.UtcNow;

                _context.Emails.Add(email);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Email saved with ID: {EmailId}", email.UniqueEmailId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving email: {EmailId}", emailDto.Id);
                throw;
            }
        }

        public async Task UpdateEmailAsync(EmailDetailDto emailDto)
        {
            try
            {
                var existingEmail = await _context.Emails
                    .Include(e => e.EmailAttachments)
                    .FirstOrDefaultAsync(e => e.UniqueEmailId == emailDto.Id);

                if (existingEmail == null)
                {
                    await SaveEmailAsync(emailDto);
                    return;
                }

                // Update existing email
                UpdateEntityFromDto(existingEmail, emailDto);
                existingEmail.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Email updated with ID: {EmailId}", emailDto.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email: {EmailId}", emailDto.Id);
                throw;
            }
        }

        public async Task<int> GetEmailCountAsync()
        {
            return await _context.Emails.CountAsync();
        }

        public async Task<List<EmailListItemDto>> GetRecentEmailsAsync(int count = 10)
        {
            // First get raw data
            var rawEmails = await _context.Emails
                .OrderByDescending(e => e.ReceivedUtc)
                .Take(count)
                .Select(e => new
                {
                    e.UniqueEmailId,
                    e.GmailUid,
                    e.Folder,
                    e.FromName,
                    e.FromEmail,
                    e.Subject,
                    e.Snippet,
                    e.ReceivedUtc,
                    e.ReceivedLocal,
                    e.Unread,
                    e.HasAttachments,
                    e.LabelsJson,
                    e.Company,
                    e.Phone,
                    e.GstNumber,
                    e.IsContactForm
                })
                .ToListAsync();

            // Then process JSON in memory
            return rawEmails.Select(e => new EmailListItemDto
            {
                Id = e.UniqueEmailId,
                GmailUid = (uint)e.GmailUid,
                Folder = e.Folder,
                FromName = e.FromName,
                FromEmail = e.FromEmail,
                Subject = e.Subject,
                Snippet = e.Snippet,
                ReceivedUtc = e.ReceivedUtc,
                ReceivedLocal = e.ReceivedLocal,
                Unread = e.Unread,
                HasAttachments = e.HasAttachments,
                Labels = ParseLabelsJson(e.LabelsJson),
                Company = e.Company,
                Phone = e.Phone,
                GST = e.GstNumber,
                IsContactForm = e.IsContactForm
            }).ToList();
        }

        private EmailDetailDto MapToDetailDto(EmailEntity email)
        {
            return new EmailDetailDto
            {
                Id = email.UniqueEmailId,
                GmailUid = (uint)email.GmailUid,
                Folder = email.Folder,
                FromName = email.FromName,
                FromEmail = email.FromEmail,
                Subject = email.Subject,
                Snippet = email.Snippet,
                ReceivedUtc = email.ReceivedUtc,
                ReceivedLocal = email.ReceivedLocal,
                Unread = email.Unread,
                HasAttachments = email.HasAttachments,
                HtmlBody = email.HtmlBody ?? string.Empty,
                TextBody = email.TextBody ?? string.Empty,
                Labels = ParseLabelsJson(email.LabelsJson),
                Attachments = email.EmailAttachments.Select(a => new EmailAttachmentDto
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.FileName,
                    SizeBytes = a.SizeBytes,
                    ContentType = a.ContentType
                }).ToList(),
                // Contact form fields
                Company = email.Company,
                Phone = email.Phone,
                GST = email.GstNumber,
                Message = email.Message,
                IsContactForm = email.IsContactForm
            };
        }

        private EmailEntity MapToEntity(EmailDetailDto dto)
        {
            // Extract contact form data from email content
            var contactData = ExtractContactFormData(dto);

            var email = new EmailEntity
            {
                UniqueEmailId = dto.Id,
                GmailUid = dto.GmailUid,
                Folder = dto.Folder,
                FromName = dto.FromName,
                FromEmail = dto.FromEmail,
                Subject = dto.Subject,
                Snippet = dto.Snippet,
                ReceivedUtc = dto.ReceivedUtc,
                ReceivedLocal = dto.ReceivedLocal,
                Unread = dto.Unread,
                HasAttachments = dto.HasAttachments,
                HtmlBody = dto.HtmlBody,
                TextBody = dto.TextBody,
                LabelsJson = SerializeLabels(dto.Labels),
                // Contact form data
                Company = contactData.Company,
                Phone = contactData.Phone,
                GstNumber = contactData.GstNumber,
                Message = contactData.Message,
                IsContactForm = contactData.IsContactForm
            };

            // Add attachments
            foreach (var attachment in dto.Attachments)
            {
                email.EmailAttachments.Add(new EmailAttachmentEntity
                {
                    AttachmentId = attachment.AttachmentId,
                    FileName = attachment.FileName,
                    SizeBytes = attachment.SizeBytes,
                    ContentType = attachment.ContentType
                });
            }

            return email;
        }

        private void UpdateEntityFromDto(EmailEntity entity, EmailDetailDto dto)
        {
            var contactData = ExtractContactFormData(dto);

            entity.FromName = dto.FromName;
            entity.FromEmail = dto.FromEmail;
            entity.Subject = dto.Subject;
            entity.Snippet = dto.Snippet;
            entity.ReceivedUtc = dto.ReceivedUtc;
            entity.ReceivedLocal = dto.ReceivedLocal;
            entity.Unread = dto.Unread;
            entity.HasAttachments = dto.HasAttachments;
            entity.HtmlBody = dto.HtmlBody;
            entity.TextBody = dto.TextBody;
            entity.LabelsJson = SerializeLabels(dto.Labels);
            entity.Company = contactData.Company;
            entity.Phone = contactData.Phone;
            entity.GstNumber = contactData.GstNumber;
            entity.Message = contactData.Message;
            entity.IsContactForm = contactData.IsContactForm;

            // Update attachments
            entity.EmailAttachments.Clear();
            foreach (var attachment in dto.Attachments)
            {
                entity.EmailAttachments.Add(new EmailAttachmentEntity
                {
                    EmailId = entity.Id,
                    AttachmentId = attachment.AttachmentId,
                    FileName = attachment.FileName,
                    SizeBytes = attachment.SizeBytes,
                    ContentType = attachment.ContentType
                });
            }
        }

        private (string? Company, string? Phone, string? GstNumber, string? Message, bool IsContactForm) ExtractContactFormData(EmailDetailDto dto)
        {
            // Check if this is a contact form email
            bool isContactForm = dto.FromName == "Contact from Website" ||
                               dto.FromEmail.Contains("contact@") ||
                               dto.Subject.ToLower().Contains("contact form");

            if (!isContactForm)
            {
                return (dto.Company, dto.Phone, dto.GST, dto.Message, false);
            }

            string content = dto.TextBody ?? dto.HtmlBody ?? string.Empty;

            // Extract contact form fields using simple string parsing
            var company = dto.Company ?? ExtractField(content, new[] { "company", "organization", "business" });
            var phone = dto.Phone ?? ExtractField(content, new[] { "phone", "mobile", "contact number", "tel" });
            var gst = dto.GST ?? ExtractField(content, new[] { "gst", "tax", "gstin" });
            var message = dto.Message ?? ExtractField(content, new[] { "message", "inquiry", "details", "comment" });

            return (company, phone, gst, message, true);
        }

        private string? ExtractField(string content, string[] fieldNames)
        {
            if (string.IsNullOrEmpty(content)) return null;

            foreach (var fieldName in fieldNames)
            {
                var pattern = $@"{fieldName}[\s]*[:=][\s]*(.+?)(?:\n|$)";
                var match = System.Text.RegularExpressions.Regex.Match(content, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return null;
        }

        // Helper methods for JSON serialization/deserialization
        private List<string> ParseLabelsJson(string? labelsJson)
        {
            if (string.IsNullOrEmpty(labelsJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(labelsJson) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize labels JSON: {LabelsJson}", labelsJson);
                return new List<string>();
            }
        }

        private string? SerializeLabels(List<string> labels)
        {
            if (labels == null || !labels.Any())
                return null;

            try
            {
                return JsonSerializer.Serialize(labels);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to serialize labels to JSON");
                return null;
            }
        }
    }
}