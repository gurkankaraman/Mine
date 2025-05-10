using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace ChatInsightGemini.Services;

public class ConversationProcessor
{
    public async Task<(string ProcessedText, List<DateTime> IncludedDates)> ProcessInputAsync(string conversation, IFormFile chatFile, int dayCount)
    {
        if (chatFile != null && chatFile.Length > 0)
        {
            using var reader = new StreamReader(chatFile.OpenReadStream());
            string fileContent = await reader.ReadToEndAsync();
            return FilterByDateRange(fileContent, dayCount);
        }
        else if (!string.IsNullOrWhiteSpace(conversation))
        {
            return (conversation, new List<DateTime>());
        }

        return (string.Empty, new List<DateTime>());
    }

    public (string FilteredText, List<DateTime> IncludedDates) FilterByDateRange(string conversation, int dayCount)
    {
        var lines = conversation.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var filteredLines = new List<string>();
        var includedDates = new HashSet<DateTime>();
        DateTime cutoff = DateTime.Now.AddDays(-dayCount + 1);
        DateTime lastDate = DateTime.MinValue;
        bool isInRange = false;

        foreach (var line in lines)
        {
            var parts = line.Split(new[] { ' ', '-' }, 4);
            if (parts.Length >= 3 && DateTime.TryParse($"{parts[0]} {parts[1]}", out var dt))
            {
                lastDate = dt;
                isInRange = dt >= cutoff;
                if (isInRange) includedDates.Add(dt.Date);
            }

            if (isInRange || (lastDate >= cutoff))
            {
                filteredLines.Add(line);
            }
        }

        return (string.Join(Environment.NewLine, filteredLines), includedDates.OrderBy(d => d).ToList());
    }

    public Dictionary<string, double> CalculateBasicMetrics(string text, List<DateTime> dates)
    {
        var lineCount = text.Split('\n').Length;
        var wordCount = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var charCount = text.Length;
        var distinctDateCount = dates.Distinct().Count();

        return new Dictionary<string, double>
        {
            { "Mesaj Satırı", lineCount },
            { "Kelime Sayısı", wordCount },
            { "Karakter Sayısı", charCount },
            { "Tarih Sayısı", distinctDateCount }
        };
    }

    public (Dictionary<string, double> BasicMetrics, string MostUsedWord, string MostUsedEmoji, Dictionary<string, int> MessageCountPerPerson) GetAdvancedMetrics(string conversationText, List<DateTime> dates)
    {
        var lines = conversationText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var includedDates = dates.Distinct().Count();

        var wordFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var emojiFreq = new Dictionary<string, int>();
        var messageCountByPerson = new Dictionary<string, int>();

        int lineCount = 0;
        int wordCount = 0;
        int charCount = 0;

        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1) continue;

            // İsim ve mesaj ayırımı
            var namePart = line.Substring(0, colonIndex).Trim();
            var message = line.Substring(colonIndex + 1).Trim();

            // Konuşmacı adı (tırnak ve boşluk temizle)
            var nameSplit = namePart.Split('-');
            var rawName = nameSplit.Length >= 2 ? nameSplit[1].Trim() : namePart;
            var speakerName = rawName.Trim('"', ' ', ':');

            if (string.IsNullOrWhiteSpace(message))
                continue;

            if (!messageCountByPerson.ContainsKey(speakerName))
                messageCountByPerson[speakerName] = 0;
            messageCountByPerson[speakerName]++;

            lineCount++;
            charCount += message.Length;

            // 1️⃣ Kelime Analizi
            var cleanText = new string(message.Where(c => !char.IsPunctuation(c)).ToArray());
            var words = cleanText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                if (word.All(char.IsLetter) && !word.Contains(speakerName)) // konuşmacı adı hariç
                {
                    if (!wordFreq.ContainsKey(word))
                        wordFreq[word] = 0;
                    wordFreq[word]++;
                    wordCount++;
                }
            }

            // 2️⃣ Emoji Analizi
            var enumerator = StringInfo.GetTextElementEnumerator(message);
            while (enumerator.MoveNext())
            {
                string emoji = enumerator.GetTextElement();
                if (emoji.Length > 0 && char.GetUnicodeCategory(emoji[0]) == UnicodeCategory.OtherSymbol)
                {
                    if (!emojiFreq.ContainsKey(emoji))
                        emojiFreq[emoji] = 0;
                    emojiFreq[emoji]++;
                }
            }
        }

        string mostUsedWord = wordFreq.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "Yok";
        string mostUsedEmoji = emojiFreq.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "Yok";

        var basic = new Dictionary<string, double>
    {
        { "Mesaj Satırı", lineCount },
        { "Kelime Sayısı", wordCount },
        { "Karakter Sayısı", charCount },
        { "Tarih Sayısı", includedDates }
    };

        return (basic, mostUsedWord, mostUsedEmoji, messageCountByPerson);
    }
}
