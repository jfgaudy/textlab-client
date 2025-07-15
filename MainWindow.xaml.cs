using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TextLabClient.Services;
using TextLabClient.Models;

namespace TextLabClient
{
    public partial class MainWindow : Window
    {
        private readonly TextLabApiService _apiService;
        private ObservableCollection<Repository> _repositories;
        private Repository? _selectedRepository;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _apiService = new TextLabApiService();
                _repositories = new ObservableCollection<Repository>();
                
                // Initialisation
                LoadSettings();
                SetStatus("Application démarrée");
                RepositoriesListBox.ItemsSource = _repositories;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        }

        private void SetConnectionStatus(string status)
        {
            ConnectionStatusText.Text = status;
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestConnectionButton.IsEnabled = false;
                SetStatus("Test de connexion en cours...");
                SetConnectionStatus("Test...");
                
                // Sauvegarder l'URL
                SaveSettings();
                CurrentApiUrlText.Text = ApiUrlTextBox.Text;
                
                // Tester la connexion
                _apiService.SetBaseUrl(ApiUrlTextBox.Text);
                var healthInfo = await _apiService.TestConnectionAsync();
                
                if (healthInfo != null)
                {
                    SetConnectionStatus("✅ Connecté");
                    ApiVersionText.Text = $"v{healthInfo.Version ?? "N/A"}";
                    SetStatus("Connexion réussie");
                    
                    // Charger automatiquement les repositories
                    await LoadRepositories();
                }
                else
                {
                    SetConnectionStatus("❌ Échec");
                    ApiVersionText.Text = "";
                    SetStatus("Échec de la connexion");
                    _repositories.Clear();
                }
            }
            catch (Exception ex)
            {
                SetConnectionStatus("❌ Erreur");
                ApiVersionText.Text = "";
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur:\n{ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task LoadRepositories()
        {
            try
            {
                SetStatus("Chargement des repositories...");
                var repositories = await _apiService.GetRepositoriesAsync();
                
                _repositories.Clear();
                
                if (repositories == null)
                {
                    SetStatus("Erreur: La réponse de l'API est null");
                    RepositoryInfoText.Text = "Erreur: Réponse API null - Vérifiez les logs debug";
                    return;
                }
                
                if (repositories.Count == 0)
                {
                    SetStatus("L'API a répondu mais la liste est vide");
                    RepositoryInfoText.Text = "Liste vide retournée par l'API";
                    return;
                }
                
                // Si on arrive ici, on a des repositories
                foreach (var repo in repositories)
                {
                    _repositories.Add(repo);
                    SetStatus($"Repository ajouté: {repo.Name} (ID: {repo.Id})");
                }
                
                SetStatus($"✅ {repositories.Count} repository(s) chargé(s) avec succès");
                RepositoryInfoText.Text = $"{repositories.Count} repository(s) disponible(s)";
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Erreur lors du chargement des repositories: {ex.Message}");
                RepositoryInfoText.Text = $"Erreur: {ex.Message}";
                MessageBox.Show($"Erreur détaillée:\n{ex}", "Erreur Debug", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RepositoriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RepositoriesListBox.SelectedItem is Repository selectedRepo)
            {
                _selectedRepository = selectedRepo;
                SelectedRepositoryText.Text = $"Repository: {selectedRepo.Name}";
                LoadDocumentsButton.IsEnabled = true;
                                 RepositoryInfoText.Text = $"ID: {selectedRepo.Id}\nType: {selectedRepo.Type}";
                
                // Charger automatiquement les documents
                _ = LoadDocuments();
            }
            else
            {
                _selectedRepository = null;
                SelectedRepositoryText.Text = "Aucun repository sélectionné";
                LoadDocumentsButton.IsEnabled = false;
                DocumentsTreeView.Items.Clear();
                RepositoryInfoText.Text = "Sélectionnez un repository";
            }
        }

        private async void LoadDocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDocuments();
        }

        private async System.Threading.Tasks.Task LoadDocuments()
        {
            if (_selectedRepository == null) return;

            try
            {
                LoadDocumentsButton.IsEnabled = false;
                SetStatus($"Chargement des documents de {_selectedRepository.Name}...");
                
                var documents = await _apiService.GetDocumentsAsync(_selectedRepository.Id);
                
                DocumentsTreeView.Items.Clear();
                
                if (documents != null && documents.Count > 0)
                {
                    // Créer le nœud racine du repository
                    var repoNode = new DocumentTreeItem(
                        _selectedRepository.Name, 
                        "📁", 
                        $"{documents.Count} document(s)",
                        "repository"
                    );
                    repoNode.Tag = _selectedRepository;
                    
                    // Grouper par catégorie
                    var categories = documents.GroupBy(d => d.Category ?? "Sans catégorie");
                    
                    foreach (var category in categories.OrderBy(c => c.Key))
                    {
                        var categoryNode = new DocumentTreeItem(
                            category.Key, 
                            "📂", 
                            $"{category.Count()} document(s)",
                            "folder"
                        );
                        
                        foreach (var doc in category.OrderBy(d => d.Title))
                        {
                            var docIcon = GetDocumentIcon(doc.Category);
                            var docInfo = $"Modifié: {doc.UpdatedAt:dd/MM/yyyy}";
                            
                            var docNode = new DocumentTreeItem(
                                doc.Title ?? "Sans titre", 
                                docIcon, 
                                docInfo,
                                "document"
                            );
                            docNode.Tag = doc;
                            
                            categoryNode.Children.Add(docNode);
                        }
                        
                        repoNode.Children.Add(categoryNode);
                    }
                    
                    DocumentsTreeView.Items.Add(repoNode);
                    
                    // Développer le nœud racine
                    if (DocumentsTreeView.ItemContainerGenerator.ContainerFromItem(repoNode) is System.Windows.Controls.TreeViewItem container)
                    {
                        container.IsExpanded = true;
                    }
                    
                    SetStatus($"{documents.Count} document(s) chargé(s) pour {_selectedRepository.Name}");
                }
                else
                {
                    var emptyNode = new DocumentTreeItem(
                        "Aucun document trouvé", 
                        "❌", 
                        "",
                        "empty"
                    );
                    DocumentsTreeView.Items.Add(emptyNode);
                    SetStatus($"Aucun document trouvé dans {_selectedRepository.Name}");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors du chargement des documents: {ex.Message}");
                MessageBox.Show($"Erreur:\n{ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadDocumentsButton.IsEnabled = true;
            }
        }

        private string GetDocumentIcon(string? category)
        {
            return category?.ToLower() switch
            {
                "technology" => "⚙️",
                "guides" => "📖",
                "api" => "🔧",
                "tutorials" => "🎓",
                "notes" => "📝",
                "drafts" => "📄",
                _ => "📄"
            };
        }

        private void DocumentsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DocumentTreeItem item)
            {
                if (item.Type == "document" && item.Tag is Document doc)
                {
                    SetStatus($"Document sélectionné: {doc.Title} (ID: {doc.Id})");
                }
                else if (item.Type == "folder")
                {
                    SetStatus($"Catégorie sélectionnée: {item.Name} - {item.Info}");
                }
                else if (item.Type == "repository" && item.Tag is Repository repo)
                {
                    SetStatus($"Repository sélectionné: {repo.Name}");
                }
            }
        }

        private async void RefreshRepositoriesButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadRepositories();
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }
    }
} 