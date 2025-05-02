using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace ChatInsightGemini;

public class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _url;

    public GeminiClient(IConfiguration config, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"];
        _url = config["Gemini:Url"];
    }

    public async Task<string> AnalyzeChatAsync(string conversation, string analysisType)
    {
        // Prompt oluşturuluyor
        string prompt = analysisType switch
        {
            "flirt" => $"Bu bir flört konuşması. Tarafların ilgi, empati ve tutumlarını analiz et:\n{conversation}",
            "conflict" => $"Bu bir tartışma. Her iki tarafın güçlü ve zayıf yönlerini analiz et:\n{conversation}",
            _ => $"Sohbeti analiz et:\n{conversation}"
        };

        // ✅ Gemini'nin beklediği JSON formatı
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        // JSON'a çevir
        var json = JsonConvert.SerializeObject(payload);

        // İstek hazırla
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_url}?key={_apiKey}" // ❗ API key burada query string içinde olmalı
        );

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // İstek gönder ve kontrol et
        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini API HATASI ({(int)response.StatusCode}): {responseContent}");
        }

        dynamic result = JsonConvert.DeserializeObject(responseContent);
        return result?.candidates?[0]?.content?.parts?[0]?.text ?? "Yanıt alınamadı.";
    }
}