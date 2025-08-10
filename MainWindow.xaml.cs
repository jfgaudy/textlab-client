#nullable enable
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
        private readonly LLMCenterAuthService _authService = new LLMCenterAuthService();
        private readonly TextLabAdminService _adminService;
        private TextLabApiService? _apiService;
        private CancellationTokenSource? _refreshCancellationTokenSource;
        private ObservableCollection<Repository> _repositories = new ObservableCollection<Repository>();
        private Repository? _selectedRepository;
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "textlab_debug.log");

        
        // 🚀 OPTIMISATION: Cache des versions pour éviter les rechargements
        private readonly Dictionary<string, DocumentVersions> _versionsCache = new();
        
        // 📂 VUES: Gestion des vues virtuelles
        private DocumentView? _currentView;
        private string _currentViewType = "all";

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialiser les services avec dépendance
            _apiService = new TextLabApiService(_authService);
            _adminService = new TextLabAdminService("", _authService); // URL sera définie dynamiquement
            
            // Initialisation
            LogDebug("Application démarrée - Initialisation");
            LogDebug($"Fichier de log: {_logFilePath}");
            LoadSettings();
            SetStatus("Application started");
            RepositoriesListBox.ItemsSource = _repositories;
            
            // Test de référence des boutons
            TestButtonReferences();
            
            // Attacher l'événement Expanded au TreeView
            DocumentsTreeView.Loaded += DocumentsTreeView_Loaded;
            
            // Attacher l'événement de chargement pour l'authentification
            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Événement déclenché quand la fenêtre principale se charge
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // L'utilisateur doit explicitement cliquer "Connecter"
            SetStatus("Application started - Click 'Connect' to authenticate and access repositories");
            await LoggingService.LogInfoAsync("🚀 Application démarrée - En attente de connexion manuelle");
        }

        /// <summary>
        /// Affiche la fenêtre de connexion et gère l'authentification
        /// </summary>
        private async Task ShowLoginDialogAsync()
        {
            try
            {
                var loginWindow = new LoginWindow(_authService);
                loginWindow.Owner = this;
                
                var result = loginWindow.ShowDialog();
                
                if (result == true && loginWindow.LoginSuccessful)
                {
                    await LoggingService.LogInfoAsync("✅ Connexion utilisateur réussie");
                    
                    var userInfo = await _authService.GetCurrentUserAsync();
                    if (userInfo != null)
                    {
                        SetStatus($"Connected as {userInfo.Username} - Test API connection");
                        await LoggingService.LogInfoAsync($"👤 Utilisateur connecté: {userInfo.Username}");
                        
                        // ❌ SUPPRIMÉ: Ne plus charger les repositories ici pour éviter le double chargement
                        // Les repositories seront chargés uniquement via le bouton "Connecter"
                    }
                }
                else
                {
                    await LoggingService.LogWarningAsync("❌ Connexion annulée par l'utilisateur");
                    SetStatus("Connection cancelled - Limited features");
                    
                    // Possibilité de fermer l'application ou continuer en mode limité
                    var response = MessageBox.Show(
                        "Sans authentification, l'accès aux données TextLab est limité.\n\nVoulez-vous réessayer de vous connecter ?",
                        "Authentification requise",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (response == MessageBoxResult.Yes)
                    {
                        await ShowLoginDialogAsync(); // Récursif
                    }
                }
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur fenêtre de connexion: {ex.Message}");
                MessageBox.Show($"Erreur lors de la connexion:\n{ex.Message}", 
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
                        // 🚀 OPTIMISATION: Charger directement les versions (un seul appel API)
                        LogDebug($"🔢 Chargement optimisé des versions pour: {document.Title}");
                        var versionsResult = await LoadDocumentVersionsForTree(docItem, document);
                        var versionsCount = versionsResult?.TotalVersions ?? 0;
                        LogDebug($"📊 {document.Title} a {versionsCount} version(s)");
                        
                        if (versionsCount > 1)
                        {
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
                SetConnectionStatus("⏳ Connexion...");
                SetStatus("Test de connexion en cours...");
                
                // 1. AUTHENTIFICATION D'ABORD
                if (!_authService.IsAuthenticated())
                {
                    await LoggingService.LogInfoAsync("⚠️ Utilisateur non authentifié - ouverture de la fenêtre de connexion");
                    
                    await ShowLoginDialogAsync();
                    
                    if (!_authService.IsAuthenticated())
                    {
                        SetConnectionStatus("❌ Non authentifié");
                        SetStatus("Connexion annulée - authentification requise");
                        return;
                    }
                }
                
                var userInfo = await _authService.GetCurrentUserAsync();
                await LoggingService.LogInfoAsync($"👤 User connected: {userInfo?.Username ?? "Unknown"}");
                
                // 2. CONFIGURATION DE L'URL API
                await LoggingService.LogInfoAsync($"🌐 API Configuration to: {ApiUrlTextBox.Text}");
                _apiService.SetBaseUrl(ApiUrlTextBox.Text);
                _adminService.SetBaseUrl(ApiUrlTextBox.Text);
                
                // 3. TEST DE CONNEXION API
                var healthInfo = await _apiService.TestConnectionAsync();
                
                if (healthInfo != null)
                {
                    SetConnectionStatus("✅ Connecté");
                    ApiVersionText.Text = $"API v{healthInfo.Version ?? "N/A"}";
                    
                    var statusMessage = $"Connexion réussie en tant que {userInfo?.Username ?? "utilisateur"}";
                    if (!string.IsNullOrEmpty(healthInfo.Version))
                        statusMessage += $" (API v{healthInfo.Version})";
                    if (!string.IsNullOrEmpty(healthInfo.Environment))
                        statusMessage += $" [{healthInfo.Environment}]";
                    
                    SetStatus(statusMessage);
                    await LoggingService.LogInfoAsync($"✅ Connexion API réussie - {statusMessage}");
                    
                    // 4. ACTIVER LES FONCTIONNALITÉS
                    EnableConnectionButtons(true);
                    
                    // 5. CHARGER LES REPOSITORIES
                    await LoadRepositories();
                }
                else
                {
                    SetConnectionStatus("❌ Échec");
                    ApiVersionText.Text = "";
                    SetStatus("API connection failed");
                    _repositories.Clear();
                    await LoggingService.LogErrorAsync("❌ Échec de connexion à l'API TextLab");
                    
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
                await LoggingService.LogErrorAsync($"❌ Erreur lors de la connexion: {ex.Message}");
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
                // 🔐 Utiliser l'endpoint authentifié (pas "public")
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
                    SetStatus($"Repository ajouté: {repo.DisplayName} (Type: {repo.TypeDisplay})");
                }
                
                SetStatus($"✅ {repositories.Count} repository(s) loaded successfully - Ctrl+N for new document");
                RepositoryInfoText.Text = $"{repositories.Count} repository(s) available";
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
            
            // Mettre à jour les boutons de la barre d'outils
            UpdateToolbarButtons();
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
                    
                    // 🌳 CONSTRUCTION ARBRE BASÉE SUR LES CHEMINS GIT (pas les catégories !)
                    await LoggingService.LogInfoAsync($"🌲 Construction arbre Git pour {documents.Count} documents");
                    
                    // Construire l'arbre hiérarchique basé sur les chemins Git
                    foreach (var doc in documents.OrderBy(d => d.GitPath ?? d.Title))
                    {
                        await LoggingService.LogDebugAsync($"📄 Traitement document: {doc.Title} (GitPath: {doc.GitPath})");
                        
                        // Nettoyer le chemin Git (enlever le préfixe documents/ s'il existe)
                        var gitPath = doc.GitPath ?? "";
                        if (gitPath.StartsWith("documents/"))
                        {
                            gitPath = gitPath.Substring("documents/".Length);
                        }
                        
                        // Séparer le chemin en segments (dossiers)
                        var pathSegments = gitPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        
                        // Naviguer/créer l'arbre hiérarchique
                        var currentParent = repoNode;
                        
                        // Traiter tous les segments SAUF le dernier (qui est le fichier)
                        for (int i = 0; i < pathSegments.Length - 1; i++)
                        {
                            var folderName = pathSegments[i];
                            
                            // Chercher si le dossier existe déjà
                            var existingFolder = currentParent.Children.FirstOrDefault(
                                child => child.Type == "folder" && child.Name == folderName);
                            
                            if (existingFolder == null)
                            {
                                // Créer le nouveau dossier
                                existingFolder = new DocumentTreeItem(
                                    folderName,
                                    "📂",
                                    "",
                                    "folder"
                                );
                                currentParent.Children.Add(existingFolder);
                                await LoggingService.LogDebugAsync($"📂 Dossier créé: {folderName}");
                            }
                            
                            currentParent = existingFolder;
                        }
                        
                        // Ajouter le document final dans le bon dossier parent
                        var docIcon = GetDocumentIcon();
                        var docInfo = $"Modifié: {doc.UpdatedAt:dd/MM/yyyy}";
                        
                        var docNode = new DocumentTreeItem(
                            doc.Title ?? "Sans titre", 
                            docIcon, 
                            docInfo,
                            "document"
                        );
                        docNode.Tag = doc;
                        
                        // Chargement vraiment paresseux : ajouter un placeholder pour tous les documents
                        var placeholderNode = new DocumentTreeItem(
                            "Cliquer pour voir les versions...", 
                            "🔍", 
                            "",
                            "lazy-placeholder"
                        );
                        docNode.Children.Add(placeholderNode);
                        
                        currentParent.Children.Add(docNode);
                    }
                    
                    // Mettre à jour les compteurs des dossiers
                    UpdateFolderCounts(repoNode);
                    

                    
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
                    SetStatus($"✅ {documents.Count} document(s) loaded");
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
                    
                    SetStatus($"❌ No documents found");
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

        /// <summary>
        /// Met à jour les compteurs de documents dans les dossiers de l'arbre Git
        /// </summary>
        private void UpdateFolderCounts(DocumentTreeItem node)
        {
            if (node.Type == "folder")
            {
                // Compter récursivement les documents dans ce dossier
                var totalDocs = CountDocumentsInNode(node);
                node.Info = $"{totalDocs} document(s)";
            }
            
            // Appliquer récursivement à tous les enfants
            foreach (var child in node.Children)
            {
                UpdateFolderCounts(child);
            }
        }

        /// <summary>
        /// Compte le nombre total de documents dans un nœud et ses enfants
        /// </summary>
        private int CountDocumentsInNode(DocumentTreeItem node)
        {
            var count = 0;
            
            foreach (var child in node.Children)
            {
                if (child.Type == "document")
                {
                    count++;
                }
                else if (child.Type == "folder")
                {
                    count += CountDocumentsInNode(child);
                }
            }
            
            return count;
        }

        private async System.Threading.Tasks.Task<DocumentVersions?> LoadDocumentVersionsForTree(DocumentTreeItem docNode, Document document)
        {
            try
            {
                LogDebug($"=== Chargement versions pour: {document.Title} ===");
                
                // 🚀 OPTIMISATION: Vérifier le cache d'abord
                if (_versionsCache.TryGetValue(document.Id, out var cachedVersions))
                {
                    LogDebug($"📦 Versions récupérées depuis le cache pour: {document.Title}");
                    // Reconstruire l'arbre depuis le cache
                    RebuildVersionTreeFromCache(docNode, cachedVersions);
                    return cachedVersions;
                }
                
                // Chargement des versions via l'API (uniquement si pas en cache)
                LogDebug($"🌐 Chargement API versions pour: {document.Title}");
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
                    // 🚀 OPTIMISATION: Mettre en cache avant de construire l'arbre
                    _versionsCache[document.Id] = versionsResult;
                    
                    // Construire l'arbre des versions
                    RebuildVersionTreeFromCache(docNode, versionsResult);
                }
                
                // 🚀 OPTIMISATION: Retourner le résultat pour éviter un second appel
                return versionsResult;
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
                
                // 🚀 OPTIMISATION: Retourner null en cas d'erreur
                return null;
            }
        }

        /// <summary>
        /// 🚀 OPTIMISATION: Reconstruit l'arbre des versions depuis le cache
        /// </summary>
        private void RebuildVersionTreeFromCache(DocumentTreeItem docNode, DocumentVersions versionsResult)
        {
            // Nettoyer les versions existantes
            docNode.Children.Clear();
            
            // Ajouter les versions depuis le cache
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

        private string GetDocumentIcon()
        {
            // Retourner une icône générique pour tous les documents
            return "📄";
        }

        private void DocumentsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Gestion des vues par tags (TreeViewItem avec Document ou ViewGroup)
            if (e.NewValue is TreeViewItem treeItem)
            {
                if (treeItem.Tag is Document document)
                {
                    SetStatus($"Document sélectionné: {document.Title} (Vue par tags) - Double-cliquez pour voir les détails");
                }
                else if (treeItem.Tag is ViewGroup group)
                {
                    SetStatus($"Groupe sélectionné: {group.Name} ({group.DocumentCount} documents)");
                }
                else if (treeItem.Tag is TagHierarchyNode node)
                {
                    SetStatus($"Tag sélectionné: {node.Tag.DisplayName} ({node.DocumentCount} documents) - Type: {node.Tag.Type}");
                }
                else if (treeItem.Tag is TagTreeNode treeNode)
                {
                    SetStatus($"🌳 Tag sélectionné: {treeNode.Name} ({treeNode.DocumentCount}/{treeNode.TotalDescendantsCount}) - Path: {treeNode.Path}");
                }
            }
            // Gestion de la vue normale (DocumentTreeItem)
            else if (e.NewValue is DocumentTreeItem item)
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
            
            // Mettre à jour les boutons de la barre d'outils selon la sélection
            UpdateToolbarButtons();
        }

        private void DocumentsTreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Gestion des vues par tags (TreeViewItem avec Document direct)
            if (DocumentsTreeView.SelectedItem is TreeViewItem treeItem && treeItem.Tag is Document document)
            {
                // Ouvrir le document depuis une vue par tags
                OpenDocumentDetails(document);
                return;
            }
            
            // Gestion de la vue normale (DocumentTreeItem)
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
                
                var detailsWindow = new DocumentDetailsWindow(document, _apiService, _authService);
                detailsWindow.Owner = this;
                
                // 🔔 S'abonner à la notification de mise à jour
                detailsWindow.DocumentUpdated += OnDocumentUpdated;
                
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

        /// <summary>
        /// 🔔 Gestionnaire appelé quand un document est mis à jour dans DocumentDetailsWindow
        /// </summary>
        private async void OnDocumentUpdated(string documentId)
        {
            try
            {
                await LoggingService.LogInfoAsync($"🔔 Document mis à jour reçu: {documentId}");
                
                // 🚀 Invalider le cache des versions pour ce document
                if (_versionsCache.ContainsKey(documentId))
                {
                    _versionsCache.Remove(documentId);
                    await LoggingService.LogInfoAsync($"🗑️ Cache des versions invalidé pour: {documentId}");
                }
                
                // 🔄 Recharger l'arbre des documents pour afficher les nouvelles versions
                await Task.Delay(500); // Petit délai pour laisser le temps à DocumentDetailsWindow de se fermer
                
                await LoggingService.LogInfoAsync($"🔄 Rechargement de l'arbre des documents...");
                await LoadDocuments();
                
                SetStatus($"✅ Arbre mis à jour après modification du document");
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur lors de la mise à jour après notification: {ex.Message}");
            }
        }

        private void OpenDocumentVersionDetails(Document document, DocumentVersion version, string versionSha)
        {
            try
            {
                SetStatus($"Ouverture de la version {version.Version} pour: {document.Title}");
                
                var detailsWindow = new DocumentDetailsWindow(document, _apiService, _authService, version, versionSha);
                detailsWindow.Owner = this;
                
                // 🔔 S'abonner à la notification de mise à jour (même pour les versions spécifiques)
                detailsWindow.DocumentUpdated += OnDocumentUpdated;
                
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
            // 🚀 OPTIMISATION: Vider le cache lors du refresh
            _versionsCache.Clear();
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
                SetStatus("Repositories refreshed");
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

        private async void ClearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Êtes-vous sûr de vouloir vider les logs ?\n\n" +
                    "Une sauvegarde sera automatiquement créée avant le vidage.\n" +
                    "Cette action ne peut pas être annulée.",
                    "Vider les logs", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var success = await LoggingService.ClearLogsAsync();
                    
                    if (success)
                    {
                        MessageBox.Show(
                            "✅ Logs vidés avec succès !\n\n" +
                            "Une sauvegarde a été créée automatiquement.\n" +
                            "Les nouveaux logs commenceront à être enregistrés.",
                            "Logs vidés", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                        
                        SetStatus("Logs cleared successfully - Backup created");
                    }
                    else
                    {
                        MessageBox.Show(
                            "❌ Erreur lors du vidage des logs.\n\n" +
                            "Consultez les logs pour plus de détails.",
                            "Erreur", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du vidage des logs: {ex.Message}", "Erreur", 
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
                    MessageBox.Show("❌ No API connection. Test the connection first.",
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
                var repositoryWindow = new RepositoryManagementWindow(_adminService!, _apiService!)
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

        // ===== MÉTHODES POUR RAFRAÎCHISSEMENT APRÈS ÉDITION =====

        /// <summary>
        /// Rafraîchit la liste des documents du repository actuellement sélectionné
        /// </summary>
        public async Task RefreshDocumentsAsync()
        {
            try
            {
                LogDebug("🔄 RefreshDocumentsAsync appelé depuis DocumentDetailsWindow");
                
                if (_selectedRepository != null)
                {
                    await LoadDocuments();
                    LogDebug($"✅ Documents rafraîchis pour {_selectedRepository.Name}");
                }
                else
                {
                    LogDebug("⚠️ Aucun repository sélectionné pour le rafraîchissement");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur dans RefreshDocumentsAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Sélectionne un document spécifique dans l'arbre après mise à jour
        /// </summary>
        public void SelectDocumentInTree(string documentId)
        {
            try
            {
                LogDebug($"🎯 Tentative de sélection du document: {documentId}");
                
                // Parcourir l'arbre pour trouver le document
                var documentItem = FindDocumentInTree(DocumentsTreeView.Items, documentId);
                
                if (documentItem != null)
                {
                    // Sélectionner l'item dans le TreeView
                    var container = DocumentsTreeView.ItemContainerGenerator.ContainerFromItem(documentItem) as TreeViewItem;
                    if (container != null)
                    {
                        container.IsSelected = true;
                        container.BringIntoView();
                        LogDebug($"✅ Document {documentId} sélectionné dans l'arbre");
                    }
                    else
                    {
                        LogDebug($"⚠️ Container non trouvé pour document {documentId}");
                    }
                }
                else
                {
                    LogDebug($"⚠️ Document {documentId} non trouvé dans l'arbre");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur dans SelectDocumentInTree: {ex.Message}");
            }
        }

        /// <summary>
        /// Trouve un document dans l'arbre par son ID
        /// </summary>
        private DocumentTreeItem? FindDocumentInTree(System.Collections.IEnumerable items, string documentId)
        {
            foreach (DocumentTreeItem item in items)
            {
                // Vérifier si c'est le document recherché
                if (item.Type == "document" && item.Tag is Document doc && doc.Id == documentId)
                {
                    return item;
                }
                
                // Rechercher récursivement dans les enfants
                var found = FindDocumentInTree(item.Children, documentId);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        // ===== ÉVÉNEMENTS DE LA BARRE D'OUTILS =====

        private void EditDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedDocument = GetSelectedDocument();
                if (selectedDocument != null)
                {
                    LogDebug($"✏️ Édition du document: {selectedDocument.Title}");
                    
                    // Ouvrir la fenêtre de détails en mode édition
                    var detailsWindow = new DocumentDetailsWindow(selectedDocument, _apiService, _authService);
                    detailsWindow.Owner = this;
                    detailsWindow.ShowDialog();
                    
                    // Rafraîchir après fermeture
                    _ = LoadDocuments();
                }
                else
                {
                    MessageBox.Show("Please select a document to edit.", "No document selected", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur EditDocumentButton_Click: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ouverture en édition:\n{ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedDocument = GetSelectedDocument();
                if (selectedDocument != null)
                {
                    LogDebug($"🗑️ Demande de suppression du document: {selectedDocument.Title}");
                    
                    var result = MessageBox.Show(
                        $"Voulez-vous vraiment supprimer le document ?\n\n" +
                        $"📄 Titre: {selectedDocument.Title}\n" +
                        $"📁 Repository: {selectedDocument.RepositoryName}\n" +
    
                        $"⚠️ Cette action effectue une suppression logique.\n" +
                        $"Le fichier Git ne sera pas supprimé.",
                        "Confirmer la suppression", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        DeleteDocumentButton.IsEnabled = false;
                        SetStatus($"Suppression du document: {selectedDocument.Title}...");
                        
                        var success = await _apiService.DeleteDocumentAsync(selectedDocument.Id);
                        
                        if (success)
                        {
                            LogDebug($"✅ Document supprimé: {selectedDocument.Title}");
                            SetStatus($"Document '{selectedDocument.Title}' supprimé avec succès");
                            
                            MessageBox.Show($"Document '{selectedDocument.Title}' supprimé avec succès!\n\n" +
                                          $"Le document a été marqué comme supprimé dans la base de données.\n" +
                                          $"L'historique Git reste intact.", 
                                          "Suppression réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Rafraîchir la liste
                            await LoadDocuments();
                        }
                        else
                        {
                            LogDebug($"❌ Échec suppression: {selectedDocument.Title}");
                            SetStatus($"Erreur lors de la suppression de '{selectedDocument.Title}'");
                            MessageBox.Show($"Erreur lors de la suppression du document '{selectedDocument.Title}'.", 
                                          "Erreur de suppression", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        SetStatus("Suppression annulée");
                    }
                }
                else
                {
                    MessageBox.Show("Please select a document to delete.", "No document selected", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur DeleteDocumentButton_Click: {ex.Message}");
                MessageBox.Show($"Erreur lors de la suppression:\n{ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DeleteDocumentButton.IsEnabled = true;
            }
        }

        private async void SyncRepositoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedRepository != null)
                {
                    LogDebug($"🔄 Synchronisation du repository: {_selectedRepository.Name}");
                    SyncRepositoryButton.IsEnabled = false;
                    SetStatus($"Synchronisation de {_selectedRepository.Name}...");
                    
                    var pullResult = await _adminService.PullRepositoryAsync(_selectedRepository.Id);
                    
                    if (pullResult?.Success == true)
                    {
                        LogDebug($"✅ Synchronisation réussie: {_selectedRepository.Name}");
                        SetStatus($"Repository '{_selectedRepository.Name}' synchronisé avec succès");
                        
                        var message = $"Repository '{_selectedRepository.Name}' synchronisé!\n\n" +
                                     $"📥 {pullResult.Changes.CommitsPulled} commits récupérés\n" +
                                     $"📝 {pullResult.Changes.FilesUpdated} fichiers mis à jour";
                        
                        if (pullResult.Changes.Conflicts.Any())
                        {
                            message += $"\n⚠️ {pullResult.Changes.Conflicts.Count} conflits détectés";
                        }
                        
                        MessageBox.Show(message, "Synchronisation réussie", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Rafraîchir les documents
                        await LoadDocuments();
                    }
                    else
                    {
                        LogDebug($"❌ Échec synchronisation: {pullResult?.Error ?? "Erreur inconnue"}");
                        SetStatus($"Erreur lors de la synchronisation de '{_selectedRepository.Name}'");
                        
                        var errorMessage = $"Erreur lors de la synchronisation du repository '{_selectedRepository.Name}'";
                        if (!string.IsNullOrEmpty(pullResult?.Error))
                        {
                            errorMessage += $"\n\nDétail: {pullResult.Error}";
                        }
                        
                        MessageBox.Show(errorMessage, "Erreur de synchronisation", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a repository to sync.", "No repository selected", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Erreur SyncRepositoryButton_Click: {ex.Message}");
                MessageBox.Show($"Erreur lors de la synchronisation:\n{ex.Message}", "Erreur", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Erreur lors de la synchronisation");
            }
            finally
            {
                SyncRepositoryButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Récupère le document actuellement sélectionné dans l'arbre
        /// </summary>
        private Document? GetSelectedDocument()
        {
            if (DocumentsTreeView.SelectedItem is DocumentTreeItem item && 
                item.Type == "document" && 
                item.Tag is Document document)
            {
                return document;
            }
            return null;
        }

        /// <summary>
        /// Met à jour l'état des boutons de la barre d'outils selon la sélection
        /// </summary>
        private void UpdateToolbarButtons()
        {
            var hasRepository = _selectedRepository != null;
            var hasSelectedDocument = GetSelectedDocument() != null;
            
            // Boutons de document
            NewDocumentButton.IsEnabled = hasRepository;
            EditDocumentButton.IsEnabled = hasSelectedDocument;
            DeleteDocumentButton.IsEnabled = hasSelectedDocument;
            
            // Boutons de repository
            SyncRepositoryButton.IsEnabled = hasRepository;
            
            LogDebug($"🔧 Boutons mis à jour - Repository: {hasRepository}, Document: {hasSelectedDocument}");
        }

        private async void ShowTokenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoggingService.LogInfoAsync("🔑 === AFFICHAGE TOKEN DEBUG ===");

                var tokenInfo = new System.Text.StringBuilder();
                tokenInfo.AppendLine("=== DEBUG AUTHENTIFICATION ===\n");

                // Vérifier l'état d'authentification
                bool isAuth = _authService.IsAuthenticated();
                tokenInfo.AppendLine($"🔐 Authentifié : {isAuth}");

                if (isAuth)
                {
                    // Récupérer le token
                    var token = await _authService.GetBearerTokenAsync();
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokenInfo.AppendLine($"🎫 Token présent : OUI ({token.Length} caractères)");
                        tokenInfo.AppendLine($"🎫 Token (premiers 50 chars) : {token.Substring(0, Math.Min(50, token.Length))}...");
                        
                        // Informations utilisateur
                        var userInfo = await _authService.GetCurrentUserAsync();
                        if (userInfo != null)
                        {
                            tokenInfo.AppendLine($"👤 Utilisateur : {userInfo.Username}");
                            tokenInfo.AppendLine($"📧 Email : {userInfo.Email}");
                            tokenInfo.AppendLine($"🏷️ Rôle : {userInfo.Role}");
                        }
                        
                        tokenInfo.AppendLine($"\n=== HEADERS ENVOYÉS À L'API ===");
                        tokenInfo.AppendLine($"X-User-Token: {token.Substring(0, Math.Min(30, token.Length))}...");
                        tokenInfo.AppendLine($"User-Agent: TextLabClient/2.0");
                        
                        tokenInfo.AppendLine($"\n=== URL D'API ===");
                        tokenInfo.AppendLine($"🌐 URL : {ApiUrlTextBox.Text}");
                        
                        tokenInfo.AppendLine($"\n=== POUR TESTER MANUELLEMENT ===");
                        tokenInfo.AppendLine($"curl -H \"X-User-Token: {token}\" \\");
                        tokenInfo.AppendLine($"     \"{ApiUrlTextBox.Text}/api/v1/repositories\"");
                        
                        tokenInfo.AppendLine($"\n=== TOKEN COMPLET ===");
                        tokenInfo.AppendLine($"{token}");
                    }
                    else
                    {
                        tokenInfo.AppendLine("❌ Token présent : NON");
                    }
                }
                else
                {
                    tokenInfo.AppendLine("❌ Pas d'authentification");
                    tokenInfo.AppendLine("ℹ️ Utilisez d'abord le bouton Connecter pour vous authentifier");
                }

                // Afficher dans une nouvelle fenêtre (contenu sélectionnable + bouton Copier)
                var tokenWindow = new Window
                {
                    Title = "🔑 Debug Token - TextLab Client",
                    Width = 900,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var rootGrid = new System.Windows.Controls.Grid();
                rootGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
                rootGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var toolbar = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Margin = new Thickness(10, 10, 10, 5)
                };

                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = tokenInfo.ToString(),
                    FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.Black,
                    Foreground = System.Windows.Media.Brushes.LimeGreen,
                    Padding = new Thickness(10),
                    BorderThickness = new Thickness(0),
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
                };

                var copyAllButton = new System.Windows.Controls.Button
                {
                    Content = "Copier",
                    Padding = new Thickness(12, 6, 12, 6)
                };
                copyAllButton.Click += (s, ev) =>
                {
                    try
                    {
                        var textToCopy = string.IsNullOrEmpty(textBox.SelectedText) ? textBox.Text : textBox.SelectedText;
                        if (!string.IsNullOrEmpty(textToCopy))
                        {
                            Clipboard.SetText(textToCopy);
                            System.Windows.MessageBox.Show("Contenu copié dans le presse-papiers.", "Copie", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Erreur de copie: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                toolbar.Children.Add(copyAllButton);

                System.Windows.Controls.Grid.SetRow(toolbar, 0);
                System.Windows.Controls.Grid.SetRow(textBox, 1);
                rootGrid.Children.Add(toolbar);
                rootGrid.Children.Add(textBox);

                tokenWindow.Content = rootGrid;
                tokenWindow.Show();

                await LoggingService.LogInfoAsync("✅ Fenêtre de debug token affichée");
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur affichage token: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'affichage du token:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Ouvre l'éditeur de tags hiérarchiques
        /// </summary>
        private async void TagEditorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_apiService == null)
                {
                    MessageBox.Show("Vous devez d'abord vous connecter pour accéder aux tags.", 
                                  "Connexion requise", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Vérifier que l'utilisateur est authentifié
                if (!_authService.IsAuthenticated())
                {
                    MessageBox.Show("Vous devez d'abord vous connecter pour accéder aux tags.\n\nCliquez sur 'Connect' pour vous authentifier.", 
                                  "Authentification requise", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Vérifier que l'API est connectée
                if (!_apiService.IsConnected)
                {
                    MessageBox.Show("La connexion à l'API TextLab n'est pas établie.\n\nCliquez sur 'Connect' pour établir la connexion.", 
                                  "Connexion API requise", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await LoggingService.LogInfoAsync("🏷️ Ouverture de l'éditeur de tags");
                
                // Test rapide des endpoints tags avant d'ouvrir l'éditeur
                try
                {
                    await LoggingService.LogInfoAsync("🧪 Test de l'endpoint tags...");
                    var testTags = await _apiService.GetTagsAsync(limit: 1);
                    await LoggingService.LogInfoAsync($"✅ Endpoint tags fonctionnel: {testTags?.Count ?? 0} tag(s) trouvé(s)");
                }
                catch (Exception testEx)
                {
                    await LoggingService.LogErrorAsync($"❌ Endpoint tags non disponible: {testEx.Message}");
                    
                    var result = MessageBox.Show(
                        $"L'API des tags n'est pas encore disponible sur ce serveur TextLab.\n\n" +
                        $"Erreur: {testEx.Message}\n\n" +
                        "Voulez-vous quand même ouvrir l'éditeur de tags pour voir l'interface ?",
                        "API Tags non disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                        
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }
                
                var tagEditor = new TagEditorWindow(_apiService);
                tagEditor.Owner = this;
                tagEditor.ShowDialog();
                
                // Optionnel : rafraîchir les documents après modification des tags
                // await RefreshCurrentView();
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur ouverture éditeur tags: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ouverture de l'éditeur de tags:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region View Management

        /// <summary>
        /// Handler pour le changement de sélection de vue
        /// </summary>
        private async void ViewSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewSelectorComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var viewType = selectedItem.Tag?.ToString() ?? "all";
                await ChangeViewAsync(viewType);
            }
        }

        /// <summary>
        /// Handler pour le bouton de rafraîchissement de vue
        /// </summary>
        private async void RefreshViewButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshCurrentViewAsync();
        }

        /// <summary>
        /// Change la vue active
        /// </summary>
        private async Task ChangeViewAsync(string viewType)
        {
            if (_currentViewType == viewType) return;
            
            _currentViewType = viewType;
            await LoggingService.LogInfoAsync($"📂 Changement de vue: {viewType}");
            
            try
            {
                ViewInfoText.Text = "Chargement...";
                
                switch (viewType)
                {
                    case "all":
                        await LoadAllDocumentsViewAsync();
                        break;
                    case "client":
                        await LoadViewByClientAsync();
                        break;
                    case "technology":
                        await LoadViewByTechnologyAsync();
                        break;
                    case "status":
                        await LoadViewByStatusAsync();
                        break;
                    case "hierarchy":
                        await LoadTagHierarchyViewAsync();
                        break;
                    default:
                        await LoadAllDocumentsViewAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur changement vue {viewType}: {ex.Message}");
                ViewInfoText.Text = "Erreur de chargement";
                MessageBox.Show($"Erreur lors du changement de vue:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Rafraîchit la vue actuelle
        /// </summary>
        private async Task RefreshCurrentViewAsync()
        {
            await ChangeViewAsync(_currentViewType);
        }

        /// <summary>
        /// Charge la vue "Tous les documents" (comportement normal)
        /// </summary>
        private async Task LoadAllDocumentsViewAsync()
        {
            ViewInfoText.Text = "Vue standard: arbre par repositories";
            // Le comportement normal du TreeView des repositories
            await LoadRepositoriesAsync();
        }

        /// <summary>
        /// Charge la vue par client
        /// </summary>
        private async Task LoadViewByClientAsync()
        {
            if (_apiService == null) return;
            
            var viewResponse = await _apiService.GetViewByClientAsync();
            if (viewResponse != null)
            {
                _currentView = ConvertToDocumentView(viewResponse, "client");
                PopulateTreeWithView(_currentView);
                var visibleDocuments = _currentView.Groups.Sum(g => g.DocumentCount);
                ViewInfoText.Text = $"{visibleDocuments} documents groupés par {_currentView.Groups.Count} clients";
            }
            else
            {
                ViewInfoText.Text = "Erreur de chargement";
            }
        }

        /// <summary>
        /// Charge la vue par technologie
        /// </summary>
        private async Task LoadViewByTechnologyAsync()
        {
            if (_apiService == null) return;
            
            var viewResponse = await _apiService.GetViewByTechnologyAsync();
            if (viewResponse != null)
            {
                _currentView = ConvertToDocumentView(viewResponse, "technology");
                PopulateTreeWithView(_currentView);
                var visibleDocuments = _currentView.Groups.Sum(g => g.DocumentCount);
                ViewInfoText.Text = $"{visibleDocuments} documents groupés par {_currentView.Groups.Count} technologies";
            }
            else
            {
                ViewInfoText.Text = "Erreur de chargement";
            }
        }

        /// <summary>
        /// Charge la vue par statut
        /// </summary>
        private async Task LoadViewByStatusAsync()
        {
            if (_apiService == null) return;
            
            var viewResponse = await _apiService.GetViewByStatusAsync();
            if (viewResponse != null)
            {
                _currentView = ConvertToDocumentView(viewResponse, "status");
                PopulateTreeWithView(_currentView);
                var visibleDocuments = _currentView.Groups.Sum(g => g.DocumentCount);
                ViewInfoText.Text = $"{visibleDocuments} documents groupés par {_currentView.Groups.Count} statuts";
            }
            else
            {
                ViewInfoText.Text = "Erreur de chargement";
            }
        }

        /// <summary>
        /// Charge la vue hiérarchique par tags
        /// </summary>
                private async Task LoadTagHierarchyViewAsync()
        {
            await LoggingService.LogInfoAsync("🔍 DEBUT LoadTagHierarchyViewAsync() - NOUVELLE API SERVEUR");
            
            if (_apiService == null)
            {
                await LoggingService.LogErrorAsync("❌ _apiService est null dans LoadTagHierarchyViewAsync");
                return;
            }
            
            // VÉRIFICATION REPOSITORY SÉLECTIONNÉ
            if (_selectedRepository == null)
            {
                ViewInfoText.Text = "⚠️ Sélectionnez d'abord un repository pour voir les tags";
                await LoggingService.LogWarningAsync("⚠️ Aucun repository sélectionné pour la vue hiérarchique");
                DocumentsTreeView.Items.Clear();
                return;
            }
            
            // AFFICHER LE REPOSITORY UTILISÉ
            ViewInfoText.Text = $"🌳 Chargement hiérarchie tags de {_selectedRepository.Name}...";
            await LoggingService.LogInfoAsync($"📁 Vue hiérarchique pour repository: {_selectedRepository.Name} ({_selectedRepository.Id})");
            
            try
            {
                // 🚀 NOUVELLE API PAGINÉE : APPEL ULTRA-EFFICACE EN MODE COMPACT !
                var hierarchy = await _apiService.GetRepositoryTagHierarchyAsync(
                    _selectedRepository.Id, 
                    compact: true,    // Mode compact = pas de documents dans la réponse
                    tagLimit: 100,    // Plus de tags par défaut
                    tagOffset: 0);
                if (hierarchy == null || hierarchy.Hierarchy.Count == 0)
                {
                    ViewInfoText.Text = "Aucune hiérarchie de tags trouvée";
                    await LoggingService.LogWarningAsync("📋 Aucune hiérarchie trouvée");
                    DocumentsTreeView.Items.Clear();
                    return;
                }
                
                // Construire l'arbre depuis la réponse API optimisée
                await PopulateTreeWithServerHierarchy(hierarchy);
                
                ViewInfoText.Text = $"🌳 {_selectedRepository.Name}: {hierarchy.TotalDocuments} documents en hiérarchie ({hierarchy.Hierarchy.Count} types de tags)";
                
                await LoggingService.LogInfoAsync($"✅ Hiérarchie affichée pour {_selectedRepository.Name}: {hierarchy.Hierarchy.Count} types, {hierarchy.TotalDocuments} documents");
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur chargement hiérarchie: {ex.Message}");
                ViewInfoText.Text = "Erreur de chargement";
            }
        }

        /// <summary>
        /// Convertit ViewResponse en DocumentView
        /// </summary>
        private DocumentView ConvertToDocumentView(ViewResponse response, string viewType)
        {
            var view = new DocumentView
            {
                Type = viewType,
                Name = response.ViewName,
                TotalDocuments = response.TotalDocuments
            };

            // Traiter la structure "organization" de l'API
            if (response.Organization != null)
            {
                foreach (var orgKvp in response.Organization)
                {
                    var groupName = orgKvp.Key;
                    
                    // Gérer le cas "Non défini" -> ignorer ou renommer
                    if (groupName == "Non défini")
                        continue;
                    
                    var viewGroup = new ViewGroup
                    {
                        Id = Guid.NewGuid().ToString(), // Générer un ID temporaire
                        Name = groupName,
                        Icon = GetDefaultIcon(viewType),
                        Color = "#0066CC",
                        Documents = new List<Document>()
                    };
                    
                    // Extraire les documents de la structure imbriquée
                    if (orgKvp.Value is Newtonsoft.Json.Linq.JObject jObj)
                    {
                        foreach (var subGroup in jObj)
                        {
                            if (subGroup.Value is Newtonsoft.Json.Linq.JArray docArray)
                            {
                                var documents = docArray.ToObject<List<Document>>();
                                if (documents != null)
                                {
                                    viewGroup.Documents.AddRange(documents);
                                }
                            }
                        }
                    }
                    
                    if (viewGroup.Documents.Count > 0)
                    {
                        view.Groups.Add(viewGroup);
                    }
                }
            }

            return view;
        }

        /// <summary>
        /// Obtient le nom d'affichage d'une vue
        /// </summary>
        private string GetViewDisplayName(string viewType)
        {
            return viewType switch
            {
                "client" => "Par Client",
                "technology" => "Par Technologie", 
                "status" => "Par Statut",
                _ => "Tous les documents"
            };
        }

        /// <summary>
        /// Obtient l'icône par défaut selon le type de vue
        /// </summary>
        private string GetDefaultIcon(string viewType)
        {
            return viewType switch
            {
                "client" => "👔",
                "technology" => "⚙️",
                "status" => "📊",
                _ => "📄"
            };
        }

        /// <summary>
        /// Remplit le TreeView avec une vue structurée
        /// </summary>
        private void PopulateTreeWithView(DocumentView view)
        {
            DocumentsTreeView.Items.Clear();
            
            foreach (var group in view.Groups)
            {
                var groupItem = new TreeViewItem
                {
                    Header = $"{group.Icon} {group.Name} ({group.DocumentCount})",
                    Tag = group,
                    IsExpanded = false
                };

                foreach (var document in group.Documents)
                {
                    var docItem = new TreeViewItem
                    {
                        Header = $"📄 {document.Title}",
                        Tag = document
                    };
                    groupItem.Items.Add(docItem);
                }
                
                DocumentsTreeView.Items.Add(groupItem);
            }
        }

        /// <summary>
        /// Construit l'arbre hiérarchique des tags à partir de la réponse API
        /// </summary>
        private async Task<List<TagHierarchyNode>> BuildTagHierarchyAsync(object hierarchy, List<Tag> allTags)
        {
            var rootNodes = new List<TagHierarchyNode>();
            
            try
            {
                // Convertir la hiérarchie en dictionnaire de tags
                var tagDict = allTags.ToDictionary(t => t.Id, t => t);
                
                // Parser la hiérarchie (structure à déterminer selon l'API réelle)
                await LoggingService.LogInfoAsync($"🔍 Analyse hiérarchie: {hierarchy.GetType()}");
                
                // Pour l'instant, créons une hiérarchie simple basée sur les types de tags
                var tagsByType = allTags.GroupBy(t => t.Type).ToList();
                
                foreach (var typeGroup in tagsByType)
                {
                    if (string.IsNullOrEmpty(typeGroup.Key)) continue;
                    
                    var typeTag = new Tag
                    {
                        Id = $"type_{typeGroup.Key}",
                        Name = GetTypeDisplayName(typeGroup.Key),
                        Type = typeGroup.Key,
                        Icon = GetTypeIcon(typeGroup.Key),
                        Color = "#0066CC"
                    };
                    
                    var typeNode = new TagHierarchyNode
                    {
                        Tag = typeTag
                    };
                    
                    // Ajouter les tags de ce type comme enfants
                    foreach (var tag in typeGroup)
                    {
                        var tagNode = new TagHierarchyNode
                        {
                            Tag = tag,
                            Parent = typeNode
                        };
                        
                        // Récupérer les documents pour ce tag et compter
                        var documents = await GetDocumentsForTagAsync(tag.Id);
                        tagNode.DocumentCount = documents.Count;
                        tagNode.DirectDocumentCount = documents.Count;
                        
                        typeNode.Children.Add(tagNode);
                    }
                    
                    // Mettre à jour le compteur du nœud parent
                    typeNode.DocumentCount = typeNode.Children.Sum(c => c.DocumentCount);
                    
                    rootNodes.Add(typeNode);
                }
                
                await LoggingService.LogInfoAsync($"✅ Hiérarchie construite: {rootNodes.Count} types, {allTags.Count} tags");
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur construction hiérarchie: {ex.Message}");
            }
            
            return rootNodes;
        }
        
        // ❌ SUPPRIMÉ : GetAllDocumentsWithTagsAsync() - Remplacé par l'API serveur optimisée
        
        /// <summary>
        /// Extrait les documents d'une réponse de vue (réutilise la logique de ConvertToDocumentView)
        /// </summary>
        private List<Document> ExtractDocumentsFromViewResponse(ViewResponse response)
        {
            var documents = new List<Document>();
            
            try
            {
                if (response.Organization != null)
                {
                    foreach (var orgKvp in response.Organization)
                    {
                        // Ignorer "Non défini"
                        if (orgKvp.Key == "Non défini") continue;
                        
                        // Extraire les documents de la structure imbriquée
                        if (orgKvp.Value is Newtonsoft.Json.Linq.JObject jObj)
                        {
                            foreach (var innerKvp in jObj)
                            {
                                if (innerKvp.Value is Newtonsoft.Json.Linq.JArray jArray)
                                {
                                    foreach (var item in jArray)
                                    {
                                        try
                                        {
                                            var document = item.ToObject<Document>();
                                            if (document != null)
                                            {
                                                documents.Add(document);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LoggingService.LogErrorAsync($"❌ Erreur désérialisation document: {ex.Message}").Wait();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogErrorAsync($"❌ Erreur extraction documents: {ex.Message}").Wait();
            }
            
            return documents;
        }
        
        // ❌ SUPPRIMÉ : BuildTagHierarchyFromDocuments() - Remplacé par PopulateTreeWithServerHierarchy()
        
        /// <summary>
        /// Construit une hiérarchie simple basée sur les types de tags (version simplifiée)
        /// </summary>
        private async Task<List<TagHierarchyNode>> BuildSimpleTagHierarchyAsync(List<Tag> allTags)
        {
            var rootNodes = new List<TagHierarchyNode>();
            
            try
            {
                await LoggingService.LogInfoAsync($"🔧 Construction hiérarchie simple avec {allTags.Count} tags");
                
                // Grouper par type de tag
                var tagsByType = allTags.GroupBy(t => string.IsNullOrEmpty(t.Type) ? "autres" : t.Type).ToList();
                
                foreach (var typeGroup in tagsByType)
                {
                    var typeTag = new Tag
                    {
                        Id = $"type_{typeGroup.Key}",
                        Name = GetTypeDisplayName(typeGroup.Key),
                        Type = typeGroup.Key,
                        Icon = GetTypeIcon(typeGroup.Key),
                        Color = "#0066CC"
                    };
                    
                    var typeNode = new TagHierarchyNode
                    {
                        Tag = typeTag
                    };
                    
                    // Ajouter les tags de ce type comme enfants
                    foreach (var tag in typeGroup)
                    {
                        var tagNode = new TagHierarchyNode
                        {
                            Tag = tag,
                            Parent = typeNode
                        };
                        
                        // Récupérer les documents pour ce tag et compter
                        var documents = await GetDocumentsForTagAsync(tag.Id);
                        tagNode.DocumentCount = documents.Count;
                        tagNode.DirectDocumentCount = documents.Count;
                        
                        typeNode.Children.Add(tagNode);
                        
                        await LoggingService.LogInfoAsync($"📎 Tag '{tag.DisplayName}': {documents.Count} documents");
                    }
                    
                    // Mettre à jour le compteur du nœud parent
                    typeNode.DocumentCount = typeNode.Children.Sum(c => c.DocumentCount);
                    
                    if (typeNode.DocumentCount > 0) // N'ajouter que les types qui ont des documents
                    {
                        rootNodes.Add(typeNode);
                        await LoggingService.LogInfoAsync($"📁 Type '{typeGroup.Key}': {typeNode.DocumentCount} documents total");
                    }
                }
                
                await LoggingService.LogInfoAsync($"✅ Hiérarchie simple construite: {rootNodes.Count} types");
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur construction hiérarchie simple: {ex.Message}");
            }
            
            return rootNodes;
        }
        
        /// <summary>
        /// Récupère les documents associés à un tag spécifique
        /// </summary>
        private async Task<List<Document>> GetDocumentsForTagAsync(string tagId)
        {
            try
            {
                if (_apiService == null) return new List<Document>();
                
                // Utiliser l'API de recherche par tags
                var searchRequest = new TagSearchRequest
                {
                    AndFilters = new List<TagFilter>
                    {
                        new TagFilter { Values = new List<string> { tagId } }
                    },
                    Limit = 100
                };
                
                var searchResponse = await _apiService.FindDocumentsByTagsAsync(searchRequest);
                return searchResponse?.Documents ?? new List<Document>();
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur récupération documents pour tag {tagId}: {ex.Message}");
                return new List<Document>();
            }
        }
        
        /// <summary>
        /// Popule le TreeView avec la hiérarchie des tags
        /// </summary>
        private void PopulateTreeWithHierarchy(List<TagHierarchyNode> rootNodes)
        {
            DocumentsTreeView.Items.Clear();
            
            foreach (var rootNode in rootNodes)
            {
                var rootItem = CreateTreeItemFromNode(rootNode);
                DocumentsTreeView.Items.Add(rootItem);
            }
        }
        
        /// <summary>
        /// Crée un TreeViewItem à partir d'un nœud de hiérarchie
        /// </summary>
        private TreeViewItem CreateTreeItemFromNode(TagHierarchyNode node)
        {
            var displayName = node.Tag?.DisplayName ?? node.Tag?.Name ?? "Inconnu";
            var icon = node.Tag?.Icon;
            
            // Optimisé : Plus de logs debug inutiles
            
            // Si le displayName commence déjà par une icône emoji, ne pas en ajouter
            var header = "";
            if (!string.IsNullOrEmpty(icon) && !displayName.StartsWith(icon))
            {
                header = $"{icon} {displayName} ({node.DocumentCount})";
            }
            else if (string.IsNullOrEmpty(icon))
            {
                // Pas d'icône définie, utiliser l'icône par défaut du type
                var defaultIcon = GetTypeIcon(node.Tag?.Type ?? "other");
                header = $"{defaultIcon} {displayName} ({node.DocumentCount})";
            }
            else
            {
                // DisplayName contient déjà l'icône ou icône déjà incluse
                header = $"{displayName} ({node.DocumentCount})";
            }
            
            // Optimisé : Affichage direct sans log debug
            
            var item = new TreeViewItem
            {
                Header = header,
                Tag = node,
                IsExpanded = node.IsExpanded
            };
            
            // Ajouter les enfants (autres tags)
            foreach (var child in node.Children)
            {
                var childItem = CreateTreeItemFromNode(child);
                item.Items.Add(childItem);
            }
            
            // ❌ SUPPRIMÉ : Ajout direct des documents - Remplacé par lazy loading dans PopulateTreeWithServerHierarchy
            
            return item;
        }
        
        // ❌ SUPPRIMÉ : GetDocumentsForTag() - Remplacé par lazy loading avec API serveur
        
        /// <summary>
        /// 🌳 RÉVOLUTION : Affiche la VRAIE hiérarchie récursive du serveur !
        /// </summary>
        private async Task PopulateTreeWithServerHierarchy(RepositoryTagHierarchy hierarchy)
        {
            DocumentsTreeView.Items.Clear();
            
            await LoggingService.LogInfoAsync($"🌳 RÉVOLUTION : Construction arbre hiérarchique avec {hierarchy.Hierarchy.Count} nœuds racines, profondeur max {hierarchy.MaxDepth}");
            
            // 🌳 RÉVOLUTION : Plus de groupes par type - VRAIE hiérarchie !
            foreach (var rootNode in hierarchy.Hierarchy)
            {
                var rootItem = CreateTreeViewItemFromNode(rootNode);
                DocumentsTreeView.Items.Add(rootItem);
                
                await LoggingService.LogInfoAsync($"🌳 Nœud racine ajouté: '{rootNode.Name}' (Level {rootNode.Level}, {rootNode.DocumentCount} docs, {rootNode.TotalDescendantsCount} total)");
            }
            
            await LoggingService.LogInfoAsync($"✅ Arbre hiérarchique construit : {hierarchy.TotalTags} tags, {hierarchy.TotalDocuments} documents");
        }

        /// <summary>
        /// 🌳 RÉVOLUTION : Crée un TreeViewItem récursif depuis TagTreeNode
        /// </summary>
        private TreeViewItem CreateTreeViewItemFromNode(TagTreeNode node)
        {
            // 🎨 Icône intelligente
            var icon = !string.IsNullOrEmpty(node.Icon) ? node.Icon : GetDefaultIconForType(node.Type);
            
            // 📊 Compteurs intelligents - afficher descendants si différent
            var countDisplay = node.TotalDescendantsCount > node.DocumentCount 
                ? $"({node.DocumentCount}/{node.TotalDescendantsCount})"
                : $"({node.DocumentCount})";
            
            var treeItem = new TreeViewItem
            {
                Header = $"{icon} {node.Name} {countDisplay}",
                Tag = node, // Important : stocker le nœud complet
                IsExpanded = false // 🚀 Collapsed par défaut pour lazy loading
            };
            
            // 🌳 RÉCURSION : Ajouter tous les enfants
            foreach (var child in node.Children)
            {
                var childItem = CreateTreeViewItemFromNode(child);
                treeItem.Items.Add(childItem);
            }
            
            // 📄 LAZY LOADING INTELLIGENT : Gérer documents + enfants
            if (node.Documents?.Any() == true)
            {
                // Documents déjà chargés
                foreach (var doc in node.Documents)
                {
                    var docItem = new TreeViewItem
                    {
                        Header = $"📄 {doc.Title}",
                        Tag = doc
                    };
                    treeItem.Items.Add(docItem);
                }
            }
            else if (node.DocumentCount > 0)
            {
                // 🔧 FIX : Si le nœud a des enfants, charger immédiatement les documents
                if (node.Children.Any())
                {
                    // Charger automatiquement en arrière-plan pour les nœuds avec enfants
                    LoadNodeDocumentsInBackground(treeItem, node);
                }
                else
                {
                    // Placeholder classique pour nœuds sans enfants
                    var placeholderItem = new TreeViewItem
                    {
                        Header = $"⏳ Chargement de {node.DocumentCount} documents...",
                        Tag = $"placeholder_{node.Id}"
                    };
                    treeItem.Items.Add(placeholderItem);
                }
            }
            
            // 🚀 EVENT : Charger documents à l'expansion
            treeItem.Expanded += TagItem_Expanded;
            
            return treeItem;
        }

        /// <summary>
        /// 🎨 Icône par défaut selon le type de tag
        /// </summary>
        private string GetDefaultIconForType(string type)
        {
            return type.ToLowerInvariant() switch
            {
                "client" => "🏢",
                "technology" => "⚙️",
                "status" => "🔄",

                "priority" => "⭐",
                _ => "🏷️"
            };
        }

        /// <summary>
        /// 🌳 Construit la hiérarchie parent-enfant des tags (MÉTHODE INTELLIGENTE)
        /// </summary>
        private async Task<List<TagHierarchyItem>> BuildTagHierarchy(List<TagWithDocuments> tags)
        {
            var hierarchy = new List<TagHierarchyItem>();
            var tagDictionary = new Dictionary<string, TagHierarchyItem>();
            
            // Créer les items hiérarchiques
            foreach (var tag in tags)
            {
                var item = new TagHierarchyItem
                {
                    Tag = tag,
                    Children = new List<TagHierarchyItem>()
                };
                hierarchy.Add(item);
                tagDictionary[tag.Id] = item;
            }
            
            // 🎯 MÉTHODE 1 : Utiliser ParentId si disponible (le plus fiable)
            foreach (var item in hierarchy)
            {
                if (!string.IsNullOrEmpty(item.Tag.ParentId) && tagDictionary.ContainsKey(item.Tag.ParentId))
                {
                    var parent = tagDictionary[item.Tag.ParentId];
                    item.Parent = parent;
                    parent.Children.Add(item);
                    continue;
                }
                
                // 🎯 MÉTHODE 2 : Utiliser Children si fournis par l'API
                if (item.Tag.Children?.Any() == true)
                {
                    foreach (var childTag in item.Tag.Children)
                    {
                        var existingChild = hierarchy.FirstOrDefault(h => h.Tag.Id == childTag.Id);
                        if (existingChild != null)
                        {
                            existingChild.Parent = item;
                            item.Children.Add(existingChild);
                        }
                    }
                    continue;
                }
            }
            
            // 🎯 MÉTHODE 3 : Fallback - Détection intelligente par noms/path
            foreach (var item in hierarchy.Where(h => h.Parent == null))
            {
                // Si on a un Path, on peut en déduire la hiérarchie
                if (!string.IsNullOrEmpty(item.Tag.Path) && item.Tag.Path.Contains(">"))
                {
                    var pathParts = item.Tag.Path.Split('>').Select(p => p.Trim()).ToList();
                    if (pathParts.Count > 1)
                    {
                        // Chercher le parent dans le path
                        var parentName = pathParts[pathParts.Count - 2]; // Avant-dernier élément
                        var parent = hierarchy.FirstOrDefault(h => 
                            h.Tag.Name.Equals(parentName, StringComparison.OrdinalIgnoreCase));
                        
                        if (parent != null)
                        {
                            item.Parent = parent;
                            parent.Children.Add(item);
                        }
                    }
                }
                // Sinon, détection par inclusion de noms INTELLIGENTE
                else
                {
                    // 🧠 LOGIQUE MÉTIER : Détection spécifique pour nos tags
                    var potentialParents = hierarchy.Where(h => h != item).ToList();
                    
                    foreach (var potentialParent in potentialParents)
                    {
                        var parentName = potentialParent.Tag.Name.ToLowerInvariant();
                        var itemName = item.Tag.Name.ToLowerInvariant();
                        
                        // 🎯 RÈGLES SPÉCIFIQUES basées sur ce qu'on voit dans l'interface :
                        bool isParent = false;
                        
                        // 1. Si tag contient le nom du parent
                        if (itemName.Contains(parentName) && itemName != parentName)
                            isParent = true;
                            
                        // 2. Règles métier spécifiques
                        if (parentName == "text lab" && (itemName == "aitm" || itemName.Contains("textlab")))
                            isParent = true;
                            
                        // 3. Si parent plus court et enfant commence par parent
                        if (itemName.StartsWith(parentName) && itemName.Length > parentName.Length)
                            isParent = true;
                        
                        if (isParent)
                        {
                            item.Parent = potentialParent;
                            potentialParent.Children.Add(item);
                            await LoggingService.LogInfoAsync($"🔗 Relation détectée: '{item.Tag.Name}' est enfant de '{potentialParent.Tag.Name}'");
                            break; // Premier parent trouvé = le bon
                        }
                    }
                }
            }
            
            return hierarchy;
        }

        /// <summary>
        /// 🌳 Crée un TreeViewItem hiérarchique récursif
        /// </summary>
        private TreeViewItem CreateHierarchicalTagItem(TagHierarchyItem tagItem, List<TagHierarchyItem> allItems)
        {
            var tag = tagItem.Tag;
            var tagIcon = !string.IsNullOrEmpty(tag.Icon) ? tag.Icon : "🏷️";
            
            var treeItem = new TreeViewItem
            {
                Header = $"{tagIcon} {tag.Name} ({tag.DocumentCount})",
                Tag = tag, // Pour récupérer les documents
                IsExpanded = false // 🚀 LAZY LOADING : Collapsed par défaut
            };
            
            // 🌳 Ajouter les enfants AVANT les documents
            foreach (var child in tagItem.Children.OrderBy(c => c.Tag.Name))
            {
                var childItem = CreateHierarchicalTagItem(child, allItems);
                treeItem.Items.Add(childItem);
            }
            
            // 📄 Ajouter les documents de ce tag (pas des enfants)
            if (tag.Documents?.Any() == true)
            {
                foreach (var doc in tag.Documents)
                {
                    var docItem = new TreeViewItem
                    {
                        Header = $"📄 {doc.Title}",
                        Tag = doc // Important pour l'ouverture
                    };
                    treeItem.Items.Add(docItem);
                }
            }
            else if (tag.DocumentCount > 0 && !tagItem.Children.Any())
            {
                // Placeholder SEULEMENT si pas d'enfants (sinon confus)
                var placeholderItem = new TreeViewItem
                {
                    Header = $"⏳ Chargement de {tag.DocumentCount} documents...",
                    Tag = $"placeholder_{tag.Id}"
                };
                treeItem.Items.Add(placeholderItem);
            }
            
            // 🚀 EVENT : Charger documents à l'expansion
            treeItem.Expanded += TagItem_Expanded;
            
            return treeItem;
        }

        /// <summary>
        /// 🌳 Structure hiérarchique pour les tags
        /// </summary>
        private class TagHierarchyItem
        {
            public TagWithDocuments Tag { get; set; } = null!;
            public TagHierarchyItem? Parent { get; set; }
            public List<TagHierarchyItem> Children { get; set; } = new();
        }

        /// <summary>
        /// 🚀 LAZY LOADING RÉVOLUTIONNAIRE : Charge documents pour TagTreeNode
        /// </summary>
        private async void TagItem_Expanded(object sender, RoutedEventArgs e)
        {
            // 🌳 RÉVOLUTION : Support TagTreeNode ET legacy TagWithDocuments
            if (sender is TreeViewItem treeItem)
            {
                // Vérifier si c'est un placeholder
                var hasPlaceholder = treeItem.Items.Count == 1 && 
                    treeItem.Items[0] is TreeViewItem placeholder &&
                    placeholder.Tag is string placeholderTag &&
                    placeholderTag.StartsWith("placeholder_");

                if (hasPlaceholder)
                {
                    // 🌳 NOUVEAU : TagTreeNode révolutionnaire
                    if (treeItem.Tag is TagTreeNode node)
                    {
                        await LoadNodeDocumentsAsync(treeItem, node);
                    }
                    // 🔄 LEGACY : TagWithDocuments (compatibility)
                    else if (treeItem.Tag is TagWithDocuments tag)
                    {
                        await LoadTagDocumentsAsync(treeItem, tag);
                    }
                }
            }
        }

        /// <summary>
        /// 🔧 FIX : Charge documents en arrière-plan sans bloquer UI
        /// </summary>
        private async void LoadNodeDocumentsInBackground(TreeViewItem treeItem, TagTreeNode node)
        {
            try
            {
                await LoadNodeDocumentsAsync(treeItem, node);
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur chargement arrière-plan pour '{node.Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// 🚀 RÉVOLUTION : Charge documents pour un TagTreeNode avec nouvelle API
        /// </summary>
        private async Task LoadNodeDocumentsAsync(TreeViewItem treeItem, TagTreeNode node)
        {
            try
            {
                if (_apiService == null || _selectedRepository == null) return;

                // Changer le placeholder en "Chargement..." (sur UI thread)
                Dispatcher.Invoke(() =>
                {
                    var placeholder = treeItem.Items.Cast<TreeViewItem>()
                        .FirstOrDefault(item => item.Tag is string tagStr && tagStr.StartsWith("placeholder_"));
                    
                    if (placeholder != null)
                    {
                        placeholder.Header = "⏳ Chargement en cours...";
                    }
                });

                // ✅ NOUVELLE API : Charger documents avec l'endpoint dédié !
                var documents = await _apiService.GetRepositoryTagDocumentsAsync(_selectedRepository.Id, node.Id, limit: 50) 
                    ?? new List<Document>();
                
                // Mettre à jour le nœud avec les documents chargés
                node.Documents = documents;

                // 🔧 FIX : Mise à jour UI sur le bon thread
                Dispatcher.Invoke(() =>
                {
                    // Chercher et supprimer SEULEMENT les placeholders (pas les enfants tags !)
                    var placeholdersToRemove = treeItem.Items.Cast<TreeViewItem>()
                        .Where(item => item.Tag is string tagStr && tagStr.StartsWith("placeholder_"))
                        .ToList();
                    
                    foreach (var placeholder in placeholdersToRemove)
                    {
                        treeItem.Items.Remove(placeholder);
                    }

                    // Ajouter les vrais documents
                    foreach (var doc in documents)
                    {
                        var docItem = new TreeViewItem
                        {
                            Header = $"📄 {doc.Title}",
                            Tag = doc // Important pour l'ouverture
                        };
                        treeItem.Items.Add(docItem);
                    }

                    // Si aucun document, afficher message
                    if (!documents.Any())
                    {
                        var emptyItem = new TreeViewItem
                        {
                            Header = "📭 Aucun document",
                            IsEnabled = false
                        };
                        treeItem.Items.Add(emptyItem);
                    }
                });

                await LoggingService.LogInfoAsync($"🚀 Lazy loading révolutionnaire: {documents.Count} documents chargés pour tag '{node.Name}' (Path: {node.Path})");
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur lazy loading documents pour tag '{node.Name}': {ex.Message}");
                
                // Afficher erreur dans l'interface
                treeItem.Items.Clear();
                var errorItem = new TreeViewItem
                {
                    Header = "❌ Erreur de chargement",
                    IsEnabled = false
                };
                treeItem.Items.Add(errorItem);
            }
        }

        /// <summary>
        /// 🚀 LAZY LOADING : Charge les documents d'un tag spécifique
        /// </summary>
        private async Task LoadTagDocumentsAsync(TreeViewItem tagItem, TagWithDocuments tag)
        {
            try
            {
                if (_apiService == null || _selectedRepository == null) return;

                // Changer le placeholder en "Chargement..."
                if (tagItem.Items.Count > 0)
                {
                    var placeholder = (TreeViewItem)tagItem.Items[0];
                    placeholder.Header = "⏳ Chargement en cours...";
                }

                // ✅ VRAIE API : Charger documents avec pagination !
                var documents = await _apiService.GetTagDocumentsAsync(tag.Id, limit: 50, offset: 0) ?? new List<Document>();
                
                // Fallback vers données serveur si API échoue
                if (!documents.Any() && tag.Documents?.Any() == true)
                {
                    documents = tag.Documents;
                    await LoggingService.LogInfoAsync($"🔄 Fallback vers données serveur pour tag '{tag.Name}'");
                }

                // Supprimer le placeholder
                tagItem.Items.Clear();

                // Ajouter les vrais documents
                foreach (var doc in documents)
                {
                    var docItem = new TreeViewItem
                    {
                        Header = $"📄 {doc.Title}",
                        Tag = doc // Important pour l'ouverture
                    };
                    tagItem.Items.Add(docItem);
                }

                // Si aucun document, afficher message
                if (!documents.Any())
                {
                    var emptyItem = new TreeViewItem
                    {
                        Header = "📭 Aucun document",
                        IsEnabled = false
                    };
                    tagItem.Items.Add(emptyItem);
                }

                await LoggingService.LogInfoAsync($"🚀 Lazy loading: {documents.Count} documents chargés pour tag '{tag.Name}'");
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur lazy loading documents pour tag '{tag.Name}': {ex.Message}");
                
                // Afficher erreur dans l'interface
                tagItem.Items.Clear();
                var errorItem = new TreeViewItem
                {
                    Header = "❌ Erreur de chargement",
                    IsEnabled = false
                };
                tagItem.Items.Add(errorItem);
            }
        }

        /// <summary>
        /// Obtient le nom d'affichage pour un type de tag
        /// </summary>
        private string GetTypeDisplayName(string type)
        {
            return type switch
            {
                "client" => "👔 Clients",
                "technology" => "⚙️ Technologies",
                "status" => "📊 Statuts",

                "priority" => "⭐ Priorités",
                _ => $"🏷️ {type.ToUpperInvariant()}"
            };
        }
        
        /// <summary>
        /// Obtient l'icône pour un type de tag
        /// </summary>
        private string GetTypeIcon(string type)
        {
            return type switch
            {
                "client" => "👔",
                "technology" => "⚙️",
                "status" => "📊",

                "priority" => "⭐",
                _ => "🏷️"
            };
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }
    }
} 