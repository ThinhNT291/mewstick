using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mewstick.Models;
using Mewstick.Services;

namespace Mewstick.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const double ChartMaxHeight = 120;

    private readonly DataService _dataService;

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> DeletedTasks { get; } = new();
    public ObservableCollection<DayStat> Stats { get; } = new();

    public string[] RangeOptions { get; } = { "Ngày", "Tuần", "Tháng" };

    [ObservableProperty]
    private string newTaskText = string.Empty;

    [ObservableProperty]
    private string selectedRange = "Ngày";

    [ObservableProperty]
    private bool isMatrixPopupOpen;

    [ObservableProperty]
    private DateTime? matrixPopupDate;

    [ObservableProperty]
    private string matrixPopupTime = string.Empty;

    [ObservableProperty]
    private bool isHelpOpen;

    public MainViewModel(DataService dataService)
    {
        _dataService = dataService;

        foreach (var task in _dataService.Load().OrderBy(t => t.Order))
        {
            if (task.IsDeleted) DeletedTasks.Add(task);
            else Tasks.Add(task);
        }

        ResortTasks();
        RebuildStats();
    }

    partial void OnSelectedRangeChanged(string value) => RebuildStats();

    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskText))
            return;

        var parsed = TaskInputParser.Parse(NewTaskText);

        if (!IsMatrixPopupOpen && parsed.Quadrant == EisenhowerQuadrant.None)
        {
            IsMatrixPopupOpen = true;
            return;
        }

        IsMatrixPopupOpen = false;
        CreateTask(parsed.CleanText, parsed.Quadrant, parsed.DueAt ?? BuildPopupDueAt());
    }

    [RelayCommand]
    private void PickQuadrant(string code)
    {
        var quadrant = code switch
        {
            "gq" => EisenhowerQuadrant.UrgentImportant,
            "gk" => EisenhowerQuadrant.UrgentNotImportant,
            "kq" => EisenhowerQuadrant.NotUrgentImportant,
            "kk" => EisenhowerQuadrant.NotUrgentNotImportant,
            _ => EisenhowerQuadrant.None
        };

        var parsed = TaskInputParser.Parse(NewTaskText);
        IsMatrixPopupOpen = false;
        CreateTask(parsed.CleanText, quadrant, parsed.DueAt ?? BuildPopupDueAt());
    }

    [RelayCommand]
    private void CloseMatrixPopup()
    {
        IsMatrixPopupOpen = false;
        IsHelpOpen = false;
    }

    [RelayCommand]
    private void ToggleHelp()
    {
        IsHelpOpen = !IsHelpOpen;
    }

    private DateTime? BuildPopupDueAt()
    {
        if (MatrixPopupDate == null && string.IsNullOrWhiteSpace(MatrixPopupTime))
            return null;

        var date = (MatrixPopupDate ?? DateTime.Today).Date;
        var time = new TimeSpan(6, 0, 0);

        if (!string.IsNullOrWhiteSpace(MatrixPopupTime) && TimeSpan.TryParse(MatrixPopupTime, out var parsedTime))
            time = parsedTime;

        return date.Add(time);
    }

    private void CreateTask(string content, EisenhowerQuadrant quadrant, DateTime? dueAt)
    {
        NewTaskText = string.Empty;
        MatrixPopupDate = null;
        MatrixPopupTime = string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            return;

        Tasks.Add(new TaskItem
        {
            Content = content,
            Quadrant = quadrant,
            DueAt = dueAt,
            Order = Tasks.Count
        });

        ResortTasks();
        Persist();
    }

    /// <summary>Gọi từ sự kiện Checked/Unchecked của checkbox (IsDone đã được binding cập nhật trước đó).</summary>
    public void OnTaskDoneToggled(TaskItem task)
    {
        task.CompletedAt = task.IsDone ? DateTime.Now : null;
        ResortTasks();
        Persist();
    }

    /// <summary>Xóa mềm: chuyển task sang danh sách Deleted (xem lại ở tab Thống kê).</summary>
    [RelayCommand]
    private void DeleteTask(TaskItem task)
    {
        task.IsDeleted = true;
        task.DeletedAt = DateTime.Now;
        Tasks.Remove(task);
        DeletedTasks.Insert(0, task);
        Persist();
    }

    /// <summary>Xóa vĩnh viễn khỏi danh sách Deleted (dùng ở tab Thống kê).</summary>
    [RelayCommand]
    private void PermanentlyDeleteTask(TaskItem task)
    {
        DeletedTasks.Remove(task);
        Persist();
    }

    [RelayCommand]
    private void BeginEdit(TaskItem task)
    {
        task.EditText = ReconstructRawText(task);
        task.IsEditing = true;
    }

    [RelayCommand]
    private void CommitEdit(TaskItem task)
    {
        if (!task.IsEditing) return;

        var parsed = TaskInputParser.Parse(task.EditText);
        if (!string.IsNullOrWhiteSpace(parsed.CleanText))
        {
            task.Content = parsed.CleanText;
            task.Quadrant = parsed.Quadrant;
            task.DueAt = parsed.DueAt;
        }

        task.IsEditing = false;
        ResortTasks();
        Persist();
    }

    [RelayCommand]
    private void CancelEdit(TaskItem task)
    {
        task.IsEditing = false;
    }

    private TaskItem? _reminderPopupTask;

    [RelayCommand]
    private void OpenReminderPopup(TaskItem task)
    {
        _reminderPopupTask = task;
        task.ReminderPopupDate = task.DueAt?.Date;
        task.ReminderPopupTime = task.DueAt?.ToString("HH:mm") ?? string.Empty;
        task.IsReminderPopupOpen = true;
    }

    [RelayCommand]
    private void SetReminderQuadrant(string code)
    {
        if (_reminderPopupTask == null) return;

        _reminderPopupTask.Quadrant = code switch
        {
            "gq" => EisenhowerQuadrant.UrgentImportant,
            "gk" => EisenhowerQuadrant.UrgentNotImportant,
            "kq" => EisenhowerQuadrant.NotUrgentImportant,
            "kk" => EisenhowerQuadrant.NotUrgentNotImportant,
            _ => EisenhowerQuadrant.None
        };
    }

    [RelayCommand]
    private void SaveReminderPopup(TaskItem task)
    {
        if (task.ReminderPopupDate.HasValue || !string.IsNullOrWhiteSpace(task.ReminderPopupTime))
        {
            var date = (task.ReminderPopupDate ?? DateTime.Today).Date;
            var time = new TimeSpan(6, 0, 0);
            if (!string.IsNullOrWhiteSpace(task.ReminderPopupTime) && TimeSpan.TryParse(task.ReminderPopupTime, out var t))
                time = t;
            task.DueAt = date.Add(time);
        }
        else
        {
            task.DueAt = null;
        }

        task.IsReminderPopupOpen = false;
        ResortTasks();
        Persist();
    }

    [RelayCommand]
    private void ClearReminder(TaskItem task)
    {
        task.DueAt = null;
        task.ReminderPopupDate = null;
        task.ReminderPopupTime = string.Empty;
        task.IsReminderPopupOpen = false;
        ResortTasks();
        Persist();
    }

    [RelayCommand]
    private void CloseReminderPopup(TaskItem task)
    {
        task.IsReminderPopupOpen = false;
    }


    private TaskItem? _priorityPopupTask;

    [RelayCommand]
    private void OpenPriorityPopup(TaskItem task)
    {
        _priorityPopupTask = task;
        task.IsPriorityPopupOpen = true;
    }

    [RelayCommand]
    private void SetPriorityQuadrant(string code)
    {
        if (_priorityPopupTask == null) return;

        _priorityPopupTask.Quadrant = code switch
        {
            "gq" => EisenhowerQuadrant.UrgentImportant,
            "gk" => EisenhowerQuadrant.UrgentNotImportant,
            "kq" => EisenhowerQuadrant.NotUrgentImportant,
            "kk" => EisenhowerQuadrant.NotUrgentNotImportant,
            _ => EisenhowerQuadrant.None
        };

        _priorityPopupTask.IsPriorityPopupOpen = false;
        ResortTasks();
        Persist();
    }

    [RelayCommand]
    private void ClosePriorityPopup(TaskItem task)
    {
        task.IsPriorityPopupOpen = false;
    }
    public void AddQuickTask(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        var parsed = TaskInputParser.Parse(content);
        if (string.IsNullOrWhiteSpace(parsed.CleanText))
            return;

        Tasks.Add(new TaskItem
        {
            Content = parsed.CleanText,
            Quadrant = parsed.Quadrant,
            DueAt = parsed.DueAt,
            Order = Tasks.Count
        });

        ResortTasks();
        Persist();
    }

    /// <summary>
    /// Sắp xếp lại toàn bộ danh sách hiển thị:
    ///  1) các group theo thứ tự người dùng kéo (Order của dòng tiêu đề), mỗi group là tiêu đề + các task của nó;
    ///  2) các task đứng ngoài group ở phía dưới.
    /// Trong mỗi vùng: chưa xong (theo mức ưu tiên) ở trên; đã xong dồn xuống dưới,
    /// tick gần nhất nằm trên, tick lâu nhất nằm dưới cùng. Task đã xong vẫn nằm trong group của nó.
    /// </summary>
    private void ResortTasks()
    {
        var sorted = BuildDisplayOrder();

        for (int i = 0; i < sorted.Count; i++)
        {
            int currentIndex = Tasks.IndexOf(sorted[i]);
            if (currentIndex != i)
                Tasks.Move(currentIndex, i);
            sorted[i].Order = i;
        }
    }

    private List<TaskItem> BuildDisplayOrder()
    {
        var headers = Tasks.Where(t => t.IsGroupHeader).OrderBy(t => t.Order).ToList();
        var headerIds = headers.Select(h => h.Id).ToHashSet();

        // Task trỏ tới group không còn tồn tại -> trả về ngoài group.
        foreach (var orphan in Tasks.Where(t => !t.IsGroupHeader && t.GroupId.HasValue && !headerIds.Contains(t.GroupId.Value)))
            orphan.GroupId = null;

        var result = new List<TaskItem>(Tasks.Count);
        foreach (var header in headers)
        {
            result.Add(header);
            result.AddRange(SortTaskGroup(Tasks.Where(t => !t.IsGroupHeader && t.GroupId == header.Id)));
        }

        result.AddRange(SortTaskGroup(Tasks.Where(t => !t.IsGroupHeader && !t.GroupId.HasValue)));
        return result;
    }

    private static IEnumerable<TaskItem> SortTaskGroup(IEnumerable<TaskItem> items) => items
        .OrderBy(t => t.IsDone)
        .ThenBy(t => t.IsDone ? 0 : RankOf(t))
        .ThenByDescending(t => t.IsDone ? (t.CompletedAt ?? DateTime.MinValue) : DateTime.MinValue)
        .ToList();

    // ------------------------- Group -------------------------

    /// <summary>Tạo group mới (hiện ở đầu danh sách), đưa task được chọn vào và cho sửa tên ngay.</summary>
    [RelayCommand]
    private void CreateGroup(TaskItem task)
    {
        if (task.IsGroupHeader) return;

        var header = new TaskItem
        {
            Content = "New group",
            IsGroupHeader = true,
            Order = -1
        };

        Tasks.Add(header);
        task.GroupId = header.Id;

        ResortTasks();
        Persist();

        header.EditText = header.Content;
        header.IsEditing = true;
    }

    [RelayCommand]
    private void RemoveFromGroup(TaskItem task)
    {
        if (task.IsGroupHeader || !task.GroupId.HasValue) return;

        task.GroupId = null;
        ResortTasks();
        Persist();
    }

    /// <summary>Gỡ group: tiêu đề bị xoá, các task bên trong trở thành task thường.</summary>
    [RelayCommand]
    private void Ungroup(TaskItem header)
    {
        if (!header.IsGroupHeader) return;

        foreach (var member in Tasks.Where(t => t.GroupId == header.Id).ToList())
            member.GroupId = null;

        Tasks.Remove(header);
        ResortTasks();
        Persist();
    }

    [RelayCommand]
    private void RenameGroup(TaskItem header)
    {
        if (!header.IsGroupHeader) return;
        BeginEdit(header);
    }

    /// <summary>Thả task lên một dòng: vào group của dòng đó (tiêu đề hoặc thành viên); thả lên task ngoài group thì task rời group.</summary>
    public void DropTaskOn(TaskItem dragged, TaskItem target)
    {
        if (dragged.IsGroupHeader || ReferenceEquals(dragged, target)) return;

        Guid? newGroup = target.IsGroupHeader ? target.Id : target.GroupId;
        if (dragged.GroupId == newGroup) return;

        dragged.GroupId = newGroup;
        ResortTasks();
        Persist();
    }

    /// <summary>Group đang chứa/được trỏ tới bởi dòng này (null nếu dòng không thuộc group nào).</summary>
    public TaskItem? HeaderOf(TaskItem row) =>
        row.IsGroupHeader ? row
        : row.GroupId.HasValue ? Tasks.FirstOrDefault(t => t.IsGroupHeader && t.Id == row.GroupId.Value)
        : null;

    /// <summary>
    /// Gọi liên tục khi kéo dòng tiêu đề <paramref name="group"/> qua dòng tiêu đề <paramref name="target"/>:
    /// hoán đổi vị trí khi con trỏ đã vượt qua nửa dòng của group kia, group kia sẽ trượt ra nhường chỗ.
    /// </summary>
    public void DragGroupOver(TaskItem group, TaskItem target, bool pointerInLowerHalf)
    {
        if (!group.IsGroupHeader || !target.IsGroupHeader || ReferenceEquals(group, target)) return;

        var headers = Tasks.Where(t => t.IsGroupHeader).OrderBy(t => t.Order).ToList();
        int from = headers.IndexOf(group);
        int to = headers.IndexOf(target);
        if (from < 0 || to < 0) return;

        // Đi xuống: chỉ hoán đổi khi qua nửa dưới; đi lên: chỉ khi qua nửa trên (tránh nhấp nháy qua lại).
        bool movesDown = from < to;
        if (movesDown != pointerInLowerHalf) return;

        headers.RemoveAt(from);
        headers.Insert(to, group);
        for (int i = 0; i < headers.Count; i++)
            headers[i].Order = i;

        ResortTasks();
    }

    /// <summary>Bôi sáng đúng 1 group đang được trỏ tới khi kéo task (null = tắt hết).</summary>
    public void HighlightDropTarget(TaskItem? header)
    {
        foreach (var t in Tasks)
        {
            bool on = header != null && ReferenceEquals(t, header);
            if (t.IsDropTarget != on) t.IsDropTarget = on;
        }
    }

    /// <summary>Gọi khi kết thúc thao tác kéo thả để lưu vị trí mới.</summary>
    public void CompleteDrag()
    {
        HighlightDropTarget(null);
        ResortTasks();
        Persist();
    }

    private static int RankOf(TaskItem task) =>
        task.Quadrant == EisenhowerQuadrant.None ? int.MaxValue : (int)task.Quadrant;

    private static string ReconstructRawText(TaskItem task)
    {
        string text = task.Content;

        text += task.Quadrant switch
        {
            EisenhowerQuadrant.UrgentImportant => " -gq",
            EisenhowerQuadrant.UrgentNotImportant => " -gk",
            EisenhowerQuadrant.NotUrgentImportant => " -kq",
            EisenhowerQuadrant.NotUrgentNotImportant => " -kk",
            _ => string.Empty
        };

        if (task.DueAt.HasValue)
            text += $" -{task.DueAt.Value:HH:mm} -{task.DueAt.Value:dd/MM}";

        return text;
    }

    /// <summary>Dựng lại dữ liệu biểu đồ theo Ngày/Tuần/Tháng đang chọn.</summary>
    private void RebuildStats()
    {
        Stats.Clear();

        var buckets = BuildBuckets();
        var raw = buckets
            .Select(b =>
            {
                var inRange = Tasks.Where(t => !t.IsGroupHeader && t.CreatedAt >= b.start && t.CreatedAt < b.end).ToList();
                return (b.label, unfinished: inRange.Count(t => !t.IsDone), finished: inRange.Count(t => t.IsDone));
            })
            .ToList();

        int max = raw.Count == 0 ? 1 : Math.Max(1, raw.Max(r => r.unfinished + r.finished));

        foreach (var r in raw)
        {
            Stats.Add(new DayStat
            {
                Label = r.label,
                Unfinished = r.unfinished,
                Finished = r.finished,
                UnfinishedHeight = ChartMaxHeight * r.unfinished / max,
                FinishedHeight = ChartMaxHeight * r.finished / max
            });
        }
    }

    private List<(string label, DateTime start, DateTime end)> BuildBuckets()
    {
        var list = new List<(string, DateTime, DateTime)>();
        var today = DateTime.Today;

        switch (SelectedRange)
        {
            case "Tuần":
                for (int i = 7; i >= 0; i--)
                {
                    int offset = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
                    var thisWeekStart = today.AddDays(-offset);
                    var weekStart = thisWeekStart.AddDays(-7 * i);
                    var weekEnd = weekStart.AddDays(7);
                    list.Add(($"{weekStart:dd/MM}", weekStart, weekEnd));
                }
                break;

            case "Tháng":
                for (int i = 5; i >= 0; i--)
                {
                    var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);
                    list.Add((monthStart.ToString("MM/yyyy"), monthStart, monthEnd));
                }
                break;

            default:
                for (int i = 6; i >= 0; i--)
                {
                    var day = today.AddDays(-i);
                    list.Add((day.ToString("dd/MM"), day, day.AddDays(1)));
                }
                break;
        }

        return list;
    }

    public void Persist()
    {
        _dataService.Save(Tasks.Concat(DeletedTasks));
        RebuildStats();
    }
}