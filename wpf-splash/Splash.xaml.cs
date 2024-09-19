using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
    public partial class Splash : Window
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
                stopwatch.Restart();
                randoTimeSpan = TimeSpan.FromSeconds(0.5 * (_rando.NextDouble() * 3d)); 
                await Task.Delay(randoTimeSpan);
                OverallProgressBar.Value = 0.5 + 100 * (i / (double)states.Length);
            }
            OverallProgressBar.Value = 100;
        }
        Random _rando = new Random(Seed:1);
    }
}
