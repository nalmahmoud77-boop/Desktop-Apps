using System;
using System.Windows;
using PersonalTaskManagement.Data;
using PersonalTaskManagement.Licensing;
using PersonalTaskManagement.Messaging;
using PersonalTaskManagement.ViewModels;
using PersonalTaskManagement.Views;
using PersonalTaskManagement.Views.Dialogs;

namespace PersonalTaskManagement
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                DatabaseInitializer.Initialize();
            }
            catch (Exception ex)
            {
                MessageDialog.Error($"Failed to initialize the database.\n\n{ex.Message}",
                    "AgileFlow Startup Error");
                Shutdown(1);
                return;
            }

            // --- License gate (tamper-guarded) ---
            var guard = new TamperGuard();
            guard.Initialize();

            var license = new LicenseService(guard);
            LicenseEvaluation eval = license.Evaluate();

            if (eval.State != LicenseState.Valid)
            {
                var activation = new LicenseWindow(license, eval);
                if (activation.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }
                eval = activation.Result ?? eval;
            }

            var window = new MainWindow
            {
                DataContext = new MainViewModel(Messenger.Default)
                {
                    LicenseSummary = eval.Summary
                }
            };
            window.Show();
        }
    }
}
