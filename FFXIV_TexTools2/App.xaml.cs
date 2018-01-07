using System;
using System.Windows;

namespace FFXIV_TexTools2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            base.OnStartup(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var line = "\n======================================================\n";
            var message = "TexTools ran into an error. \n\nPlease submit a bug report with the following information.\n " + line +
                e.ExceptionObject.ToString() + line + "\nCopy to clipboard?";
            if (MessageBox.Show(message, "Crash Report", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                Clipboard.SetText(e.ExceptionObject.ToString());
            }
        }
    }
}
