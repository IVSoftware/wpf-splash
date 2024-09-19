
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
            WindowState = WindowState.Minimized;
            Width = 0;
            Height = 0;
            ShowInTaskbar = false;
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                //Visibility = Visibility.Hidden;
                _ = InitializeAsync();
            };
        }

        private async Task InitializeAsync()
        {
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
                double left = (screenWidth - this.Width) / 2;
                double top = (screenHeight - this.Height) / 2;
                this.Left = left;
                this.Top = top;
            }
            #endregion L o c a l M e t h o d s
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {

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