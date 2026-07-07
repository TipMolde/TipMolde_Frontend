// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using TipMolde.Diagnostics;

namespace TipMolde.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            StartupCrashLogger.RegisterGlobalHandlers();
            UnhandledException += OnUnhandledException;

            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                StartupCrashLogger.LogException("TipMolde.WinUI.App constructor", ex);
                throw;
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
#if DEBUG
                DebugSettings.IsBindingTracingEnabled = true;
#endif
                base.OnLaunched(args);
            }
            catch (Exception ex)
            {
                StartupCrashLogger.LogException("TipMolde.WinUI.App.OnLaunched", ex);
                throw;
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            StartupCrashLogger.LogException("TipMolde.WinUI.App.UnhandledException", e.Exception);
        }
    }
}
