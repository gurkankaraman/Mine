using Microsoft.AspNetCore.Mvc;

namespace ChatInsightGemini.Controllers
{
    public class HomeController : Controller
    {
        private readonly GeminiClient _geminiClient;

        public HomeController(GeminiClient geminiClient)
        {
            _geminiClient = geminiClient;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string conversation, IFormFile chatFile, int dayCount, string type)
        {
            try
            {
                string inputText = "";

                // Dosya mı, metin mi kontrolü
                if (chatFile != null && chatFile.Length > 0)
                {
                    // Dosyadan konuşmayı al
                    using (var reader = new StreamReader(chatFile.OpenReadStream()))
                    {
                        inputText = await reader.ReadToEndAsync();
                    }
                }
                else if (!string.IsNullOrWhiteSpace(conversation))
                {
                    // Metin alanından konuşmayı al
                    inputText = conversation;
                }
                else
                {
                    ViewBag.Error = "Lütfen bir sohbet metni girin veya bir dosya yükleyin.";
                    return View();
                }

                // Son X gün konuşmalarını filtrele
                var (filteredConversation, includedDates) = FilterConversationByDays(inputText, dayCount);

                if (string.IsNullOrWhiteSpace(filteredConversation))
                {
                    ViewBag.Error = $"Son {dayCount} güne ait konuşma bulunamadı.";
                    return View();
                }

                var result = await _geminiClient.AnalyzeChatAsync(filteredConversation, type);
                ViewBag.AnalysisResult = result;
                ViewBag.FilteredConversation = filteredConversation;
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