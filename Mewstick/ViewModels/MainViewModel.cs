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

    /// <summary>Chưa xong (theo mức ưu tiên) luôn ở trên; đã xong dồn xuống dưới,
    /// trong nhóm đã xong thì tick gần nhất nằm trên, tick lâu nhất nằm dưới cùng.</summary>
    private void ResortTasks()
    {
        var sorted = Tasks
            .OrderBy(t => t.IsDone)
            .ThenBy(t => t.IsDone ? 0 : RankOf(t))
            .ThenByDescending(t => t.IsDone ? (t.CompletedAt ?? DateTime.MinValue) : DateTime.MinValue)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            int currentIndex = Tasks.IndexOf(sorted[i]);
            if (currentIndex != i)
                Tasks.Move(currentIndex, i);
        }
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
                var inRange = Tasks.Where(t => t.CreatedAt >= b.start && t.CreatedAt < b.end).ToList();
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