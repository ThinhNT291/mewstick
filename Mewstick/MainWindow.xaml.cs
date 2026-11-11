using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Mewstick.ViewModels;

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
}
