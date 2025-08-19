using System.ComponentModel.DataAnnotations;

namespace IndustrialSolutions.Models
{
    public class ContactModel
    {
        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } // user's email

        [StringLength(15)]
        public string GSTNumber { get; set; }

        [StringLength(150)]
        public string Company { get; set; }

        [Required, StringLength(50)]
        public string Subject { get; set; }

        [Required, StringLength(4000)]
        public string Message { get; set; }
    }
}