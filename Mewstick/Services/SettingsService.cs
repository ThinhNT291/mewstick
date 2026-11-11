using System.IO;
using System.Text.Json;

namespace Mewstick.Services;

/// <summary>Cài đặt người dùng lưu ở %AppData%\Mewstick\settings.json (tách riêng khỏi tasks.json).</summary>
public class AppSettings
{
    // Vị trí/kích thước cửa sổ chính lần cuối (null = chưa lưu -> mở giữa màn hình)
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }

    /// <summary>Đang ghim luôn trên cùng.</summary>
    public bool IsPinned { get; set; }

    /// <summary>Giao diện tối.</summary>
    public bool IsDarkTheme { get; set; }
}

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public AppSettings Current { get; private set; } = new();

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mewstick");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
                Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath)) ?? new AppSettings();
        }
        catch
        {
            // File hỏng -> dùng cài đặt mặc định thay vì crash
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(Current, JsonOptions));
        }
        catch
        {
            // Không ghi được (ổ đĩa đầy/bị khoá...) thì bỏ qua, không làm phiền người dùng
        }
    }
}
