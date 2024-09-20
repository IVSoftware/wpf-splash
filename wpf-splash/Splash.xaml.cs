using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace wpf_splash
{
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
                randoTimeSpan = TimeSpan.FromSeconds(0.5 + (_rando.NextDouble() * 2d)); 
                await Task.Delay(randoTimeSpan);
                OverallProgressBar.Value = 0.5 + 100 * (i / (double)states.Length);
            }
            OverallProgressBar.Value = 100;
            isCancelled = true;
            await taskSpinState;
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
}
