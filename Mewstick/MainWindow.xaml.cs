using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Mewstick.Models;
using Mewstick.ViewModels;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using ContextMenu = System.Windows.Controls.ContextMenu;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using TextBoxBase = System.Windows.Controls.Primitives.TextBoxBase;

namespace Mewstick;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Closing += MainWindow_Closing;
    }

    /// <summary>
    /// Đóng nút X chỉ ẩn cửa sổ (app vẫn chạy nền qua tray icon).
    /// Thoát thật sự chỉ qua menu tray "Thoát".
    /// </summary>
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            _viewModel.Persist();
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }
    /// <summary>Khi ô sửa hiện ra, tự đưa con trỏ vào và bôi đen sẵn nội dung.</summary>
    private void EditTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox tb && tb.IsVisible)
        {
            tb.Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.Focus();
                tb.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    /// <summary>Click ra ngoài ô sửa cũng tự lưu lại, không bắt buộc phải bấm Enter.</summary>
    private void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Mewstick.Models.TaskItem task)
            _viewModel.CommitEditCommand.Execute(task);
    }
    private void TaskCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Mewstick.Models.TaskItem task)
            _viewModel.OnTaskDoneToggled(task);
    }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void PinToggle_Click(object sender, RoutedEventArgs e)
    {
        Topmost = PinToggle.IsChecked == true;
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
    public void ShowHelpTab()
    {
        MainTabControl.SelectedIndex = 0;
    }

    // ===================== Group + kéo thả =====================

    private const string DragFormat = "Mewstick.TaskItem";
    private const double AutoScrollEdge = 28;
    private const double AutoScrollStep = 14;

    private Point _dragStartPoint;
    private TaskItem? _dragCandidate;
    private TaskItem? _draggedTask;

    private static TaskItem? TaskOf(object sender) =>
        (sender as FrameworkElement)?.DataContext as TaskItem;

    /// <summary>Task của dòng đang mở menu chuột phải (PlacementTarget của ContextMenu).</summary>
    private static TaskItem? MenuTask(object sender)
    {
        var menu = sender as ContextMenu
            ?? ItemsControl.ItemsControlFromItemContainer((DependencyObject)sender) as ContextMenu;
        return (menu?.PlacementTarget as FrameworkElement)?.DataContext as TaskItem;
    }

    /// <summary>Hiện/ẩn từng mục menu tuỳ dòng đang bấm là task thường, task trong group hay tiêu đề group.</summary>
    private void TaskContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || MenuTask(menu) is not { } task)
            return;

        foreach (MenuItem item in menu.Items.OfType<MenuItem>())
        {
            bool visible = item.Tag as string switch
            {
                "create" => !task.IsGroupHeader,
                "remove" => !task.IsGroupHeader && task.IsGrouped,
                "rename" or "ungroup" => task.IsGroupHeader,
                _ => true
            };
            item.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void CreateGroup_Click(object sender, RoutedEventArgs e)
    {
        if (MenuTask(sender) is { } task) _viewModel.CreateGroupCommand.Execute(task);
    }

    private void RemoveFromGroup_Click(object sender, RoutedEventArgs e)
    {
        if (MenuTask(sender) is { } task) _viewModel.RemoveFromGroupCommand.Execute(task);
    }

    private void RenameGroup_Click(object sender, RoutedEventArgs e)
    {
        if (MenuTask(sender) is { } task) _viewModel.RenameGroupCommand.Execute(task);
    }

    private void Ungroup_Click(object sender, RoutedEventArgs e)
    {
        if (MenuTask(sender) is { } task) _viewModel.UngroupCommand.Execute(task);
    }

    /// <summary>Ghi nhận điểm bấm chuột; không bắt đầu kéo nếu bấm trên nút/checkbox/ô nhập.</summary>
    private void TaskRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragCandidate = null;

        if (TaskOf(sender) is not { IsEditing: false } task)
            return;

        for (var d = e.OriginalSource as DependencyObject; d != null && !ReferenceEquals(d, sender); d = GetParent(d))
        {
            if (d is ButtonBase or TextBoxBase or Popup)
                return;
        }

        _dragCandidate = task;
        _dragStartPoint = e.GetPosition(this);
    }

    private static DependencyObject? GetParent(DependencyObject d) =>
        d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);

    private void TaskRow_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragCandidate == null || _draggedTask != null)
            return;

        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _draggedTask = _dragCandidate;
        _dragCandidate = null;

        try
        {
            DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(DragFormat, _draggedTask), DragDropEffects.Move);
        }
        finally
        {
            _draggedTask = null;
            _viewModel.CompleteDrag();
        }
    }

    private void TaskRow_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (_draggedTask == null || TaskOf(sender) is not { } target)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        e.Effects = DragDropEffects.Move;
        AutoScrollWhileDragging(e);

        if (_draggedTask.IsGroupHeader)
        {
            // Kéo cả group: hoán đổi vị trí với group bị trỏ tới, group đó trượt ra nhường chỗ.
            if (target.IsGroupHeader && sender is FrameworkElement row && row.ActualHeight > 0)
            {
                bool lowerHalf = e.GetPosition(row).Y > row.ActualHeight / 2;
                _viewModel.DragGroupOver(_draggedTask, target, lowerHalf);
            }
        }
        else
        {
            // Kéo task: bôi sáng group sẽ nhận task (nếu có).
            var header = _viewModel.HeaderOf(target);
            _viewModel.HighlightDropTarget(header);
        }
    }

    private void TaskRow_DragLeave(object sender, DragEventArgs e)
    {
        if (_draggedTask is { IsGroupHeader: false })
            _viewModel.HighlightDropTarget(null);
    }

    private void TaskRow_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        _viewModel.HighlightDropTarget(null);

        if (_draggedTask is { IsGroupHeader: false } task && TaskOf(sender) is { } target)
            _viewModel.DropTaskOn(task, target);
    }

    /// <summary>Kéo sát mép trên/dưới vùng danh sách thì tự cuộn.</summary>
    private void AutoScrollWhileDragging(DragEventArgs e)
    {
        var y = e.GetPosition(TaskScrollViewer).Y;

        if (y < AutoScrollEdge)
            TaskScrollViewer.ScrollToVerticalOffset(TaskScrollViewer.VerticalOffset - AutoScrollStep);
        else if (y > TaskScrollViewer.ActualHeight - AutoScrollEdge)
            TaskScrollViewer.ScrollToVerticalOffset(TaskScrollViewer.VerticalOffset + AutoScrollStep);
    }
}
