using System.Text.RegularExpressions;
using Mewstick.Models;

namespace Mewstick.Services;

public class ParsedTaskInput
{
    public string CleanText { get; init; } = string.Empty;
    public EisenhowerQuadrant Quadrant { get; init; } = EisenhowerQuadrant.None;
    public DateTime? DueAt { get; init; }
}

/// <summary>
/// Phân tích gõ tắt trong ô nhập task:
///   -gq / -gk / -kq / -kk  -> mức độ ưu tiên (ma trận Eisenhower)
///   -hh:mm                 -> đặt giờ (ngày mặc định = hôm nay)
///   -dd/mm                 -> đặt ngày (giờ mặc định = 06:00)
/// Các mã này bị loại khỏi nội dung hiển thị sau khi phân tích.
/// </summary>
public static class TaskInputParser
{
    private static readonly Regex PriorityRegex =
        new(@"(?:^|\s)-(gq|gk|kq|kk)\b", RegexOptions.IgnoreCase);

    private static readonly Regex TimeRegex =
        new(@"(?:^|\s)-(\d{1,2}):(\d{2})\b");

    private static readonly Regex DateRegex =
        new(@"(?:^|\s)-(\d{1,2})/(\d{1,2})\b");

    public static ParsedTaskInput Parse(string rawText)
    {
        string text = rawText;
        var quadrant = EisenhowerQuadrant.None;
        TimeSpan? time = null;
        (int day, int month)? date = null;

        var priorityMatch = PriorityRegex.Match(text);
        if (priorityMatch.Success)
        {
            quadrant = priorityMatch.Groups[1].Value.ToLowerInvariant() switch
            {
                "gq" => EisenhowerQuadrant.UrgentImportant,
                "gk" => EisenhowerQuadrant.UrgentNotImportant,
                "kq" => EisenhowerQuadrant.NotUrgentImportant,
                "kk" => EisenhowerQuadrant.NotUrgentNotImportant,
                _ => EisenhowerQuadrant.None
            };
            text = text.Remove(priorityMatch.Index, priorityMatch.Length);
        }

        var timeMatch = TimeRegex.Match(text);
        if (timeMatch.Success &&
            int.TryParse(timeMatch.Groups[1].Value, out int hh) &&
            int.TryParse(timeMatch.Groups[2].Value, out int mm) &&
            hh is >= 0 and <= 23 && mm is >= 0 and <= 59)
        {
            time = new TimeSpan(hh, mm, 0);
            text = text.Remove(timeMatch.Index, timeMatch.Length);
        }

        var dateMatch = DateRegex.Match(text);
        if (dateMatch.Success &&
            int.TryParse(dateMatch.Groups[1].Value, out int dd) &&
            int.TryParse(dateMatch.Groups[2].Value, out int mo) &&
            mo is >= 1 and <= 12 && dd is >= 1 and <= 31)
        {
            date = (dd, mo);
            text = text.Remove(dateMatch.Index, dateMatch.Length);
        }

        DateTime? dueAt = null;
        if (time.HasValue || date.HasValue)
        {
            var today = DateTime.Today;
            var d = date.HasValue ? new DateTime(today.Year, date.Value.month, date.Value.day) : today;
            var t = time ?? new TimeSpan(6, 0, 0);
            dueAt = d.Add(t);
        }

        return new ParsedTaskInput
        {
            CleanText = Regex.Replace(text, @"\s{2,}", " ").Trim(),
            Quadrant = quadrant,
            DueAt = dueAt
        };
    }
}