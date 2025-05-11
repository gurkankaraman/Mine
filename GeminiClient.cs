using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
        _apiKey = config["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey is missing in configuration.");
        _url = config["Gemini:Url"] ?? throw new ArgumentNullException("Gemini:Url is missing in configuration.");
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

        dynamic? result = JsonConvert.DeserializeObject(responseContent);
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

        dynamic? result = JsonConvert.DeserializeObject(responseContent);
        return result?.candidates?[0]?.content?.parts?[0]?.text ?? "Yanıt alınamadı.";
    }

    public async Task<string> GenerateChatResponseAsync(string conversation, string personName, string userMessage, string chatHistory = "")
    {
        // Daha doğal ve gerçekçi cevaplar için iyileştirilmiş prompt
        string prompt = $@"Bu bir WhatsApp sohbet simülasyonudur. '{personName}' adlı kişinin yerine geçeceksin. 

        AŞAĞIDAKİ TALİMATLAR ÇOK ÖNEMLİDİR, TAM OLARAK UYGULANMALIDIR:
        1. Asla açıklama yapmayacaksın ve asla bir yapay zeka olduğunu belirtmeyeceksin
        2. Sadece '{personName}' olarak yanıt vereceksin
        3. Yanıtın sadece '{personName}'in vereceği yanıt olmalı - hiçbir başlık, etiket veya açıklama ekleme
        4. Gerçekçi bir WhatsApp sohbeti gibi kısa ve doğal cevaplar ver (genellikle 1-3 cümle) 
        5. '{personName}'in tarzını taklit et: emojileri, kısaltmaları, yazım hatalarını, noktalama işaretlerini ve hitap şeklini aynen kullan
        6. Asla uzun açıklamalar, analizler veya yapay zeka çıktısı gibi görünen metinler yazma

        İşte '{personName}'in yer aldığı WhatsApp konuşması:
        {conversation}

        İşte şu anki sohbet geçmişimiz:
        {chatHistory}

        Şimdi bu son mesaja '{personName}' olarak cevap ver:
        Ben: {userMessage}";

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
        },
            generationConfig = new
            {
                temperature = 0.9,           // Daha yaratıcı cevaplar için biraz yükseltildi
                topK = 40,                  // Daha doğal dil için
                topP = 0.95,                // Daha insansı yanıtlar için
                maxOutputTokens = 150,      // Kısa cevaplar için sınırlama
                stopSequences = new[] { "Ben:", "Yapay", "AI:", "Asistan" }  // Bu kelimelerle bitirmeyi durdur
            }
        };

        // JSON'a çevir
        var json = JsonConvert.SerializeObject(payload);

        // İstek hazırla
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_url}?key={_apiKey}" // API key burada query string içinde
        );
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // İstek gönder ve kontrol et
        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini API HATASI ({(int)response.StatusCode}): {responseContent}");
        }

        dynamic? result = JsonConvert.DeserializeObject(responseContent);
        string aiResponse = result?.candidates?[0]?.content?.parts?[0]?.text ?? "Yanıt alınamadı.";

        // Cevabı temizle ve düzenle
        return CleanResponse(aiResponse, personName);
    }

    // Cevabı temizleme fonksiyonu
    private string CleanResponse(string response, string personName)
    {
        // Fazladan açıklamaları ve etiketleri temizle
        response = Regex.Replace(response, $@"^({personName}:|{personName}\s*:)", "", RegexOptions.IgnoreCase);
        response = Regex.Replace(response, @"^[""'\s]+", ""); // Baştaki gereksiz karakterleri temizle
    
    // Yapay zeka açıklamalarını temizle
        response = Regex.Replace(response, @"(yapay zeka olarak|ai olarak|bir asistan olarak|bir yapay zeka asistanı olarak).*", "", RegexOptions.IgnoreCase);

        // "Ben bir yapay zeka değilim" gibi ifadeleri temizle
        response = Regex.Replace(response, @"(ben bir (yapay zeka|ai|asistan|bot) değilim).*", "", RegexOptions.IgnoreCase);

        // Birden fazla boşlukları tek boşluğa çevir
        response = Regex.Replace(response, @"\s+", " ").Trim();

        // Çok uzun cevapları kısalt (WhatsApp'ta tipik bir yanıt genellikle kısadır)
        if (response.Length > 300)
        {
            var sentences = Regex.Split(response, @"(?<=[.!?])\s+");
            if (sentences.Length > 3)
            {
                response = string.Join(" ", sentences.Take(3));
            }
        }

        return response.Trim();
    }
}