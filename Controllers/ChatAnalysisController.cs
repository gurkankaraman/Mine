using ChatInsightGemini;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ChatAnalysisController : ControllerBase
{
    private readonly GeminiClient _geminiClient;

    public ChatAnalysisController(GeminiClient geminiClient)
    {
        _geminiClient = geminiClient;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] ChatRequest request)
    {
        var result = await _geminiClient.AnalyzeChatAsync(request.Conversation, request.Type);
        return Ok(new { analysis = result });
    }
}

public class ChatRequest
{
    public string Conversation { get; set; }
    public string Type { get; set; } // "flirt" or "conflict"
}