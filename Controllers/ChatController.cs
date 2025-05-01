using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly GeminiService _geminiService = new();

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        var response = await _geminiService.GetGeminiResponse(request.Prompt);
        return Ok(new { reply = response });
    }
}

public class ChatRequest
{
    public string Prompt { get; set; }
}
