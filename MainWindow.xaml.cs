#nullable enable
using System;
using System.Collections.ObjectModel;
using System.IO;
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
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textlab_debug.log");

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _apiService = new TextLabApiService();
                _repositories = new ObservableCollection<Repository>();
                
                // Initialisation
                LogDebug("Application démarrée - Initialisation");
                LogDebug($"Fichier de log: {_logFilePath}");
                LoadSettings();
                SetStatus("Application démarrée");
                RepositoriesListBox.ItemsSource = _repositories;
                
                // Attacher l'événement Expanded au TreeView
                DocumentsTreeView.Loaded += DocumentsTreeView_Loaded;
            }
            catch (Exception ex)
            {
                LogDebug($"Erreur d'initialisation: {ex.Message}");
                MessageBox.Show($"Erreur d'initialisation:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogDebug(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logMessage);
                System.Diagnostics.Debug.WriteLine(message);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        private void DocumentsTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Attacher l'événement Expanded à tous les TreeViewItems
            AttachExpandedEventToTreeViewItems(DocumentsTreeView);
        }

        private void AttachExpandedEventToTreeViewItems(ItemsControl itemsControl)
        {
            if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is System.Windows.Controls.TreeViewItem treeViewItem)
                    {
                        // Vérifier si l'événement n'est pas déjà attaché
                        treeViewItem.Expanded -= TreeViewItem_Expanded; // Supprimer s'il existe
                        treeViewItem.Expanded += TreeViewItem_Expanded; // Ajouter
                        
                        LogDebug($"🔗 Événement attaché à item index {i}");
                        AttachExpandedEventToTreeViewItems(treeViewItem);
                    }
                }
            }
            else
            {
                itemsControl.ItemContainerGenerator.StatusChanged += (s, args) =>
                {
                    if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                    {
                        for (int i = 0; i < itemsControl.Items.Count; i++)
                        {
                            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is System.Windows.Controls.TreeViewItem treeViewItem)
                            {
                                // Vérifier si l'événement n'est pas déjà attaché
                                treeViewItem.Expanded -= TreeViewItem_Expanded; // Supprimer s'il existe
                                treeViewItem.Expanded += TreeViewItem_Expanded; // Ajouter
                                
                                LogDebug($"🔗 Événement attaché à item index {i} (StatusChanged)");
                                AttachExpandedEventToTreeViewItems(treeViewItem);
                            }
                        }
                    }
                };
            }
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TreeViewItem treeViewItem && 
                treeViewItem.DataContext is DocumentTreeItem docItem &&
                docItem.Type == "document" && 
                docItem.Tag is Document document)
            {
                LogDebug($"🔍 TreeView développé pour document: {document.Title}");
                
                // Protection contre les appels multiples : vérifier si déjà traité
                if (docItem.Info.Contains("version"))
                {
                    LogDebug($"⚠️ Document {document.Title} déjà traité - ignorer");
                    return;
                }
                
                // Vérifier si nous avons un lazy-placeholder (premier chargement)
                if (docItem.Children.Count == 1 && 
                    docItem.Children[0].Type == "lazy-placeholder")
                {
                    LogDebug($"🚀 Premier chargement paresseux pour: {document.Title}");
                    
                    // Supprimer le placeholder temporairement
                    docItem.Children.Clear();
                    
                    try
                    {
                        // D'abord vérifier le nombre de versions
                        LogDebug($"🔢 Vérification du nombre de versions pour: {document.Title}");
                        var versionsCount = await _apiService.GetDocumentVersionsCountAsync(document.Id);
                        LogDebug($"📊 {document.Title} a {versionsCount} version(s)");
                        
                        if (versionsCount > 1)
                        {
                            // Charger les détails des versions pour obtenir la date de la version actuelle
                            await LoadDocumentVersionsForTree(docItem, document);
                            
                            // Récupérer la date de la version actuelle
                            var currentVersionDate = GetCurrentVersionDate(docItem);
                            if (currentVersionDate.HasValue)
                            {
                                // Mettre à jour l'info avec la date de la version actuelle
                                docItem.Info = $"Modifié: {currentVersionDate.Value:dd/MM/yyyy} ({versionsCount} versions)";
                            }
                            else
                            {
                                // Fallback si pas de version actuelle trouvée
                                docItem.Info += $" ({versionsCount} versions)";
                            }
                            
                            LogDebug($"✅ Versions chargées pour: {document.Title}");
                        }
                        else if (versionsCount == 1)
                        {
                            // Une seule version, pas besoin d'afficher dans l'arbre
                            docItem.Info += " (1 version)";
                            LogDebug($"ℹ️ {document.Title} n'a qu'une seule version - rien à afficher");
                        }
                        else
                        {
                            LogDebug($"⚠️ {document.Title} n'a aucune version détectée");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"❌ Erreur chargement paresseux pour {document.Title}: {ex.Message}");
                        
                        // En cas d'erreur, remettre un placeholder
                        var errorPlaceholder = new DocumentTreeItem(
                            "Erreur de chargement", 
                            "❌", 
                            "",
                            "error"
                        );
                        docItem.Children.Add(errorPlaceholder);
                    }
                    
                    // Rafraîchir l'affichage du TreeView
                    treeViewItem.Items.Refresh();
                }
                
                // Gérer aussi l'ancien type "placeholder" pour compatibilité
                else if (docItem.Children.Count == 1 && 
                         docItem.Children[0].Type == "placeholder")
                {
                    LogDebug($"📥 Chargement paresseux des versions pour: {document.Title}");
                    
                    // Supprimer le placeholder
                    docItem.Children.Clear();
                    
                    // Charger les versions maintenant
                    try
                    {
                        await LoadDocumentVersionsForTree(docItem, document);
                        LogDebug($"✅ Chargement paresseux terminé pour: {document.Title}");
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"❌ Erreur chargement paresseux pour {document.Title}: {ex.Message}");
                    }
                    
                    // Rafraîchir l'affichage du TreeView
                    treeViewItem.Items.Refresh();
                }
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
                LogDebug($"🚀 LoadDocuments démarré pour repository: {_selectedRepository.Name}");
                SetStatus($"Chargement des documents de {_selectedRepository.Name}...");
                
                var documents = await _apiService.GetDocumentsAsync(_selectedRepository.Id);
                LogDebug($"📄 Documents récupérés: {documents?.Count ?? 0}");
                
                DocumentsTreeView.Items.Clear();
                
                if (documents != null && documents.Count > 0)
                {
                    LogDebug($"🏗️ Construction de l'arbre pour {documents.Count} documents");
                    
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
                        LogDebug($"📂 Traitement catégorie: {category.Key} ({category.Count()} documents)");
                        
                        var categoryNode = new DocumentTreeItem(
                            category.Key, 
                            "📂", 
                            $"{category.Count()} document(s)",
                            "folder"
                        );
                        
                        foreach (var doc in category.OrderBy(d => d.Title))
                        {
                            LogDebug($"📄 Traitement document: {doc.Title} (ID: {doc.Id})");
                            
                            var docIcon = GetDocumentIcon(doc.Category);
                            var docInfo = $"Modifié: {doc.UpdatedAt:dd/MM/yyyy}";
                            
                            var docNode = new DocumentTreeItem(
                                doc.Title ?? "Sans titre", 
                                docIcon, 
                                docInfo,
                                "document"
                            );
                            docNode.Tag = doc;
                            
                            // Chargement vraiment paresseux : ajouter un placeholder pour tous les documents
                            // Le nombre de versions sera vérifié seulement lors du premier clic
                            var placeholderNode = new DocumentTreeItem(
                                "Cliquer pour voir les versions...", 
                                "🔍", 
                                "",
                                "lazy-placeholder"
                            );
                            docNode.Children.Add(placeholderNode);
                            
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
                    
                    // Attacher les événements aux nouveaux items avec délai pour s'assurer que le rendu est terminé
                    Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        // Attendre un court délai pour s'assurer que tous les conteneurs sont générés
                        await System.Threading.Tasks.Task.Delay(100);
                        AttachExpandedEventToTreeViewItems(DocumentsTreeView);
                        LogDebug("🔗 Événements Expanded attachés à tous les TreeViewItems");
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    
                    LogDebug($"🎯 Arbre construit avec succès - {documents.Count} documents chargés");
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
                LogDebug($"❌ Erreur dans LoadDocuments: {ex.Message}");
                SetStatus($"Erreur lors du chargement des documents: {ex.Message}");
                MessageBox.Show($"Erreur:\n{ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadDocumentsButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task LoadDocumentVersionsForTree(DocumentTreeItem docNode, Document document)
        {
            try
            {
                LogDebug($"=== Chargement versions pour: {document.Title} ===");
                var versionsResult = await _apiService.GetDocumentVersionsAsync(document.Id);
                
                LogDebug($"Versions result: {versionsResult?.TotalVersions ?? 0} versions trouvées");
                if (versionsResult?.Versions != null)
                {
                    LogDebug($"Versions.Count: {versionsResult.Versions.Count}");
                    foreach (var v in versionsResult.Versions)
                    {
                        LogDebug($"  - Version: {v.Version}, SHA: {v.CommitSha}, Date: {v.Date}");
                    }
                }
                
                if (versionsResult != null && versionsResult.Versions.Count > 1)
                {
                    LogDebug($"Document {document.Title} a {versionsResult.Versions.Count} versions - ajout dans l'arbre");
                    
                    // Seulement afficher les versions si il y en a plus d'une
                    foreach (var version in versionsResult.Versions.OrderByDescending(v => v.Date))
                    {
                        var versionIcon = version.IsCurrent ? "🔷" : "📦"; // Icône différente pour la version actuelle
                        var versionInfo = $"{version.Author} - {version.Date:dd/MM/yyyy}";
                        if (version.IsCurrent)
                        {
                            versionInfo += " (actuelle)";
                        }
                        
                        LogDebug($"Ajout version: {version.Version} avec icône {versionIcon}");
                        
                        var versionNode = new DocumentTreeItem(
                            version.Version, 
                            versionIcon, 
                            versionInfo,
                            "version",
                            version,
                            version.CommitSha
                        );
                        
                        docNode.Children.Add(versionNode);
                    }
                    
                    LogDebug($"LoadDocumentVersionsForTree terminé - {versionsResult.Versions.Count} versions ajoutées à l'arbre");
                }
                else if (versionsResult != null && versionsResult.Versions.Count == 1)
                {
                    // Une seule version, pas besoin d'afficher dans l'arbre
                    LogDebug($"Document {document.Title} a 1 seule version - pas d'affichage dans l'arbre");
                }
                else
                {
                    LogDebug($"Document {document.Title} - aucune version trouvée ou erreur");
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur de chargement des versions, continuer silencieusement
                LogDebug($"ERREUR chargement versions pour {document.Title}: {ex.Message}");
            }
        }

        private DateTime? GetCurrentVersionDate(DocumentTreeItem docItem)
        {
            // Chercher la version marquée comme actuelle (avec icône 🔷)
            var currentVersion = docItem.Children.FirstOrDefault(child => 
                child.Type == "version" && child.Icon == "🔷");
            
            if (currentVersion?.Version != null)
            {
                LogDebug($"📅 Version actuelle trouvée: {currentVersion.Version.Version} - Date: {currentVersion.Version.Date}");
                return currentVersion.Version.Date;
            }
            
            // Fallback: prendre la première version si aucune n'est marquée comme actuelle
            var firstVersion = docItem.Children.FirstOrDefault(child => child.Type == "version");
            if (firstVersion?.Version != null)
            {
                LogDebug($"📅 Utilisation de la première version: {firstVersion.Version.Version} - Date: {firstVersion.Version.Date}");
                return firstVersion.Version.Date;
            }
            
            LogDebug($"⚠️ Aucune version trouvée pour extraire la date");
            return null;
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
                    SetStatus($"Document sélectionné: {doc.Title} (ID: {doc.Id}) - Double-cliquez pour voir les détails");
                }
                else if (item.Type == "version" && item.Version != null)
                {
                    SetStatus($"Version sélectionnée: {item.Version.Version} ({item.Version.Author}, {item.Version.Date:dd/MM/yyyy}) - Double-cliquez pour ouvrir cette version");
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

        private void DocumentsTreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DocumentsTreeView.SelectedItem is DocumentTreeItem item)
            {
                if (item.Type == "document" && item.Tag is Document doc)
                {
                    // Ouvrir la version actuelle du document
                    OpenDocumentDetails(doc);
                }
                else if (item.Type == "version" && item.Version != null)
                {
                    // Ouvrir une version spécifique du document
                    // Récupérer le document parent
                    var parentDocument = GetParentDocument(item);
                    if (parentDocument != null)
                    {
                        OpenDocumentVersionDetails(parentDocument, item.Version, item.VersionSha ?? "");
                    }
                }
            }
        }

        private Document? GetParentDocument(DocumentTreeItem versionItem)
        {
            // Chercher le document parent dans l'arbre
            return FindParentDocument(DocumentsTreeView.Items, versionItem);
        }

        private Document? FindParentDocument(System.Collections.IEnumerable items, DocumentTreeItem targetVersionItem)
        {
            foreach (DocumentTreeItem item in items)
            {
                if (item.Type == "document" && item.Children.Contains(targetVersionItem))
                {
                    return item.Tag as Document;
                }
                
                var result = FindParentDocument(item.Children, targetVersionItem);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void OpenDocumentDetails(Document document)
        {
            try
            {
                SetStatus($"Ouverture des détails pour: {document.Title}");
                
                var detailsWindow = new DocumentDetailsWindow(document, _apiService);
                detailsWindow.Owner = this;
                detailsWindow.ShowDialog();
                
                SetStatus($"Détails fermés pour: {document.Title}");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur ouverture détails: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ouverture des détails:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDocumentVersionDetails(Document document, DocumentVersion version, string versionSha)
        {
            try
            {
                SetStatus($"Ouverture de la version {version.Version} pour: {document.Title}");
                
                var detailsWindow = new DocumentDetailsWindow(document, _apiService, version, versionSha);
                detailsWindow.Owner = this;
                detailsWindow.ShowDialog();
                
                SetStatus($"Détails de version fermés pour: {document.Title}");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur ouverture détails de version: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ouverture des détails de version:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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