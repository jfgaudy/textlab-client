#nullable enable
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TextLabClient.Services;
using TextLabClient.Models;
using System.Collections.Generic; // Added for List

namespace TextLabClient
{
    public partial class MainWindow : Window
    {
        private readonly TextLabApiService _apiService = new TextLabApiService();
        private readonly TextLabAdminService _adminService = new TextLabAdminService();
        private ObservableCollection<Repository> _repositories = new ObservableCollection<Repository>();
        private Repository? _selectedRepository;
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textlab_debug.log");

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Initialisation
                LogDebug("Application démarrée - Initialisation");
                LogDebug($"Fichier de log: {_logFilePath}");
                LoadSettings();
                SetStatus("Application démarrée");
                RepositoriesListBox.ItemsSource = _repositories;
                
                // Test de référence des boutons
                TestButtonReferences();
                
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

        private void TestButtonReferences()
        {
            try
            {
                LogDebug("🔍 Test des références de boutons...");
                
                if (NewDocumentButton != null)
                {
                    LogDebug($"✅ NewDocumentButton trouvé - IsEnabled: {NewDocumentButton.IsEnabled}");
                }
                else
                {
                    LogDebug("❌ NewDocumentButton est NULL");
                }

                if (SyncRepositoryButton != null)
                {
                    LogDebug($"✅ SyncRepositoryButton trouvé - IsEnabled: {SyncRepositoryButton.IsEnabled}");
                }
                else
                {
                    LogDebug("❌ SyncRepositoryButton est NULL");
                }

                if (TestConnectionButton != null)
                {
                    LogDebug($"✅ TestConnectionButton trouvé - IsEnabled: {TestConnectionButton.IsEnabled}");
                }
                else
                {
                    LogDebug("❌ TestConnectionButton est NULL");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur lors du test des boutons: {ex.Message}");
            }
        }

        private void EnableConnectionButtons(bool enabled)
        {
            TestInitButton.IsEnabled = enabled;
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
                            // URL affichée dans le champ ApiUrlTextBox
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
            
            // Mettre à jour l'indicateur visuel en fonction du message
            try
            {
                var indicator = this.FindName("ConnectionStatusIndicator") as System.Windows.Shapes.Ellipse;
                if (indicator != null)
                {
                    if (message.Contains("Erreur") || message.Contains("❌") || message.Contains("Échec"))
                    {
                        indicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D13438")); // Rouge
                    }
                    else if (message.Contains("réussi") || message.Contains("✅") || message.Contains("Connecté") || message.Contains("terminé"))
                    {
                        indicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10")); // Vert
                    }
                    else if (message.Contains("Chargement") || message.Contains("🔄") || message.Contains("Synchronisation"))
                    {
                        indicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")); // Bleu
                    }
                    else
                    {
                        indicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")); // Gris
                    }
                }
            }
            catch
            {
                // Ignorer si l'indicateur n'existe pas encore
            }
        }

        private void SetConnectionStatus(string status)
        {
            ConnectionStatusText.Text = status;
            
            // Mettre à jour aussi la barre de statut
            if (this.FindName("StatusBarConnectionText") is TextBlock statusBarText)
            {
                statusBarText.Text = status;
            }
            
            // Mettre à jour les indicateurs de connexion
            try
            {
                var indicator = this.FindName("ConnectionStatusIndicator") as System.Windows.Shapes.Ellipse;
                var statusBarIndicator = this.FindName("StatusBarConnectionIndicator") as System.Windows.Shapes.Ellipse;
                
                SolidColorBrush color;
                if (status.Contains("Connecté") || status.Contains("✅"))
                {
                    color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10")); // Vert
                }
                else if (status.Contains("Erreur") || status.Contains("❌") || status.Contains("Échec"))
                {
                    color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D13438")); // Rouge
                }
                else if (status.Contains("Test") || status.Contains("🔄"))
                {
                    color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")); // Bleu
                }
                else
                {
                    color = new SolidColorBrush(Colors.Gray); // Gris par défaut
                }
                
                if (indicator != null) indicator.Fill = color;
                if (statusBarIndicator != null) statusBarIndicator.Fill = color;
            }
            catch (Exception ex)
            {
                // Ignorer l'erreur de mise à jour de l'indicateur
            }
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
                
                // Tester la connexion
                _apiService.SetBaseUrl(ApiUrlTextBox.Text);
                var healthInfo = await _apiService.TestConnectionAsync();
                
                if (healthInfo != null)
                {
                    SetConnectionStatus("✅ Connecté");
                    ApiVersionText.Text = $"API v{healthInfo.Version ?? "N/A"}";
                    
                    // URL déjà visible dans le champ ApiUrlTextBox
                    
                    SetStatus("Connexion réussie");
                    
                    // Activer les boutons qui nécessitent une connexion
                    EnableConnectionButtons(true);
                    
                    // Charger automatiquement les repositories
                    await LoadRepositories();
                }
                else
                {
                    SetConnectionStatus("❌ Échec");
                    ApiVersionText.Text = "";
                    SetStatus("Échec de la connexion");
                    _repositories.Clear();
                    
                    // Désactiver les boutons qui nécessitent une connexion
                    EnableConnectionButtons(false);
                }
            }
            catch (Exception ex)
            {
                SetConnectionStatus("❌ Erreur");
                ApiVersionText.Text = "";
                SetStatus($"Erreur de connexion: {ex.Message}");
                _repositories.Clear();
                EnableConnectionButtons(false);
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private async void TestInitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestInitButton.IsEnabled = false;
                SetStatus("Test de récupération des versions...");
                
                // TEST SPÉCIFIQUE DES VERSIONS
                if (_selectedRepository != null)
                {
                    var documents = await _apiService.GetDocumentsAsync(_selectedRepository.Id);
                    
                    if (documents != null && documents.Count > 0)
                    {
                        // Prendre le premier document pour test
                        var firstDoc = documents.First();
                        LogDebug($"🧪 TEST VERSIONS pour document: {firstDoc.Title} (ID: {firstDoc.Id})");
                        
                        var versionsResult = await _apiService.GetDocumentVersionsAsync(firstDoc.Id);
                        
                        if (versionsResult != null)
                        {
                            LogDebug($"🧪 RÉSULTAT: {versionsResult.TotalVersions} versions total");
                            LogDebug($"🧪 RÉSULTAT: {versionsResult.Versions.Count} versions dans la liste");
                            
                            foreach (var v in versionsResult.Versions)
                            {
                                LogDebug($"🧪 VERSION: {v.Version} | SHA: {v.CommitSha} | Auteur: {v.Author} | Date: {v.Date} | IsCurrent: {v.IsCurrent}");
                            }
                            
                            // Afficher le résultat à l'utilisateur
                            var message = $"Document testé: {firstDoc.Title}\n" +
                                         $"Versions trouvées: {versionsResult.TotalVersions}\n" +
                                         $"Versions dans liste: {versionsResult.Versions.Count}\n\n";
                            
                            if (versionsResult.Versions.Count > 0)
                            {
                                message += "Détails des versions:\n";
                                foreach (var v in versionsResult.Versions.Take(3))
                                {
                                    message += $"• {v.Version} ({v.Author}, {v.Date:dd/MM/yyyy})\n";
                                }
                            }
                            
                            MessageBox.Show(message, "🧪 Test des Versions", MessageBoxButton.OK, MessageBoxImage.Information);
                            SetStatus($"✅ Test versions réussi: {versionsResult.TotalVersions} versions trouvées");
                        }
                        else
                        {
                            LogDebug($"🧪 ERREUR: versionsResult est null");
                            MessageBox.Show($"Erreur: Aucune version trouvée pour {firstDoc.Title}\nL'API a retourné null.", 
                                          "❌ Test des Versions", MessageBoxButton.OK, MessageBoxImage.Error);
                            SetStatus("❌ Test versions échec: result null");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Aucun document trouvé pour tester les versions.\nSélectionnez d'abord un repository et chargez les documents.", 
                                      "⚠️ Test des Versions", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Sélectionnez d'abord un repository pour tester les versions.", 
                                  "⚠️ Test des Versions", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Erreur test diagnostic: {ex.Message}");
                LogDebug($"❌ Erreur test diagnostic: {ex.Message}");
                MessageBox.Show($"Erreur lors du test des versions:\n{ex.Message}", 
                              "❌ Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestInitButton.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task LoadRepositories()
        {
            try
            {
                SetStatus("Chargement des repositories...");
                // Utiliser les nouveaux endpoints publics
                var repositories = await _apiService.GetPublicRepositoriesAsync();
                
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
                    SetStatus($"Repository ajouté: {repo.DisplayName} (Type: {repo.TypeDisplay})");
                }
                
                SetStatus($"✅ {repositories.Count} repository(s) chargé(s) avec succès - Ctrl+N pour nouveau document");
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

        // Méthode pour recharger les repositories (alias pour compatibilité)
        private async Task LoadRepositoriesAsync()
        {
            await LoadRepositories();
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
                    
                    // Mettre à jour l'information du repository sélectionné
                    RepositoryInfoText.Text = $"📁 {_selectedRepository.Name} • {documents.Count} document(s) • {_selectedRepository.Type}";
                    
                    // Status plus concis
                    SetStatus($"✅ {documents.Count} document(s) chargé(s)");
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
                    
                    // Mettre à jour l'information du repository même s'il est vide
                    RepositoryInfoText.Text = $"📁 {_selectedRepository.Name} • Aucun document • {_selectedRepository.Type}";
                    
                    SetStatus($"❌ Aucun document trouvé");
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
                
                // Chargement des versions via l'API Phase 6
                var versionsResult = await _apiService.GetDocumentVersionsAsync(document.Id);
                
                LogDebug($"Versions result: {versionsResult?.TotalVersions ?? 0} versions trouvées");
                if (versionsResult != null)
                {
                    LogDebug($"Versions.Count: {versionsResult.Versions.Count}");
                    foreach (var v in versionsResult.Versions)
                    {
                        LogDebug($"  - Version: {v.Version}, SHA: {v.CommitSha}, Date: {v.Date}");
                    }
                }
                
                if (versionsResult != null && versionsResult.Versions.Count > 0)
                {
                    // Ajouter directement les versions sous le document (sans dossier intermédiaire)
                    foreach (var version in versionsResult.Versions.OrderByDescending(v => v.Date))
                    {
                        var versionItem = new DocumentTreeItem
                        {
                            Name = $"📄 {version.Version} - {version.CommitSha?.Substring(0, 7)} ({version.Date:dd/MM/yyyy HH:mm})",
                            Info = $"Version: {version.Version}\nSHA: {version.CommitSha}\nAuteur: {version.Author}\nDate: {version.Date:dd/MM/yyyy HH:mm:ss}\nMessage: {version.Message}",
                            Type = "version",
                            Icon = "📄",
                            Version = version,
                            VersionSha = version.CommitSha
                        };
                        docNode.Children.Add(versionItem);
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"ERREUR chargement versions: {ex.Message}");
                
                // Ajouter un message d'erreur informatif
                var errorItem = new DocumentTreeItem
                {
                    Name = "❌ Erreur de connexion API",
                    Info = $"Erreur: {ex.Message}\n\nLes endpoints /versions ne sont pas encore disponibles dans l'API de production."
                };
                docNode.Children.Add(errorItem);
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

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Actualiser les documents du repository sélectionné
            if (_selectedRepository != null)
            {
                await LoadDocuments();
                SetStatus($"Documents actualisés pour {_selectedRepository.Name}");
            }
            else
            {
                await LoadRepositories();
                SetStatus("Repositories actualisés");
            }
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Raccourci Ctrl+N pour nouveau document
            if (e.Key == System.Windows.Input.Key.N && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                LogDebug("🎹 Raccourci Ctrl+N détecté - création de document");
                NewDocumentButton_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            // Raccourci F5 pour actualiser
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                LogDebug("🎹 Raccourci F5 détecté - actualisation");
                RefreshButton_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📚 TextLab Client v2.0\n\nClient Windows moderne pour l'API TextLab\n\n🚀 Fonctionnalités:\n• Gestion multi-repositories\n• Interface moderne avec cartes\n• Visualisation des documents\n• Création de nouveaux documents (Ctrl+N)\n• Synchronisation Git avancée\n• Historique des versions\n\n⌨️ Raccourcis:\n• Ctrl+N : Nouveau document\n• F5 : Actualiser\n\n🎨 Interface modernisée avec design Microsoft !",
                           "À propos de TextLab Client", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void SyncAllRepositoriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Synchronisation de tous les repositories...");
                LogDebug("🔄 Début de synchronisation de tous les repositories");
                
                // Si on a le service admin, utiliser la méthode de pull de tous les repos
                var repositories = _repositories.ToList();
                var successCount = 0;
                var errorCount = 0;
                
                foreach (var repo in repositories)
                {
                    try
                    {
                        LogDebug($"🔄 Synchronisation de {repo.Name}...");
                        var pullResult = await _adminService.PullRepositoryAsync(repo.Id);
                        
                        if (pullResult?.Success == true)
                        {
                            successCount++;
                            LogDebug($"✅ Synchronisation réussie pour {repo.Name}");
                        }
                        else
                        {
                            errorCount++;
                            LogDebug($"❌ Échec synchronisation pour {repo.Name}: {pullResult?.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LogDebug($"❌ Erreur synchronisation {repo.Name}: {ex.Message}");
                    }
                }
                
                SetStatus($"Synchronisation terminée: {successCount} réussies, {errorCount} erreurs");
                
                if (successCount > 0)
                {
                    // Actualiser la liste des repositories
                    await LoadRepositories();
                    if (_selectedRepository != null)
                    {
                        await LoadDocuments();
                    }
                }
                
                MessageBox.Show($"Synchronisation terminée:\n\n✅ {successCount} repositories synchronisés\n❌ {errorCount} erreurs\n\nConsultez les logs pour plus de détails.",
                               "Synchronisation", MessageBoxButton.OK, 
                               successCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de la synchronisation: {ex.Message}");
                LogDebug($"❌ Erreur dans SyncAllRepositoriesButton_Click: {ex.Message}");
                MessageBox.Show($"Erreur lors de la synchronisation:\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logs = await LoggingService.GetLogsContentAsync(200); // 200 dernières lignes
                
                var logsWindow = new Window
                {
                    Title = "Logs TextLab Client",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                var textBlock = new TextBlock
                {
                    Text = logs,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                };

                scrollViewer.Content = textBlock;
                logsWindow.Content = scrollViewer;
                
                // Scroller vers la fin
                logsWindow.Loaded += (s, args) => scrollViewer.ScrollToEnd();
                
                logsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture des logs: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenLogsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logPath = LoggingService.GetLogFilePath();
                var logDir = Path.GetDirectoryName(logPath);
                
                if (Directory.Exists(logDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logDir);
                }
                else
                {
                    MessageBox.Show("Le dossier de logs n'existe pas encore.\nLes logs seront créés au premier lancement.", 
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du dossier: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier que nous avons une connexion
                if (!_apiService.IsConnected)
                {
                    MessageBox.Show("❌ Aucune connexion à l'API. Testez la connexion d'abord.",
                                   "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Déterminer le repository pré-sélectionné
                string? preSelectedRepositoryId = null;
                
                // Si un repository est sélectionné dans la liste
                if (_selectedRepository != null)
                {
                    preSelectedRepositoryId = _selectedRepository.Id;
                    LogDebug($"📁 Repository pré-sélectionné: {_selectedRepository.Name} ({_selectedRepository.Id})");
                }
                // Sinon, essayer de détecter depuis l'arbre sélectionné
                else if (DocumentsTreeView.SelectedItem is DocumentTreeItem selectedItem)
                {
                    if (selectedItem.Type == "repository" && selectedItem.Tag is Repository repoFromTree)
                    {
                        preSelectedRepositoryId = repoFromTree.Id;
                        LogDebug($"📁 Repository détecté depuis l'arbre: {repoFromTree.Name} ({repoFromTree.Id})");
                    }
                    else if (selectedItem.Type == "document" || selectedItem.Type == "folder" || selectedItem.Type == "version")
                    {
                        // Pour les documents, versions ou dossiers, utiliser le repository actuellement sélectionné
                        // car l'arbre est organisé par repository
                        if (_selectedRepository != null)
                        {
                            preSelectedRepositoryId = _selectedRepository.Id;
                            LogDebug($"📁 Repository détecté depuis sélection courante: {_selectedRepository.Name} ({_selectedRepository.Id})");
                        }
                    }
                }

                // Ouvrir la fenêtre de création
                var newDocWindow = new NewDocumentWindow(_apiService);
                newDocWindow.Owner = this;

                SetStatus("Ouverture de la fenêtre de création de document...");

                if (newDocWindow.ShowDialog() == true)
                {
                    LogDebug($"✅ Document créé avec succès");

                    // Actualiser les documents pour voir le nouveau document
                    if (_selectedRepository != null)
                    {
                        await LoadDocuments();
                        SetStatus($"Document créé et liste actualisée");
                    }
                    else
                    {
                        // Si aucun repository n'était sélectionné, charger tous les repositories
                        await LoadRepositories();
                        SetStatus($"Document créé - actualisez le repository pour le voir");
                    }

                    // Actualiser la sélection si nécessaire
                    if (_selectedRepository != null)
                    {
                        LogDebug($"Repository actuel maintenu: {_selectedRepository.Name}");
                    }
                }
                else
                {
                    SetStatus("Création de document annulée");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur dans NewDocumentButton_Click: {ex.Message}");
                MessageBox.Show($"❌ Erreur lors de l'ouverture de la fenêtre de création:\n\n{ex.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Erreur lors de la création de document");
            }
        }

        // ===== GESTION DES REPOSITORIES =====

        private void ManageRepositoriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("Ouverture de la fenêtre de gestion des repositories");
                var repositoryWindow = new RepositoryManagementWindow
                {
                    Owner = this
                };
                repositoryWindow.ShowDialog();
                
                // Rafraîchir la liste des repositories après fermeture
                _ = LoadRepositories();
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur dans ManageRepositoriesButton_Click: {ex.Message}");
                MessageBox.Show($"❌ Erreur lors de l'ouverture de la gestion des repositories:\n\n{ex.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SyncRepositoriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("Synchronisation de tous les repositories");
                SetStatus("Synchronisation en cours...");

                // Récupérer tous les repositories
                var repositories = await _adminService.GetRepositoriesAsync();
                int successCount = 0;
                int errorCount = 0;

                foreach (var repo in repositories.Where(r => r.IsActive))
                {
                    try
                    {
                        var pullRequest = new PullRequest
                        {
                            RepositoryId = repo.Id,
                            AutoResolveConflicts = false,
                            ForcePull = false
                        };

                        var response = await _adminService.PullRepositoryAsync(repo.Id, pullRequest);
                        if (response?.Success == true)
                        {
                            successCount++;
                            LogDebug($"✅ Pull réussi pour {repo.Name}: {response.Changes.CommitsPulled} commits");
                        }
                        else
                        {
                            errorCount++;
                            LogDebug($"❌ Échec du pull pour {repo.Name}: {response?.Error ?? "Erreur inconnue"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LogDebug($"❌ Exception lors du pull de {repo.Name}: {ex.Message}");
                    }
                }

                SetStatus($"Synchronisation terminée: {successCount} réussis, {errorCount} échecs");
                
                string message = $"Synchronisation terminée:\n" +
                               $"• {successCount} repositories synchronisés avec succès\n" +
                               $"• {errorCount} erreurs";

                MessageBox.Show(message, "Synchronisation", 
                               errorCount == 0 ? MessageBoxButton.OK : MessageBoxButton.OK,
                               errorCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

                // Rafraîchir la liste des documents
                await LoadDocuments();
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur dans SyncRepositoriesButton_Click: {ex.Message}");
                MessageBox.Show($"❌ Erreur lors de la synchronisation:\n\n{ex.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Erreur lors de la synchronisation");
            }
        }

        // Méthode supprimée - info repository maintenant dans le panneau gauche

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }
    }
} 