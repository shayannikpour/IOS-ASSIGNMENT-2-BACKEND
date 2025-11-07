using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AiMiddleTier.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AiController(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        [HttpPost("prompt")]
        public async Task<IActionResult> SendPrompt([FromBody] string prompt)
        {
            var token = _config["GitHub:Token"];
            if (string.IsNullOrEmpty(token))
                return BadRequest("GitHub token not found in configuration.");

            // ✅ Required headers for GitHub Models
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AiMiddleTierApp");

            // ✅ Correct request body format
            var body = new
            {
                model = "gpt-4o-mini", // model hosted on GitHub Models
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant focused on BCIT-related topics." },
                    new { role = "user", content = prompt }
                }
            };

            // ✅ Correct endpoint for GitHub Models API
            var response = await _httpClient.PostAsJsonAsync(
                "https://models.inference.ai.azure.com/chat/completions", body);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, error);
            }

            var result = await response.Content.ReadAsStringAsync();

            try
            {
                // Parse and extract the model’s reply cleanly
                using var jsonDoc = JsonDocument.Parse(result);
                var message = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return Ok(new { response = message });
            }
            catch
            {
                return Ok(result);
            }
        }
    }
}
