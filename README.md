Since the underlying issue is the same for WPF, I'm going to my [answer](https://stackoverflow.com/a/75534137/5438626) for WinForms and make it work for WPF. And how about we make it work _first_, and then explain why it works _second_.

Everything you need to do is in the `Loaded` event handler here in the `MainWindow` constructor. 

```
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
        // Set the dimensions that you actually want here, and turn the TaskBar icon back on.
        Width = 500;
        Height = 300;
        ShowInTaskbar = true;
        localCenterToScreen();
        // Do this BEFORE closing the splash. It's a smoke-and-mirrors
        // trick that hides some of the ugly transient draws.
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
```

____

#### Splash Screen Simulates a 10-state Application Boot

###### Xaml

```
<Window x:Class="wpf_splash.Splash"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:wpf_splash"
        mc:Ignorable="d"
        Title="Splash"
        Height="300" 
        Width="460"
        WindowStyle="None"
        WindowStartupLocation="CenterScreen"
        ShowInTaskbar="False">
    <Grid>
        <Image Source="pack://application:,,,/Resources/Splash.png" Stretch="Fill" />
        <StackPanel VerticalAlignment="Bottom" Background="#AAFFFFFF" >
            <!-- State Progress Grid -->
            <Grid Margin="0,0,0,5">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="150"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Label Content="{Binding CurrentState}"  VerticalAlignment="Center" Grid.Column="0"/>
                <ProgressBar x:Name="StateProgressBar" Maximum="100" Height="20" Margin="5,0,0,0" Grid.Column="1" />
            </Grid>
            <!-- Overall Progress Grid -->
            <Grid Margin="0,5,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="150"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Label Content="Total Progress:" FontWeight="Bold" VerticalAlignment="Center" Grid.Column="0"/>
                <ProgressBar x:Name="OverallProgressBar" Maximum="100" Height="20" Margin="5,0,0,0" Grid.Column="1"/>
            </Grid>
        </StackPanel>
    </Grid>
</Window>
```
###### Code behind

```
public enum StartupState
{
    InitializingComponents,
    LoadingConfiguration,
    CheckingForUpdates,
    ContactingServer,
    AuthenticatingUser,
    LoadingResources,
    LoadingAvatars,
    EstablishingConnections,
    PreparingUserInterface,
    FinalizingSetup
}
public partial class Splash : Window, INotifyPropertyChanged
{
    public Splash()
    {
        InitializeComponent();
        DataContext = this;
    }
    public new async Task Show()
    {
        base.Show();
        StartupState[] states = Enum.GetValues<StartupState>();
        var stopwatch = new Stopwatch();
        TimeSpan randoTimeSpan = TimeSpan.MaxValue;
        Task taskSpinState;

        var progress = new Progress<double>(percent =>
        {
            StateProgressBar.Value = percent;
        });
        bool isCancelled = false;
        taskSpinState = Task.Run(() =>
        {
            while(!isCancelled)
            {
                ((IProgress<double>)progress).Report(100 * (stopwatch.ElapsedMilliseconds / (double)randoTimeSpan.TotalMilliseconds));
                Thread.Sleep(_rando.Next(100, 200));
            }
        });

        for (int i = 0; i < states.Length; i++)
        {
            CurrentState = states[i].ToString().CamelCaseToSpaces();
            stopwatch.Restart();
            randoTimeSpan = TimeSpan.FromSeconds(0.5 * (_rando.NextDouble() * 3d)); 
            await Task.Delay(randoTimeSpan);
            OverallProgressBar.Value = 0.5 + 100 * (i / (double)states.Length);
        }
        OverallProgressBar.Value = 100;
    }
    Random _rando = new Random(Seed:1);
    public string CurrentState
    {
        get => _currentState;
        set
        {
            if (!Equals(_currentState, value))
            {
                _currentState = value;
                OnPropertyChanged();
            }
        }
    }
    string _currentState = string.Empty;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;
}
static partial class Extensions
{
    public static string CamelCaseToSpaces(this string @string)
    {
        string pattern = "(?<![A-Z])([A-Z][a-z]|(?<=[a-z])[A-Z])";
        string replacement = " $1";
        return Regex.Replace(@string, pattern, replacement).Trim();
    }
}
```
