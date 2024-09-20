Your question:

> I created a waiting window that stays open to view the software loading. When the software finishes the calculations, it opens the main window [and should] close the waiting window.

___

For this scenario where a "splash screen" is shown before the main window, the underlying issue is the same for WPF as it is for WinForms, so I'm going to port an earlier [answer](https://stackoverflow.com/a/75534137/5438626) and make it work for WPF. If you like, you can [clone](https://github.com/IVSoftware/wpf-splash.git) this WPF solution and try it from my GitHub repo to see if this looks like the kind of behavior you want, and if so I'll start with the _how_, and then explain the _why_ - my rationale for going about it in this manner.

[![splash before main app window][1]][1]
___

If you have something similar to the `Splash` window class shown below, where an awaitable `public new async Task Show()` method can now be called in place of the normal `Show()` method, then everything you need to do in `MainWindow` is in the `Loaded` event handler here in the `MainWindow` constructor. 

```
public MainWindow()
{
    // Minimize the window
    WindowState = WindowState.Minimized;
    // Hide TaskBar icon so there's no temptation to un-minimizing it until we say so! 
    ShowInTaskbar = false;
    // This might be old habits, but I always feel better with a small visual footprint 
    // in case there's spurious flicker when the window creation occurs.
    Width = 0;
    Height = 0;
    WindowStyle = WindowStyle.None;


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
    InitializingComponents, LoadingConfiguration, CheckingForUpdates, ContactingServer, AuthenticatingUser, 
    LoadingResources, LoadingAvatars, EstablishingConnections, PreparingUserInterface, FinalizingSetup
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

#### Theory of Operation

There may indeed be other ways to go about it, but here's the rationale for this approach based on my understanding.

When a splash screen is created before the main window and is the first to create the native HWND, it may inadvertently become the main window in the application's context. Obviously this can turn things on their head, both at startup because you're now trying to close a main window pretender (the splash) while keeping open an unintended child window (main window), and also at shutdown now prone to hangs due to weirdly undisposed handles.

So, both my WPF solution and my WinForms solution work by making sure the win 32 native HWND (the thing that is abstracted in `Window` in WPF and `Control` in WinForms) gets created FIRST. Which is all well and good, but the _problem_ is that creation of the HWND is usually a consequence of making it visible, and we don't _want_ the main form visible. What we want, obviously, is for the splash to be visible, and to not see a bunch of flickering or artifacts, for example if we try and show-then-hide the main window in an effort to get around this.

The specific challenge of WPF is that (to my knowledge) you can't create the window handle out of band the same way you can in WinForms by calling `var forceCreate = Handle` in your `Control` or `Form`. Instead, I take the approach of creating the main window minimized, temporarily hiding the task bar icon that could be used to un-minimize it, and then waiting for the main window to fire its `Loading` event (which is a sure sign that the HWND now exists).

All this to say: my experience (more than I care to think about) is that this is a reliable way to do splash screens, but it comes with no additional warranty.


  [1]: https://i.sstatic.net/4mnPDtLj.png