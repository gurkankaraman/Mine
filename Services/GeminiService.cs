using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = "AIzaSyARD5I-B9Xm2bFPYNf30fItAvxvcJZIA2c";

    public GeminiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetGeminiResponse(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
               new {
                   parts = new[]
                   {
                       new { text = prompt }
                   }
               }
           }
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine(json);

        var result = JsonConvert.DeserializeObject<dynamic>(json);
        return result?.candidates?[0]?.content?.parts?[0]?.text ?? "Yanıt alınamadı.";
    }
}
