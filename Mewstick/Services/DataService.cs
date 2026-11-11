using System.IO;
using System.Text.Json;
using Mewstick.Models;

namespace Mewstick.Services;

/// <summary>
/// Lưu trữ danh sách task bằng JSON tại %AppData%\Mewstick\tasks.json.
/// MVP dùng JSON cho đơn giản; khi cần tìm kiếm nhanh / nhắc hẹn phức tạp
/// có thể thay bằng SQLite (Microsoft.Data.Sqlite) mà không đổi API bên ngoài.
/// </summary>
public class DataService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public DataService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mewstick");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "tasks.json");
    }

    public List<TaskItem> Load()
    {
        if (!File.Exists(_filePath))
            return new List<TaskItem>();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }
        catch
        {
            // File hỏng/không đọc được -> khởi tạo lại rỗng thay vì crash app
            return new List<TaskItem>();
        }
    }

    public void Save(IEnumerable<TaskItem> tasks)
    {
        var json = JsonSerializer.Serialize(tasks, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
