using Backend.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Backend
{
    public class TurnstileService
    {
        private TurnstileSettings _turnstile;

        public TurnstileService(IOptions<TurnstileSettings> turnstile)
        {
            _turnstile = turnstile.Value;
        }

        public async Task<bool> VerifyTurnstileAsync(string token)
        {
            var secret = _turnstile.SecretKey;

            using var client = new HttpClient();
            var data = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("secret", secret),
            new KeyValuePair<string, string>("response", token)
            });

            var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", data);
            var json = await response.Content.ReadAsStringAsync();

            dynamic? result = JsonConvert.DeserializeObject(json);

            if (result == null)
                return false;

            return result.success == true;
        }

    }
}
