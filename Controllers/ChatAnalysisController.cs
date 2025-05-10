using ChatInsightGemini;
using ChatInsightGemini.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

[ApiController]
[Route("api/[controller]")]
public class ChatAnalysisController : ControllerBase
{
    private readonly GeminiClient _geminiClient;
    private readonly ConversationProcessor _conversationProcessor;

    public ChatAnalysisController(GeminiClient geminiClient, ConversationProcessor conversationProcessor)
    {
        _geminiClient = geminiClient;
        _conversationProcessor = conversationProcessor;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] ChatRequest request)
    {
        var result = await _geminiClient.AnalyzeChatAsync(request.Conversation, request.Type);
        return Ok(new { analysis = result });
    }

    [HttpPost("DetectUsers")]
    public async Task<IActionResult> DetectUsers([FromForm] IFormFile chatFile, [FromForm] string conversation, [FromForm] int dayCount)
    {
        try
        {
            // Konuşma metnini işle
            var (processedText, includedDates) = await _conversationProcessor.ProcessInputAsync(conversation, chatFile, dayCount);

            if (string.IsNullOrWhiteSpace(processedText))
            {
                return BadRequest(new { error = "Geçerli bir konuşma metni bulunamadı." });
            }

            // Konuşmadaki kişileri tespit et
            var speakerStats = _conversationProcessor.ExtractSpeakersFromConversation(processedText);

            if (speakerStats.Count == 0)
            {
                return BadRequest(new { error = "Konuşmada hiçbir kişi tespit edilemedi." });
            }

            // Kişileri mesaj sayılarına göre sırala
            var speakers = speakerStats
                .Select(kvp => new { name = kvp.Key, messageCount = kvp.Value })
                .OrderByDescending(s => s.messageCount)
                .ToList();

            return Ok(new
            {
                speakers = speakers,
                conversationText = processedText,
                includedDates = includedDates
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpPost("GenerateResponse")]
    public async Task<IActionResult> GenerateResponse([FromBody] ChatResponseRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PersonName) || string.IsNullOrWhiteSpace(request.UserMessage))
            {
                return BadRequest(new { error = "Kişi adı ve kullanıcı mesajı gereklidir." });
            }

            // Sohbet geçmişini ve konuşma metnini kullanarak yapay cevap üret
            string chatHistoryText = "";
            if (request.ChatHistory != null && request.ChatHistory.Count > 0)
            {
                chatHistoryText = "Mevcut sohbet:\n";
                foreach (var message in request.ChatHistory)
                {
                    chatHistoryText += $"{(message.Role == "user" ? "Ben" : request.PersonName)}: {message.Message}\n";
                }
            }

            string response = await _geminiClient.GenerateChatResponseAsync(
                request.Conversation,
                request.PersonName,
                request.UserMessage,
                chatHistoryText
            );

            return Ok(new { response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Bir hata oluştu: {ex.Message}" });
        }
    }
}

public class ChatRequest
{
    public string Conversation { get; set; }
    public string Type { get; set; }
}

public class ChatResponseRequest
{
    public string Conversation { get; set; }
    public string PersonName { get; set; }
    public string UserMessage { get; set; }
    public List<ChatMessage> ChatHistory { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } // "user" veya "assistant"
    public string Message { get; set; }
}