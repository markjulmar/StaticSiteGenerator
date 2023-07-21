using System;
using System.Windows;

namespace SGLMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        private void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.ToString());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            UserSettings.Default.Save();
        }
    }
}
