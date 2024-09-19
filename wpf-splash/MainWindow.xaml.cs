
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace wpf_splash
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Minimized, and without a TaskBar icon, no
            // chance of un-minimizing it until we say so!
            WindowState = WindowState.Minimized;
            Width = 0;
            Height = 0;
            ShowInTaskbar = false;
            InitializeComponent();
            Loaded += async(sender, e) =>
            {
                // NOW the native hWnd exists AND it's the first hWnd to come
                // into existence, making this class (MainWindow) the "official"
                // application main window. This means that when it closes, the
                // app will exit as long as we make our other window handles "behave".

                var splash = new Splash();
                await splash.Show();

                WindowState = WindowState.Normal;
                Width = 500;
                Height = 300;
                ShowInTaskbar = true;
                localCenterToScreen();
                splash.Close();

                #region L o c a l M e t h o d s
                void localCenterToScreen()
                {
                    double screenWidth = SystemParameters.PrimaryScreenWidth;
                    double screenHeight = SystemParameters.PrimaryScreenHeight;
                    double left = (screenWidth - Width) / 2;
                    double top = (screenHeight - Height) / 2;
                    Left = left;
                    Top = top;
                }
                #endregion L o c a l M e t h o d s
            };
        }
        new MainWindowViewModel DataContext => (MainWindowViewModel)base.DataContext;

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            DataContext.Status.ReadyVisibility = Visibility.Hidden;

            var progress = new Progress<int>(percent =>
            {
                progressBar.Value = percent;
            });
            await Task.Run(() =>
            {
                // Simulate a 2 S background processing task.
                for (int i = 0; i <= 20; i++)
                {
                    ((IProgress<int>)progress).Report(i * 5);
                    Thread.Sleep(100);
                }
            });

            DataContext.Status.ReadyVisibility = Visibility.Visible;
        }
    }
    public class MainWindowViewModel
    {
        public Status Status { get; set; } = new Status();
    }
    public class Status : INotifyPropertyChanged
    {
        public Visibility ReadyVisibility
        {
            get => _readyVisibility;
            set
            {
                if (!Equals(_readyVisibility, value))
                {
                    _readyVisibility = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BusyVisibility));
                    OnPropertyChanged(nameof(IsButtonEnabled));
                }
            }
        }
        Visibility _readyVisibility = Visibility.Visible;
        public Visibility BusyVisibility =>
            Equals(ReadyVisibility, Visibility.Visible) ? Visibility.Hidden : Visibility.Visible;

        public bool IsButtonEnabled => Equals(ReadyVisibility, Visibility.Visible);

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}