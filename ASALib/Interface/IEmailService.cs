using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASALib.Model;

namespace ASALib.Interface
{
    public interface IEmailService
    {
        Task<bool> SendContactEmailAsync(ContactModel contactModel);
        Task<bool> SendConfirmationEmailAsync(ContactModel contactModel);
    }
}
