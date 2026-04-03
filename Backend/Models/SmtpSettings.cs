namespace Backend.Models
{
    public class SmtpSettings
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Host {  get; set; } = null!;
        public int Port { get; set; }

    }
}
