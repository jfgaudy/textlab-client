using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextLabClient.Services;
using TextLabClient.Models;

namespace TextLabClient
{
    public partial class MainWindow : Window
    {
        private readonly TextLabApiService _apiService;
        private readonly ObservableCollection<TreeViewItemModel> _treeViewItems;
        private readonly ObservableCollection<DocumentDisplayModel> _documents;
        private Repository? _selectedRepository;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new TextLabApiService();
            _treeViewItems = new ObservableCollection<TreeViewItemModel>();
            _documents = new ObservableCollection<DocumentDisplayModel>();
            
            // Initialisation
            LoadSettings();
            SetStatus("Application démarrée");
            
            // Binding des collections
            RepositoriesTreeView.ItemsSource = _treeViewItems;
            DocumentsDataGrid.ItemsSource = _documents;
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
                
                _treeViewItems.Clear();
                
                if (repositories != null && repositories.Count > 0)
                {
                    foreach (var repo in repositories)
                    {
                        var displayName = $"📁 {repo.Name}";
                        if (repo.IsDefault) displayName += " (défaut)";
                        
                        var treeItem = new TreeViewItemModel
                        {
                            DisplayName = displayName,
                            ItemType = "repository",
                            Tag = repo
                        };
                        
                        _treeViewItems.Add(treeItem);
                    }
                    SetStatus($"{repositories.Count} repository(s) chargé(s)");
                }
                else
                {
                    var noRepoItem = new TreeViewItemModel
                    {
                        DisplayName = "Aucun repository trouvé",
                        ItemType = "empty"
                    };
                    _treeViewItems.Add(noRepoItem);
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

        private async void RepositoriesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItemModel item && item.Tag is Repository repository)
            {
                _selectedRepository = repository;
                DocumentsHeaderText.Text = $"Documents - {repository.Name}";
                SetStatus($"Repository sélectionné: {repository.Name}");
                
                // Afficher l'onglet Documents et charger les documents
                DocumentsTab.Visibility = Visibility.Visible;
                MainTabControl.SelectedItem = DocumentsTab;
                
                await LoadDocuments(repository.Id);
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

        private async System.Threading.Tasks.Task LoadDocuments(string repositoryId)
        {
            try
            {
                SetStatus("Chargement des documents...");
                var documents = await _apiService.GetDocumentsAsync(repositoryId);
                
                _documents.Clear();
                
                if (documents != null && documents.Count > 0)
                {
                    foreach (var doc in documents)
                    {
                        var displayModel = new DocumentDisplayModel
                        {
                            Id = doc.Id,
                            Title = doc.Title,
                            Content = doc.Content,
                            Category = doc.Category,
                            GitPath = doc.GitPath,
                            CommitSha = doc.CommitSha,
                            Version = doc.Version,
                            RepositoryId = doc.RepositoryId,
                            CreatedAt = doc.CreatedAt,
                            UpdatedAt = doc.UpdatedAt
                        };
                        _documents.Add(displayModel);
                    }
                    
                    DocumentsCountText.Text = $"({documents.Count} document(s))";
                    SetStatus($"{documents.Count} document(s) chargé(s)");
                }
                else
                {
                    DocumentsCountText.Text = "(aucun document)";
                    SetStatus("Aucun document trouvé dans ce repository");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur chargement documents: {ex.Message}");
                DocumentsCountText.Text = "(erreur)";
            }
        }

        private async void RefreshDocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository != null)
            {
                await LoadDocuments(_selectedRepository.Id);
            }
        }

        private void NewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null)
            {
                MessageBox.Show("Veuillez d'abord sélectionner un repository", "Information", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            MessageBox.Show("Création de nouveau document - À implémenter en Phase 4", "Information", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DocumentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is DocumentDisplayModel document)
            {
                SetStatus($"Document sélectionné: {document.Title}");
            }
        }

        private void DocumentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is DocumentDisplayModel document)
            {
                MessageBox.Show($"Ouverture du document: {document.Title}\n\nÀ implémenter en Phase 3", 
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }
    }
} 