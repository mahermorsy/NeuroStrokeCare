using System.Threading.Tasks;

namespace NeuroStrokeCare.Service.Email
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}
