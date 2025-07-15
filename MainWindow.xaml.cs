using System;
using System.Windows;
using System.Windows.Controls;
using TextLabClient.Services;
using TextLabClient.Models;

namespace TextLabClient
{
    public partial class MainWindow : Window
    {
        private readonly TextLabApiService _apiService;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new TextLabApiService();
            
            // Initialisation
            LoadSettings();
            SetStatus("Application démarrée");
        }

        private void LoadSettings()
        {
            var settings = ConfigurationService.LoadSettings();
            ApiUrlTextBox.Text = settings.ApiUrl;
            CurrentApiUrlText.Text = settings.ApiUrl;
        }

        private void SaveSettings()
        {
            var settings = new AppSettings
            {
                ApiUrl = ApiUrlTextBox.Text
            };
            ConfigurationService.SaveSettings(settings);
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message;
            LastUpdateText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void SetConnectionStatus(string status, System.Windows.Media.Brush color)
        {
            ConnectionStatusText.Text = status;
            ConnectionStatusText.Foreground = color;
        }

        // Event Handlers
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            await TestConnection();
        }

        private async void TestConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await TestConnection();
        }

        private async System.Threading.Tasks.Task TestConnection()
        {
            try
            {
                SetStatus("Test de connexion en cours...");
                SetConnectionStatus("Test en cours...", System.Windows.Media.Brushes.Orange);
                
                // Sauvegarder l'URL
                SaveSettings();
                CurrentApiUrlText.Text = ApiUrlTextBox.Text;
                
                // Tester la connexion
                _apiService.SetBaseUrl(ApiUrlTextBox.Text);
                var healthInfo = await _apiService.TestConnectionAsync();
                
                if (healthInfo != null)
                {
                    SetConnectionStatus("✅ Connecté", System.Windows.Media.Brushes.Green);
                    ApiVersionText.Text = healthInfo.Version ?? "N/A";
                    SetStatus("Connexion réussie");
                    
                    // Charger les repositories
                    await LoadRepositories();
                }
                else
                {
                    SetConnectionStatus("❌ Échec", System.Windows.Media.Brushes.Red);
                    ApiVersionText.Text = "-";
                    SetStatus("Échec de la connexion");
                }
            }
            catch (Exception ex)
            {
                SetConnectionStatus("❌ Erreur", System.Windows.Media.Brushes.Red);
                ApiVersionText.Text = "-";
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur lors du test de connexion:\n{ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadRepositories()
        {
            try
            {
                SetStatus("Chargement des repositories...");
                var repositories = await _apiService.GetRepositoriesAsync();
                
                RepositoriesTreeView.Items.Clear();
                
                if (repositories != null && repositories.Count > 0)
                {
                    foreach (var repo in repositories)
                    {
                        var treeItem = new TreeViewItem
                        {
                            Header = $"📁 {repo.Name} {(repo.IsDefault ? "(défaut)" : "")}",
                            Tag = repo
                        };
                        RepositoriesTreeView.Items.Add(treeItem);
                    }
                    SetStatus($"{repositories.Count} repository(s) chargé(s)");
                }
                else
                {
                    var noRepoItem = new TreeViewItem
                    {
                        Header = "Aucun repository trouvé",
                        IsEnabled = false
                    };
                    RepositoriesTreeView.Items.Add(noRepoItem);
                    SetStatus("Aucun repository trouvé");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur chargement repositories: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_apiService.IsConnected)
            {
                await LoadRepositories();
            }
            else
            {
                SetStatus("Veuillez d'abord tester la connexion");
            }
        }

        private void RepositoriesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is Repository repository)
            {
                MainContentHeader.Text = $"Repository: {repository.Name}";
                SetStatus($"Repository sélectionné: {repository.Name}");
            }
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fenêtre de paramètres - À implémenter en Phase 8", "Information", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TextLab Client v1.0\n\nClient Windows pour TextLab API\nPhase 1 - Setup et Configuration", 
                          "À propos", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }
    }
} 