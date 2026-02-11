using System.Text.Json;

namespace HarvestHavenSecurePortal.Services
{
    public class CaptchaV3Service
    {
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public CaptchaV3Service(IHttpClientFactory http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<(bool ok, string reason, double score)> VerifyAsync(string token, string action)
        {
            var secret = _config["GoogleReCaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
                return (false, "Missing reCAPTCHA secret key", 0);

            if (string.IsNullOrWhiteSpace(token))
                return (false, "Missing token", 0);

            var client = _http.CreateClient();

            var form = new Dictionary<string, string>
            {
                ["secret"] = secret!,
                ["response"] = token
            };

            using var content = new FormUrlEncodedContent(form);
            using var resp = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            var json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            bool success = root.TryGetProperty("success", out var s) && s.GetBoolean();
            double score = root.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0.0;
            string respAction = root.TryGetProperty("action", out var a) ? a.GetString() ?? "" : "";

            if (!success) return (false, "Verification failed", score);
            if (!string.Equals(respAction, action, StringComparison.OrdinalIgnoreCase))
                return (false, "Action mismatch", score);

            double minScore = _config.GetValue<double>("GoogleReCaptcha:MinScore", 0.5);
            if (score < minScore) return (false, $"Low score ({score:0.00})", score);

            return (true, "OK", score);
        }
    }
}
