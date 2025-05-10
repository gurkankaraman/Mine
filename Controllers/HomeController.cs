using ChatInsightGemini.Services;
using Microsoft.AspNetCore.Mvc;

using ChatInsightGemini.Controllers;


namespace ChatInsightGemini.Controllers
{
    public class HomeController : Controller
    {
        private readonly GeminiClient _geminiClient;
        private readonly ConversationProcessor _conversationProcessor;

        public HomeController(GeminiClient geminiClient, ConversationProcessor conversationProcessor)
        {
            _geminiClient = geminiClient;
            _conversationProcessor = conversationProcessor;
        }

        // Ana sayfa - sadece seçim sayfasını göster
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Analiz sayfası - GET
        [HttpGet]
        public IActionResult Analyze()
        {
            return View();
        }

        // Analiz sayfası - POST
        [HttpPost]
        public async Task<IActionResult> Analyze(string conversation, IFormFile chatFile, int dayCount, string type)
        {
            try
            {
                // Metni işle
                var (processedText, includedDates) = await _conversationProcessor.ProcessInputAsync(conversation, chatFile, dayCount);

                // Boş metin kontrolü
                if (string.IsNullOrWhiteSpace(processedText))
                {
                    if (chatFile != null && chatFile.Length > 0)
                    {
                        ViewBag.Error = $"Son {dayCount} güne ait konuşma bulunamadı.";
                    }
                    else
                    {
                        ViewBag.Error = "Lütfen bir sohbet metni girin veya bir dosya yükleyin.";
                    }
                    return View();
                }
                // Temel metrikleri hesapla
                var (basicMetrics, mostUsedWord, mostUsedEmoji, speakerStats) = _conversationProcessor.GetAdvancedMetrics(processedText, includedDates);

                // Sayısal hesaplar
                var numericMetrics = _conversationProcessor.CalculateBasicMetrics(processedText, includedDates);


                // Analiz işlemine devam et

                var result = await _geminiClient.AnalyzeChatAsync(processedText, type);

                // ViewBag değerlerini ata
                ViewBag.NumericAnalysis = basicMetrics;
                ViewBag.MostUsedWord = mostUsedWord;
                ViewBag.MostUsedEmoji = mostUsedEmoji;
                ViewBag.SpeakerStats = speakerStats;
                ViewBag.AnalysisResult = result;
                ViewBag.NumericAnalysis = numericMetrics;
                ViewBag.FilteredConversation = processedText;
                ViewBag.IncludedDates = includedDates;
                ViewBag.DayCount = dayCount;


                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Hata oluştu: {ex.Message}";
                return View();
            }
        }

        // Cevap önerileri sayfası - GET
        [HttpGet]
        public IActionResult SuggestResponses()
        {
            return View();
        }

        // Cevap önerileri sayfası - POST
        [HttpPost]
        public async Task<IActionResult> SuggestResponses(string conversation, IFormFile chatFile, int dayCount)
        {
            try
            {
                // Metni işle - aynı ProcessConversationInput metodunu kullan
                var (processedText, includedDates) = await ProcessConversationInput(conversation, chatFile, dayCount);

                // Boş metin kontrolü
                if (string.IsNullOrWhiteSpace(processedText))
                {
                    if (chatFile != null && chatFile.Length > 0)
                    {
                        ViewBag.Error = $"Son {dayCount} güne ait konuşma bulunamadı.";
                    }
                    else
                    {
                        ViewBag.Error = "Lütfen bir sohbet metni girin veya bir dosya yükleyin.";
                    }
                    return View();
                }

                // Cevap önerileri al
                var result = await _geminiClient.SuggestResponsesAsync(processedText);
                ViewBag.SuggestedResponses = result;
                ViewBag.OriginalConversation = processedText;
                ViewBag.IncludedDates = includedDates;
                ViewBag.DayCount = dayCount;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Hata oluştu: {ex.Message}";
                return View();
            }
        }


        // Yapay Sohbet sayfası - GET
        [HttpGet]
        public IActionResult ArtificialChat()
        {
            return View();
        }

        
        private async Task<(string ProcessedText, List<DateTime> IncludedDates)> ProcessConversationInput(string conversation, IFormFile chatFile, int dayCount)
        {
            string inputText = "";
            List<DateTime> includedDates = new List<DateTime>();

            // Dosya mı, metin mi kontrolü
            if (chatFile != null && chatFile.Length > 0)
            {
                // Dosyadan konuşmayı al
                using (var reader = new StreamReader(chatFile.OpenReadStream()))
                {
                    inputText = await reader.ReadToEndAsync();
                }

                // Dosyadan gelen konuşmayı filtrele
                var (filteredConversation, dates) = FilterConversationByDays(inputText, dayCount);

                if (string.IsNullOrWhiteSpace(filteredConversation))
                {
                    // Boş döndüğünde hata oluştur, bu durumu controller'da kontrol edebiliriz
                    return (string.Empty, new List<DateTime>());
                }

                // Filtrelenmiş metni ve tarihleri döndür
                return (filteredConversation, dates);
            }
            else if (!string.IsNullOrWhiteSpace(conversation))
            {
                // Metin alanından konuşmayı al ve filtreleme YAPMA
                return (conversation, new List<DateTime>());
            }

            // Her iki durumda da veri yoksa boş değer döndür
            return (string.Empty, new List<DateTime>());
        }

        private (string FilteredText, List<DateTime> IncludedDates) FilterConversationByDays(string conversation, int dayCount)
        {
            var lines = conversation.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredLines = new List<string>();
            var includedDates = new HashSet<DateTime>();

            DateTime cutoffDate = DateTime.Now.AddDays(-dayCount + 1);
            DateTime lastDetectedDate = DateTime.MinValue;
            bool isInRange = false;

            foreach (var line in lines)
            {
                bool dateFound = false;

                // Yeni WhatsApp formatı: "3.05.2025 16:33 - Gürkan Karaman: mesaj"
                var dateParts = line.Split(new[] { ' ', '-' }, 4); // En fazla 4 parçaya böl

                if (dateParts.Length >= 3)
                {
                    try
                    {
                        // Tarih ve saat kısımlarını al
                        string dateStr = dateParts[0]; // 3.05.2025
                        string timeStr = dateParts[1]; // 16:33

                        // Tarihi parse et
                        if (DateTime.TryParse($"{dateStr} {timeStr}", out DateTime messageDate))
                        {
                            lastDetectedDate = messageDate;
                            isInRange = messageDate >= cutoffDate;
                            dateFound = true;

                            if (isInRange)
                            {
                                // Sadece tarih bileşenini ekleyelim (saat olmadan)
                                includedDates.Add(messageDate.Date);
                            }
                        }
                    }
                    catch
                    {
                        // Parse hatası olursa yok say
                    }
                }

                // Eğer tarih satırı değilse ve önceki tarih aralıktaysa dahil et
                if (isInRange || (lastDetectedDate >= cutoffDate && !dateFound))
                {
                    filteredLines.Add(line);
                }
            }

            return (string.Join(Environment.NewLine, filteredLines), includedDates.OrderBy(d => d).ToList());
        }
    }
}