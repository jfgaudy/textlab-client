using System.Windows;
using TextLabClient.Services;

namespace TextLabClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialisation des services
            ConfigurationService.Initialize();
            
            // Gestion des erreurs globales
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Erreur non gérée: {e.Exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
} 