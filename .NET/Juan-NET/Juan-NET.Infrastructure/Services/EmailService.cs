namespace Juan_NET.Infrastructure.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var settings = _configuration.GetSection("EmailSettings");
            var from = settings["From"] ?? string.Empty;
            var password = settings["Password"] ?? string.Empty;
            var host = settings["Host"] ?? "smtp.gmail.com";
            var port = int.TryParse(settings["Port"], out var configuredPort) ? configuredPort : 587;

            using var message = new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = true
            };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(from, password)
            };

            await client.SendMailAsync(message);
        }
    }
}
