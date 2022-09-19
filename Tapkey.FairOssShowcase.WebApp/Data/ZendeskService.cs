using Tapkey.FairOssShowcase.Webapp;
using Tapkey.FairOssShowcase.WebApp.Model;
using ZendeskApi_v2;
using ZendeskApi_v2.Models.Tickets;

namespace Tapkey.FairOssShowcase.WebApp.Data
{
    public class ZendeskService
    {
        private AppConfig _appConfig;
        private ZendeskApi _zendeskApi;

        public ZendeskService(AppConfig appConfig)
        {
            _appConfig = appConfig;
            _zendeskApi = new ZendeskApi(_appConfig.ZendeskUrl, _appConfig.ZendeskUser, _appConfig.ZendeskApiToken, _appConfig.ZendeskLocale);
        }

        public async Task<bool> CreateZendeskTicket(User user)
        {
            if (!_appConfig.ZendeskEnabled) return false;

            var ticket = new Ticket()
            {
                AssigneeId = _appConfig.ZendeskAssigneeId,
                Subject = _appConfig.FairTitle,
                Comment = new Comment()
                {
                    AuthorId = _appConfig.ZendeskAssigneeId,
                    HtmlBody = GetHtmlBody(user),
                    Public = false
                },
                Requester = new Requester()
                {
                    Email = user.Email,
                    Name = $"{user.FirstName} {user.LastName}"
                },
                CreatedAt = DateTime.UtcNow,
                Tags = new List<string> { "tapkey_sales" }
            };

            try
            {
                var result = await _zendeskApi.Tickets.CreateTicketAsync(ticket);
                return result?.Ticket?.Id != null;
            } catch
            {
                return false;
            }
        }

        private string GetHtmlBody(User user)
        {
            var firstName = $"<tr><th>First Name</th><td>{user.FirstName}</td></tr>";
            var lastName = $"<tr><th>Last Name</th><td>{user.LastName}</td></tr>";
            var email = $"<tr><th>Email</th><td>{user.Email}</td></tr>";
            var phone = $"<tr><th>Phone</th><td>{user.PhoneNumber}</td></tr>";
            var company = $"<tr><th>Company</th><td>{user.Company}</td></tr>";
            var position = $"<tr><th>Position</th><td>{user.Position}</td></tr>";

            return $"<div>An user has registered on {_appConfig.FairTitle}:</div><table>{firstName}{lastName}{email}{phone}{company}{position}</table>";
        }
    }
}
