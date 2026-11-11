using System.Windows;
using System.Windows.Input;
using System.Media;
using CommunityToolkit.WinUI.Notifications;
using Mewstick.Services;
using Mewstick.ViewModels;
using Mewstick.Views;



namespace Mewstick;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstanceMutex;
    private const string MutexName = "Mewstick_SingleInstance_Mutex";

    public static bool IsExiting { get; private set; }

    private SettingsService _settings = null!;
    private DataService _dataService = null!;
    private MainViewModel _mainViewModel = null!;
    private MainWindow _mainWindow = null!;
    private QuickAddWindow _quickAddWindow = null!;
    private TrayIconService _trayIconService = null!;
    private HotkeyService _quickAddHotkeyService = null!;
    private HotkeyService _mainWindowHotkeyService = null!;
    private System.Windows.Threading.DispatcherTimer _reminderTimer = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            System.Windows.MessageBox.Show("Mewstick đang chạy rồi (xem khay hệ thống).", "Mewstick",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _settings = new SettingsService();
        ThemeService.Apply(_settings.Current.IsDarkTheme);

        _dataService = new DataService();
        _mainViewModel = new MainViewModel(_dataService);
        _mainWindow = new MainWindow(_mainViewModel, _settings);
        _quickAddWindow = new QuickAddWindow(_mainViewModel);

        _mainWindow.Show();

        // Windows tự khởi động (có tham số --startup) -> chạy ẩn; mở bằng tay -> hiện giao diện
        if (e.Args.Contains("--startup"))
            _mainWindow.Hide();

        // Bấm vào toast (hoặc nút trên toast) -> mở cửa sổ chính lên
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            Dispatcher.Invoke(() =>
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            });
        };

        _trayIconService = new TrayIconService();
        _trayIconService.ShowMainWindowRequested += () =>
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        };
        _trayIconService.QuickAddRequested += () => _quickAddWindow.ShowAtBottomRight();
        _trayIconService.HelpRequested += () =>
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.ShowHelpTab();
            _mainViewModel.IsHelpOpen = true;
            _mainWindow.Activate();
        };
        _trayIconService.ExitRequested += ExitApplication;

        // Ctrl+Alt+N: mở popup Thêm nhanh
        _quickAddHotkeyService = new HotkeyService(0x2333);
        var vkN = (uint)KeyInterop.VirtualKeyFromKey(Key.N);
        _quickAddHotkeyService.Register(_mainWindow, HotkeyModifiers.Control | HotkeyModifiers.Alt, vkN);
        _quickAddHotkeyService.HotkeyPressed += () => _quickAddWindow.ShowAtBottomRight();

        // Ctrl+Alt+M: bật/tắt cửa sổ chính
        _mainWindowHotkeyService = new HotkeyService(0x2334);
        var vkM = (uint)KeyInterop.VirtualKeyFromKey(Key.M);
        _mainWindowHotkeyService.Register(_mainWindow, HotkeyModifiers.Control | HotkeyModifiers.Alt, vkM);
        _mainWindowHotkeyService.HotkeyPressed += ToggleMainWindow;

        // Cứ 20 giây kiểm tra 1 lần xem có task nào tới giờ hẹn chưa
        _reminderTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(20)
        };
        _reminderTimer.Tick += (_, _) => CheckReminders();
        _reminderTimer.Start();
    }

    private void CheckReminders()
    {
        var now = DateTime.Now;
        bool changed = false;

        foreach (var task in _mainViewModel.Tasks)
        {
            if (task.DueAt.HasValue && !task.IsDone && !task.IsNotified && task.DueAt.Value <= now)
            {
                new ToastContentBuilder()
                    .AddArgument("taskId", task.Id.ToString())
                    .AddText("Mewstick nhắc việc")
                    .AddText(task.Content)
                    .SetToastScenario(ToastScenario.Reminder)
                    .Show();
                SystemSounds.Asterisk.Play();
                task.IsNotified = true;
                changed = true;
            }
        }

        if (changed)
            _mainViewModel.Persist();
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    private void ExitApplication()
    {
        _reminderTimer.Stop();
        IsExiting = true;
        _mainViewModel.Persist();
        _quickAddHotkeyService.Dispose();
        _mainWindowHotkeyService.Dispose();
        _trayIconService.Dispose();
        _mainWindow.Close();
        _singleInstanceMutex?.ReleaseMutex();
        Shutdown();
    }
}