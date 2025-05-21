using Backend.Models;
using Dadata.Model;
using DataBase.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Backend
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendRegistrationEmail(User user)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Priazov-Impact", "priazovimpact@gmail.com"));
            message.To.Add(new MailboxAddress("", user.Email));
            message.Subject = "Успешная регистрация";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "RegisterEmail.html");
            var htmlTemplate = await File.ReadAllTextAsync(templatePath);

            var htmlBody = htmlTemplate
                .Replace("[Имя пользователя]", user.Name)
                .Replace("[Почта пользователя]", user.Email)
                .Replace("[Телефон пользователя]", user.Phone);


            message.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Login, _smtpSettings.Password);
                await client.SendAsync(message);
                _logger.LogInformation($"Письмо успешно отправлено на {user.Email} ");
            }
            catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxBusy)
            {
                _logger.LogWarning(ex, "Почтовый ящик занят для пользователя {Email}. Повтор через 5 секунд...", user.Email);
                await Task.Delay(5000);
                await SendRegistrationEmail(user);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "Ошибка SMTP при отправке письма пользователю {Email}: {Message}", user.Email, ex.Message);
                throw new ApplicationException($"SMTP error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить письмо пользователю {Email}", user.Email);
                throw new ApplicationException($"Failed to send email: {ex.Message}", ex);
            }
            finally
            {
                _logger.LogInformation("Отключение от SMTP сервера после отправки письма пользователю {Email}", user.Email);
                await client.DisconnectAsync(true);
            }
        }

        public async Task SendPasswordResetEmail(string email, string resetCode)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Priazov-Impact", "priazovimpact@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Сброс пароля";
            message.Body = new TextPart("html")
            {
                Text = $"""
                <div style ="
                margin: 20px;
                padding: 5px;
                border: groove 2px black;"><p style = "font-size: 20px">Здравствуйте!</p>
                <p style = "font-size: 20px">Код для сброса пароля: {resetCode}</p>
                <p style = "font-size: 20px">Если вы ничего не запрашивали проигнорируйте это сообщение</p></div>
                """
            };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Login, _smtpSettings.Password);
                await client.SendAsync(message);
                _logger.LogInformation($"Письмо успешно отправлено на {email} ");
            }
            catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxBusy)
            {
                _logger.LogWarning(ex, "Почтовый ящик занят для пользователя {Email}. Повтор через 5 секунд...", email);
                await Task.Delay(5000);
                await SendPasswordResetEmail(email, resetCode);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "Ошибка SMTP при отправке письма пользователю {Email}: {Message}", email, ex.Message);
                throw new ApplicationException($"SMTP error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить письмо пользователю {Email}", email);
                throw new ApplicationException($"Failed to send email: {ex.Message}", ex);
            }
            finally
            {
                _logger.LogInformation("Отключение от SMTP сервера после отправки письма пользователю {Email}", email);
                await client.DisconnectAsync(true);
            }
        }

        public async Task SendPasswordOkayEmail(string email)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Priazov-Impact", "priazovimpact@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Пароль успешно изменён";
            message.Body = new TextPart("html")
            {
                Text = $"""
                <p style = "font-size: 20px">Здравствуйте!</p>
                <p style = "font-size: 20px">Ваш пароль был успешно изменён, если это были не вы
                срочно измените пароль по ссылке: </p>
                <p style = "font-size: 20px">Если вы ничего не запрашивали проигнорируйте это сообщение</p>
                """
            };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Login, _smtpSettings.Password);
                await client.SendAsync(message);
                _logger.LogInformation($"Письмо успешно отправлено на {email} ");
            }
            catch (SmtpCommandException ex) when (ex.StatusCode == SmtpStatusCode.MailboxBusy)
            {
                _logger.LogWarning(ex, "Почтовый ящик занят для пользователя {Email}. Повтор через 5 секунд...", email);
                await Task.Delay(5000);
                await SendPasswordOkayEmail(email);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "Ошибка SMTP при отправке письма пользователю {Email}: {Message}", email, ex.Message);
                throw new ApplicationException($"SMTP error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось отправить письмо пользователю {Email}", email);
                throw new ApplicationException($"Failed to send email: {ex.Message}", ex);
            }
            finally
            {
                _logger.LogInformation("Отключение от SMTP сервера после отправки письма пользователю {Email}", email);
                await client.DisconnectAsync(true);
            }
        }
    }
}
