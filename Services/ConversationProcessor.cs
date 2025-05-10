using System.Text.RegularExpressions;

namespace ChatInsightGemini.Services
{
    public class ConversationProcessor
    {
        public async Task<(string ProcessedText, List<DateTime> IncludedDates)> ProcessInputAsync(string conversation, IFormFile chatFile, int dayCount)
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

        public Dictionary<string, double> CalculateBasicMetrics(string conversation, List<DateTime> includedDates)
        {
            var metrics = new Dictionary<string, double>();

            // Cümle sayısı
            int sentenceCount = Regex.Matches(conversation, @"[.!?]+").Count;
            metrics.Add("Cümle Sayısı", sentenceCount);

            // Kelime sayısı
            string[] words = conversation.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            metrics.Add("Kelime Sayısı", words.Length);

            // Ortalama kelime uzunluğu
            if (words.Length > 0)
            {
                double avgWordLength = words.Average(w => w.Length);
                metrics.Add("Ortalama Kelime Uzunluğu", Math.Round(avgWordLength, 2));
            }

            // Emoji sayısı (Basit bir emoji regex)
            int emojiCount = Regex.Matches(conversation, @"[\u1F600-\u1F64F\u1F300-\u1F5FF\u1F680-\u1F6FF\u1F700-\u1F77F\u1F780-\u1F7FF\u1F800-\u1F8FF\u1F900-\u1F9FF\u1FA00-\u1FA6F\u1FA70-\u1FAFF]").Count;
            metrics.Add("Emoji Sayısı", emojiCount);

            // Tarih aralığı (günler)
            if (includedDates.Count > 1)
            {
                TimeSpan timeRange = includedDates.Max() - includedDates.Min();
                metrics.Add("Tarih Aralığı (gün)", timeRange.TotalDays);
            }

            return metrics;
        }

        public (Dictionary<string, string> BasicMetrics, string MostUsedWord, string MostUsedEmoji, Dictionary<string, int> SpeakerStats) GetAdvancedMetrics(string conversation, List<DateTime> includedDates)
        {
            var basicMetrics = new Dictionary<string, string>();
            var speakerStats = ExtractSpeakersFromConversation(conversation);
            string mostUsedWord = GetMostUsedWord(conversation);
            string mostUsedEmoji = GetMostUsedEmoji(conversation);

            // Konuşma Uzunluğu
            basicMetrics.Add("Konuşma Uzunluğu", $"{conversation.Length} karakter");

            // Mesaj Sayısı
            int messageCount = Regex.Matches(conversation, @":\s").Count;
            basicMetrics.Add("Toplam Mesaj Sayısı", messageCount.ToString());

            // Konuşma Süresi
            if (includedDates.Count > 1)
            {
                TimeSpan timeRange = includedDates.Max() - includedDates.Min();
                basicMetrics.Add("Konuşma Süresi", $"{timeRange.Days} gün");
            }

            return (basicMetrics, mostUsedWord, mostUsedEmoji, speakerStats);
        }

        public Dictionary<string, int> ExtractSpeakersFromConversation(string conversation)
        {
            var speakerStats = new Dictionary<string, int>();
            var lines = conversation.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Try to match WhatsApp message format: "Date Time - Speaker: Message"
                var match = Regex.Match(line, @".*?-\s*[""']?(.*?)[""']?:\s");
                if (match.Success && match.Groups.Count > 1)
                {
                    string speaker = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(speaker))
                    {
                        if (speakerStats.ContainsKey(speaker))
                            speakerStats[speaker]++;
                        else
                            speakerStats[speaker] = 1;
                    }
                }
            }

            return speakerStats;
        }

        private string GetMostUsedWord(string conversation)
        {
            // Mesajları çıkar (tarih ve isim kısımlarını kaldır)
            var messageTextOnly = Regex.Replace(conversation, @"\d{1,2}\.\d{1,2}\.\d{4}\s\d{1,2}:\d{1,2}\s-\s.*?:\s", " ");

            // Sadece kelimeler
            var words = Regex.Matches(messageTextOnly.ToLower(), @"\b[a-zğüşıöç]{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(w => !IsStopWord(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(1)
                .FirstOrDefault();

            return words != null ? $"{words.Key} ({words.Count()} kez)" : "Bulunamadı";
        }

        private bool IsStopWord(string word)
        {
            // Türkçe stop words (edat, bağlaç vs.)
            var stopWords = new HashSet<string> { "bir", "ve", "ile", "için", "bu", "da", "de", "mi", "ama", "fakat", "çünkü", "ise", "ne", "ki", "diye" };
            return stopWords.Contains(word);
        }

        private string GetMostUsedEmoji(string conversation)
        {
            // Basit bir emoji regex (tam değil)
            var emojiMatches = Regex.Matches(conversation, @"[\u1F600-\u1F64F\u1F300-\u1F5FF\u1F680-\u1F6FF\u1F700-\u1F77F\u1F780-\u1F7FF\u1F800-\u1F8FF\u1F900-\u1F9FF\u1FA00-\u1FA6F\u1FA70-\u1FAFF]|😊|😂|❤️|👍|🙏|😍|😒|👌|🤔");

            if (emojiMatches.Count == 0)
                return "Emoji kullanılmamış";

            var mostUsedEmoji = emojiMatches
                .Cast<Match>()
                .Select(m => m.Value)
                .GroupBy(e => e)
                .OrderByDescending(g => g.Count())
                .Take(1)
                .FirstOrDefault();

            return mostUsedEmoji != null ? $"{mostUsedEmoji.Key} ({mostUsedEmoji.Count()} kez)" : "Bulunamadı";
        }
    }
}