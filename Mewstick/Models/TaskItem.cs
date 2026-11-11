using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace Mewstick.Models;

public partial class TaskItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private bool isDone;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ObservableProperty]
    private DateTime? dueAt;

    [ObservableProperty]
    private EisenhowerQuadrant quadrant = EisenhowerQuadrant.None;

    /// <summary>Thời điểm tick hoàn thành gần nhất — dùng để sắp xếp nhóm "đã xong".</summary>
    [ObservableProperty]
    private DateTime? completedAt;

    /// <summary>Xóa mềm: task vẫn còn trong file dữ liệu, chỉ ẩn khỏi danh sách chính, xem lại ở tab Thống kê.</summary>
    [ObservableProperty]
    private bool isDeleted;

    [ObservableProperty]
    private DateTime? deletedAt;

    /// <summary>True: đây là dòng tiêu đề của một group (Content = tên group), không phải task thường.</summary>
    [ObservableProperty]
    private bool isGroupHeader;

    /// <summary>Id của dòng tiêu đề group mà task này thuộc về; null nếu task đứng ngoài group.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGrouped))]
    private Guid? groupId;

    [JsonIgnore]
    public bool IsGrouped => GroupId.HasValue;

    /// <summary>Đang có task được kéo qua dòng tiêu đề này (để tô sáng vùng thả).</summary>
    [ObservableProperty]
    [property: JsonIgnore]
    private bool isDropTarget;

    public string Color { get; set; } = "#FFF59D";

    public int Order { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    private bool isEditing;

    [JsonIgnore]
    [ObservableProperty]
    private string editText = string.Empty;

    // --- Trạng thái popup sửa nhắc hẹn, KHÔNG lưu xuống JSON ---
    [JsonIgnore]
    [ObservableProperty]
    private bool isReminderPopupOpen;

    [JsonIgnore]
    [ObservableProperty]
    private DateTime? reminderPopupDate;

    [JsonIgnore]
    [ObservableProperty]
    private string reminderPopupTime = string.Empty;

    [JsonIgnore]
    [ObservableProperty]
    private bool isPriorityPopupOpen;

    /// <summary>Đã hiện thông báo cho lần hẹn giờ hiện tại chưa — tránh báo lặp lại.</summary>
    [ObservableProperty]
    private bool isNotified;

    /// <summary>Mỗi lần đổi ngày giờ hẹn (kể cả xóa hẹn), coi như "chưa báo" lại từ đầu.</summary>
    partial void OnDueAtChanged(DateTime? value)
    {
        IsNotified = false;
    }
}