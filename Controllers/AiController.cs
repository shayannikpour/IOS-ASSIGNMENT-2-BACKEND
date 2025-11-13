using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AiMiddleTier.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protect all AI endpoints w/ JWT authentication
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
            // Input validation
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return BadRequest(new { Message = "Prompt cannot be empty." });
            }

            if (prompt.Length > 4000)
            {
                return BadRequest(new { Message = "Prompt too long. Maximum 4000 characters allowed." });
            }

            var token = _config["GitHub:Token"];
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { Message = "AI service temporarily unavailable." });
            }

            // Required headers for GitHub Models
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AiMiddleTierApp");

            // Enhanced general helpful assistant system prompt for domain grounding
            var systemPrompt = @"You are a great assistant dedicated to providing accurate and factual information. Your core principles are:

1. **Accuracy First**: Always strive to provide the most accurate and up-to-date information available
2. **Factual Foundation**: Base your responses on verified facts and reliable sources
3. **Honest About Limitations**: If you don't know something, be transparent about it
4. **Guidance When Uncertain**: When you lack specific information, provide clear instructions on how the user can find accurate information
5. **Helpful Resources**: Suggest appropriate sources, websites, contacts, or methods for obtaining reliable information

Response Guidelines:
- Provide clear, well-structured answers
- Cite general knowledge areas when appropriate
- If uncertain, say 'I don't have specific information about this, but here's how you can find accurate details...'
- Suggest authoritative sources like official websites, government agencies, academic institutions, or professional organizations
- Maintain a helpful, supportive, and professional tone
- Ask clarifying questions if the request is ambiguous

Your goal is to be genuinely helpful while maintaining the highest standards of information accuracy and reliability.";

            var body = new
            {
                model = "gpt-4o-mini", // model hosted on GitHub Models
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1000
            };

            // Correct endpoint for GitHub Models API
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

                return Ok(new
                {
                    Response = message,
                    Model = "gpt-4o-mini",
                    Domain = "BCIT Assistant"
                });
            }
            catch
            {
                return Ok(result);
            }
        }
    }
}
