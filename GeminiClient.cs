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
        string prompt = analysisType switch
        {
            "flirt" => $"Bu bir flört konuşması. Konuşmadaki kişilerin ilgi, empati ve tutumlarını analiz et. Aralarındaki duygusal dinamiği, sohbetin samimiyetini, olası ilişki potansiyelini değerlendir:\n{conversation}",

            "conflict" => $"Bu bir tartışma/anlaşmazlık konuşması. Her iki tarafın argümanlarını, güçlü ve zayıf yönlerini analiz et. Anlaşmazlığın kaynağını belirle ve olası çözüm yolları öner:\n{conversation}",

            "friendship" => $"Bu bir arkadaşlar arası konuşma. Arkadaşlık dinamiklerini, samimiyet düzeyini, ortak ilgi alanlarını ve iletişim şekillerini analiz et. Arkadaşlık bağının gücünü değerlendir:\n{conversation}",

            "family" => $"Bu bir aile üyeleri arasında geçen konuşma. Aile dinamiklerini, roller arasındaki ilişkileri, duygusal bağları ve iletişim kalıplarını analiz et. Ailenin ilişki yapısını değerlendir:\n{conversation}",

            "professional" => $"Bu bir iş/profesyonel ortamda geçen konuşma. İş ilişkilerini, profesyonel iletişim düzeyini, iş süreçlerini ve çalışma dinamiklerini analiz et. Profesyonel ilişkinin etkinliğini değerlendir:\n{conversation}",

            "general" => $"Bu bir sohbet. Konuşmacıların iletişim tarzlarını, konu değişimlerini, duygu tonlarını ve etkileşim kalıplarını analiz et. Konuşmanın genel havasını ve katılımcıların birbirlerine karşı tutumlarını değerlendir:\n{conversation}",

            _ => $"Sohbeti analiz et. Katılımcıların iletişim stillerini, konuşma konularını ve genel diyalog dinamiklerini değerlendir:\n{conversation}"
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

    public async Task<string> SuggestResponsesAsync(string conversation)
    {
        string prompt = $"Aşağıdaki konuşmada son mesaja verilecek 5 farklı olası cevap öner. Her bir cevap farklı bir yaklaşım içermeli (örn: kabul etme, reddetme, erteleme, soru sorma, vb.). Cevapları maddeler halinde numara vererek liste. Cevaplar doğal, samimi ve konuşmanın akışına uygun olmalı:\n\n{conversation}";

        // Gemini API'si için JSON payload hazırla
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