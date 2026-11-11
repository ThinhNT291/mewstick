using System.Windows;
using System.Windows.Input;
using Mewstick.ViewModels;

namespace Mewstick.Views;

public partial class QuickAddWindow : Window
{
    private readonly MainViewModel _mainViewModel;

    public QuickAddWindow(MainViewModel mainViewModel)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
        Loaded += (_, _) => QuickAddTextBox.Focus();
    }

    /// <summary>Hiện popup cố định ở góc dưới bên phải màn hình, giống toast thông báo.</summary>
    public void ShowAtBottomRight()
    {
        QuickAddTextBox.Text = string.Empty;

        // Show() trước để cửa sổ có "handle" thật, cần thiết để đọc đúng
        // tỉ lệ phóng đại màn hình (DPI scale) ngay bên dưới.
        Show();

        var workAreaPx = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;

        // Quy đổi pixel vật lý -> đơn vị WPF, để không bị lệch khi màn hình
        // đặt Scale 125%/150%/200%.
        double scaleX = 1.0, scaleY = 1.0;
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            scaleX = source.CompositionTarget.TransformFromDevice.M11;
            scaleY = source.CompositionTarget.TransformFromDevice.M22;
        }

        const double margin = 16;
        double workRight = workAreaPx.Right * scaleX;
        double workBottom = workAreaPx.Bottom * scaleY;

        Left = workRight - Width - margin;
        Top = workBottom - Height - margin;

        Topmost = false;
        Topmost = true;
        Activate();
        QuickAddTextBox.Focus();
        Keyboard.Focus(QuickAddTextBox);
    }

    private void QuickAddTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                _mainViewModel.AddQuickTask(QuickAddTextBox.Text);
                Hide();
                break;
            case Key.Escape:
                Hide();
                break;
        }
    }

    private void Window_Deactivated(object sender, EventArgs e) => Hide();
}
