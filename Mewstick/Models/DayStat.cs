namespace Mewstick.Models;

/// <summary>Một cột trong biểu đồ thống kê: số task chưa xong / đã xong trong 1 khoảng thời gian.</summary>
public class DayStat
{
    public string Label { get; set; } = string.Empty;
    public int Unfinished { get; set; }
    public int Finished { get; set; }
    public double UnfinishedHeight { get; set; }
    public double FinishedHeight { get; set; }
}