using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Configuration;
using System.Diagnostics;
using ChatModule.Repositories;
using ChatModule.Services;
using ChatModule.src.views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChatModule
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static Window? MainAppWindow { get; private set; }
        private Window? _window;
        public DatabaseManager? DatabaseManager { get; private set; }

        public static void SetMainWindow(Window window)
        {
            MainAppWindow = window;
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var configuredConnection = ConfigurationManager.ConnectionStrings["ChatModuleDb"]?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(configuredConnection))
            {
                DatabaseManager = new DatabaseManager(configuredConnection);
            }

            var db = DatabaseManager
                     ?? new DatabaseManager("Data Source=localhost;Initial Catalog=ChatModule;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;");

            var authService = new AuthService(new UserRepository(db));

            var loginWindow = new LoginWindow(authService);
            loginWindow.LoginSucceeded += (userId, username) =>
            {
                try
                {
                    var mainWindow = new MainWindow(userId, username);
                    MainAppWindow = mainWindow;
                    _window = mainWindow;
                    mainWindow.Activate();
                    loginWindow.DispatcherQueue.TryEnqueue(() => loginWindow.Close());
                }
                catch (Exception ex)
                {
                    LogException("LoginSuccessTransition", ex.ToString());
                    loginWindow.ViewModel.ErrorMessage = "Failed to open main window. See crash log.";
                }

                return System.Threading.Tasks.Task.CompletedTask;
            };

            _window = loginWindow;
            MainAppWindow = _window;
            _window.Activate();
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogException("UnhandledException", e.Exception?.ToString() ?? e.Message);
            e.Handled = true;
        }

        private static void LogException(string source, string details)
        {
            try
            {
                var directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatModule");
                Directory.CreateDirectory(directory);
                var filePath = System.IO.Path.Combine(directory, "crash.log");
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{details}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
                File.AppendAllText(filePath, entry);
                Debug.WriteLine(entry);
            }
            catch
            {
                // Intentionally ignored to avoid recursive failure while logging crashes.
            }
        }
    }
}
