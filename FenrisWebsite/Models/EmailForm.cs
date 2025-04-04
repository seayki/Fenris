using System.Text.RegularExpressions;

namespace FenrisWebsite.Models
{
    public class EmailForm
    {
        public string? UserEmail { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }

        public EmailForm()
        {

        }
        public bool ValidateEmail()
        {
            if (string.IsNullOrWhiteSpace(UserEmail))
                return true; 

            // Basic email pattern
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(UserEmail, emailPattern, RegexOptions.IgnoreCase);
        }

        public bool ValidateSubject()
        {
            return !string.IsNullOrWhiteSpace(Subject);
        }

        public bool ValidateMessage()
        {
            return !string.IsNullOrWhiteSpace(Message); 
        }
    }
}
