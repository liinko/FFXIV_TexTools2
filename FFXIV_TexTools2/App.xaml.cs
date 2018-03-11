using System;
using System.Windows;
using System.Windows.Forms;
using FFXIV_TexTools2.Helpers;
using Clipboard = System.Windows.Clipboard;
using System.Diagnostics;

namespace FFXIV_TexTools2
{
    /// <inheritdoc />
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // hook on error before app really starts
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string ver = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
            const string lineBreak = "\n======================================================\n";
            var errorText = "TexTools ran into an error.\n\n" +
                            "Please submit a bug report with the following information.\n " +
                            lineBreak +
                            e.ExceptionObject +
                            lineBreak + "\n" +
                            "Copy to clipboard?";
            if (FlexibleMessageBox.Show(errorText, "Crash Report " + ver,MessageBoxButtons.YesNo,MessageBoxIcon.Error) ==DialogResult.Yes)
            {
                Clipboard.SetText(e.ExceptionObject.ToString());
            }
        }
    }
}
