using FenrisWebsite.Models;

namespace FenrisWebsite.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailForm emailForm);
    }
}
