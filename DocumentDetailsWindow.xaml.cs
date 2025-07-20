#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TextLabClient.Models;
using TextLabClient.Services;
using System.Windows.Media; // Added for Brushes
using System.Net.Http; // Added for HttpRequestException

namespace TextLabClient
{
    public partial class DocumentDetailsWindow : Window
    {
        private readonly TextLabApiService _apiService;
        private Document _document;
        private DocumentContent? _documentContent;
        private DocumentVersions? _documentVersions;
        
        // Nouveaux champs pour la gestion des versions spécifiques
        private readonly DocumentVersion? _specificVersion;
        private readonly string? _specificVersionSha;
        private readonly bool _isViewingSpecificVersion;

        // Variables pour le mode édition - SUPPRIMÉES car interface d'édition supprimée
        // private bool _isEditMode = false;
        // private string _originalTitle = "";
        // private string _originalContent = "";
        // private const string DEFAULT_AUTHOR = "TextLab Client";

        // MÉTHODES D'ÉDITION SUPPRIMÉES - Interface d'édition retirée du XAML
        // Les boutons SaveButton, EditModeButtons, TitleEditPanel, etc. n'existent plus

        // Constructeur original pour la version actuelle
        public DocumentDetailsWindow(Document document, TextLabApiService apiService)
        {
            InitializeComponent();
            _document = document;
            _apiService = apiService;
            _specificVersion = null;
            _specificVersionSha = null;
            _isViewingSpecificVersion = false;
            
            // Initialiser l'affichage avec les informations de base
            InitializeDocumentInfo();
            
            // Charger les détails complets
            _ = LoadDocumentDetailsAsync();
        }

        // Nouveau constructeur pour une version spécifique
        public DocumentDetailsWindow(Document document, TextLabApiService apiService, DocumentVersion specificVersion, string versionSha)
        {
            InitializeComponent();
            _document = document;
            _apiService = apiService;
            _specificVersion = specificVersion;
            _specificVersionSha = versionSha;
            _isViewingSpecificVersion = true;
            
            // Initialiser l'affichage avec les informations de base
            InitializeDocumentInfo();
            
            // Charger les détails complets
            _ = LoadDocumentDetailsAsync();
        }

        private void InitializeDocumentInfo()
        {
            // Informations de base depuis le document fourni
            if (_isViewingSpecificVersion && _specificVersion != null)
            {
                // Affichage pour une version spécifique
                DocumentTitleText.Text = $"{_document.Title ?? "Document sans titre"} [{_specificVersion.Version}]";
                DocumentPathText.Text = _document.GitPath ?? "";
                
                // Métadonnées avec informations de version
                DocumentIdText.Text = _document.Id;
                DocumentTitleDetailText.Text = $"{_document.Title ?? "Sans titre"} (Version {_specificVersion.Version})";
                DocumentCategoryText.Text = !string.IsNullOrEmpty(_document.CategoryDisplay) 
                    ? _document.CategoryDisplay 
                    : (_document.Category ?? "Non catégorisé");
                DocumentRepositoryText.Text = !string.IsNullOrEmpty(_document.RepositoryName) 
                    ? _document.RepositoryName 
                    : _document.RepositoryId;
                DocumentGitPathText.Text = _document.GitPath ?? "";
                
                // Informations de la version spécifique
                DocumentVersionText.Text = !string.IsNullOrEmpty(_specificVersion.CommitSha) 
                    ? _specificVersion.CommitSha.Substring(0, Math.Min(8, _specificVersion.CommitSha.Length))
                    : _specificVersion.Version;
                    
                DocumentUpdatedText.Text = _specificVersion.Date.ToString("dd/MM/yyyy HH:mm:ss");
                
                // Titre de fenêtre avec indication de version
                this.Title = $"Détails du Document - {_document.Title} [{_specificVersion.Version}]";
                
                Console.WriteLine($"=== DEBUG VERSION SPÉCIFIQUE ===");
                Console.WriteLine($"Document: {_document.Title}");
                Console.WriteLine($"Version: {_specificVersion.Version}");
                Console.WriteLine($"SHA: {_specificVersion.CommitSha}");
                Console.WriteLine($"Auteur: {_specificVersion.Author}");
                Console.WriteLine($"Date: {_specificVersion.Date}");
                Console.WriteLine($"================================");
            }
            else
            {
                // Affichage normal pour la version actuelle
                DocumentTitleText.Text = _document.Title ?? "Document sans titre";
                DocumentPathText.Text = _document.GitPath ?? "";
                
                // Métadonnées
                DocumentIdText.Text = _document.Id;
                DocumentTitleDetailText.Text = _document.Title ?? "Sans titre";
                DocumentCategoryText.Text = !string.IsNullOrEmpty(_document.CategoryDisplay) 
                    ? _document.CategoryDisplay 
                    : (_document.Category ?? "Non catégorisé");
                DocumentRepositoryText.Text = !string.IsNullOrEmpty(_document.RepositoryName) 
                    ? _document.RepositoryName 
                    : _document.RepositoryId;
                DocumentGitPathText.Text = _document.GitPath ?? "";
                DocumentVersionText.Text = !string.IsNullOrEmpty(_document.CurrentCommitSha) 
                    ? _document.CurrentCommitSha.Substring(0, Math.Min(8, _document.CurrentCommitSha.Length))
                    : (_document.Version ?? "N/A");
                
                DocumentUpdatedText.Text = _document.UpdatedAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Date inconnue";
                
                // Debug complet de l'objet document
                Console.WriteLine($"=== DEBUG DOCUMENT ===");
                Console.WriteLine($"ID: {_document.Id}");
                Console.WriteLine($"Title: {_document.Title}");
                Console.WriteLine($"FileSizeBytes: {_document.FileSizeBytes} (type: {_document.FileSizeBytes.GetType()})");
                Console.WriteLine($"RepositoryName: '{_document.RepositoryName}'");
                Console.WriteLine($"GitPath: '{_document.GitPath}'");
                Console.WriteLine($"CurrentCommitSha: '{_document.CurrentCommitSha}'");
                Console.WriteLine($"==================");
            }
            
            DocumentSizeText.Text = _document.FileSizeBytes > 0 
                ? $"{_document.FileSizeBytes:N0} octets" 
                : $"Taille inconnue (valeur: {_document.FileSizeBytes})";
            
            SetStatus("Chargement des détails...");
        }

        private async System.Threading.Tasks.Task LoadDocumentDetailsAsync()
        {
            try
            {
                SetStatus("Chargement des détails complets...");
                
                // Recharger les données du document depuis l'API pour avoir les dernières informations
                if (!_isViewingSpecificVersion)
                {
                    await LoggingService.LogDebugAsync($"🔄 Rechargement du document {_document.Id} depuis l'API");
                    
                    var freshDocument = await _apiService.GetDocumentAsync(_document.Id);
                    if (freshDocument != null)
                    {
                        // Mettre à jour l'objet document avec les données fraîches
                        _document = freshDocument;
                        await LoggingService.LogDebugAsync($"✅ Document rechargé: {_document.Title}, commit: {_document.CurrentCommitSha}");
                        
                        // Réinitialiser l'affichage avec les nouvelles données
                        InitializeDocumentInfo();
                    }
                    else
                    {
                        await LoggingService.LogDebugAsync($"⚠️ Impossible de recharger le document {_document.Id}");
                    }
                }
                
                // Charger le contenu
                await LoadDocumentContent();
                
                // Charger les versions
                await LoadDocumentVersions();
                
                SetStatus("Détails chargés avec succès");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors du chargement: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des détails:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadDocumentContent()
        {
            try
            {
                SetStatus("Chargement du contenu...");
                await LoggingService.LogDebugAsync($"🔄 LoadDocumentContent appelé pour document {_document.Id}");
                
                // Choisir la méthode de chargement selon le contexte
                if (_isViewingSpecificVersion && !string.IsNullOrEmpty(_specificVersionSha))
                {
                    await LoggingService.LogDebugAsync($"📋 Chargement contenu version spécifique: {_specificVersionSha}");
                    var doc = await _apiService.GetDocumentWithContentAsync(_document.Id, _specificVersionSha);
                    if (doc != null)
                    {
                        _documentContent = new DocumentContent
                        {
                            Content = doc.Content ?? "",
                            GitPath = doc.GitPath ?? "",
                            Version = _specificVersionSha,
                            LastModified = doc.UpdatedAt ?? DateTime.Now,
                            RepositoryName = doc.RepositoryName ?? "",
                            FileSizeBytes = doc.FileSizeBytes
                        };
                        await LoggingService.LogDebugAsync($"✅ Contenu version spécifique chargé: {doc.Content?.Length ?? 0} caractères");
                    }
                }
                else
                {
                    await LoggingService.LogDebugAsync($"📋 Chargement contenu version actuelle, commit SHA: {_document.CurrentCommitSha}");
                    var doc = await _apiService.GetDocumentWithContentAsync(_document.Id);
                    if (doc != null)
                    {
                        _documentContent = new DocumentContent
                        {
                            Content = doc.Content ?? "",
                            GitPath = doc.GitPath ?? "",
                            Version = doc.CurrentCommitSha ?? "current",
                            LastModified = doc.UpdatedAt ?? DateTime.Now,
                            RepositoryName = doc.RepositoryName ?? "",
                            FileSizeBytes = doc.FileSizeBytes
                        };
                        await LoggingService.LogDebugAsync($"✅ Contenu version actuelle chargé: {doc.Content?.Length ?? 0} caractères, SHA: {doc.CurrentCommitSha}");
                        await LoggingService.LogDebugAsync($"🔍 Premiers 100 caractères: {doc.Content?.Substring(0, Math.Min(100, doc.Content?.Length ?? 0))}");
                    }
                    else
                    {
                        await LoggingService.LogDebugAsync($"❌ Aucun contenu retourné par l'API pour document {_document.Id}");
                    }
                }
                
                if (_documentContent != null)
                {
                    // Afficher le contenu
                    await LoggingService.LogDebugAsync($"📝 Affichage du contenu dans TextBox: {_documentContent.Content?.Length ?? 0} caractères");
                    DocumentContentTextBox.Text = _documentContent.Content;
                    ContentSizeText.Text = $"{_documentContent.FileSizeBytes} octets";
                    
                    // Mettre à jour les informations de fichier SEULEMENT si meilleures que celles existantes
                    Console.WriteLine($"LoadDocumentContent: _documentContent.FileSizeBytes = {_documentContent.FileSizeBytes}");
                    Console.WriteLine($"LoadDocumentContent: _document.FileSizeBytes = {_document.FileSizeBytes}");
                    
                    if (_documentContent.FileSizeBytes > 0 && _document.FileSizeBytes <= 0)
                    {
                        Console.WriteLine("Mise à jour de la taille depuis _documentContent");
                        DocumentSizeText.Text = $"{_documentContent.FileSizeBytes:N0} octets";
                    }
                    else
                    {
                        Console.WriteLine("Conservation de la taille depuis _document (pas d'écrasement)");
                    }
                    // Ne pas écraser la taille si elle était déjà correcte dans _document
                    
                    if (!string.IsNullOrEmpty(_documentContent.RepositoryName))
                    {
                        DocumentRepositoryText.Text = _documentContent.RepositoryName;
                    }
                }
                else
                {
                    // Afficher un message informatif avec les données disponibles
                    var contentInfo = $@"📋 INFORMATIONS DU DOCUMENT

🔸 ID: {_document.Id}
🔸 Titre: {_document.Title ?? "Sans titre"}
🔸 Repository: {_document.RepositoryName ?? _document.RepositoryId}
🔸 Catégorie: {(!string.IsNullOrEmpty(_document.CategoryDisplay) ? _document.CategoryDisplay : _document.Category) ?? "Non catégorisé"}
🔸 Chemin Git: {_document.GitPath ?? "Non spécifié"}
🔸 Taille: {(_document.FileSizeBytes > 0 ? $"{_document.FileSizeBytes:N0} octets" : "Inconnue")}";

                    if (_isViewingSpecificVersion && _specificVersion != null)
                    {
                        contentInfo += $@"

🔸 VERSION SPÉCIFIQUE: {_specificVersion.Version}
🔸 SHA: {_specificVersion.CommitSha}
🔸 Auteur: {_specificVersion.Author}
🔸 Date: {_specificVersion.Date:dd/MM/yyyy HH:mm:ss}
🔸 Message: {_specificVersion.Message}
🔸 Changements: {_specificVersion.ChangesCount}";
                    }
                    else
                    {
                        contentInfo += $@"
🔸 Version: {(!string.IsNullOrEmpty(_document.CurrentCommitSha) ? _document.CurrentCommitSha.Substring(0, Math.Min(8, _document.CurrentCommitSha.Length)) : _document.Version ?? "N/A")}";
                    }

                    contentInfo += $@"
🔸 Créé le: {_document.CreatedAt:dd/MM/yyyy HH:mm:ss}
🔸 Modifié le: {_document.UpdatedAt:dd/MM/yyyy HH:mm:ss}
🔸 Créé par: {_document.CreatedBy ?? "Non spécifié"}
🔸 Visibilité: {(!string.IsNullOrEmpty(_document.VisibilityDisplay) ? _document.VisibilityDisplay : _document.Visibility) ?? "Non spécifiée"}
🔸 Actif: {(_document.IsActive ? "Oui" : "Non")}";

                    // Ajouter l'aperçu du contenu s'il est disponible
                    if (!string.IsNullOrEmpty(_document.ContentPreview))
                    {
                        contentInfo += $@"

📄 APERÇU DU CONTENU
{_document.ContentPreview}";
                    }

                    if (_isViewingSpecificVersion)
                    {
                        contentInfo += @"

⚠️  CONTENU DE VERSION NON DISPONIBLE
L'API ne fournit pas encore l'endpoint pour récupérer le contenu des versions spécifiques.
Vous visualisez les métadonnées de cette version.";
                    }
                    else
                    {
                        contentInfo += @"

⚠️  CONTENU COMPLET NON DISPONIBLE
L'API ne fournit pas encore l'endpoint pour récupérer le contenu complet des documents.
Les endpoints /content et /versions retournent actuellement des erreurs 404.";
                    }

                    contentInfo += @"

📧 Contactez l'administrateur de l'API pour activer ces fonctionnalités.";

                    DocumentContentTextBox.Text = contentInfo;
                    ContentSizeText.Text = "Contenu indisponible";
                }
            }
            catch (Exception ex)
            {
                DocumentContentTextBox.Text = $"❌ Erreur lors du chargement du contenu:\n{ex.Message}";
                ContentSizeText.Text = "Erreur";
            }
        }

        private async System.Threading.Tasks.Task LoadDocumentVersions()
        {
            try
            {
                SetStatus("Chargement de l'historique...");
                
                _documentVersions = await _apiService.GetDocumentVersionsAsync(_document.Id);
                
                if (_documentVersions != null && _documentVersions.Versions.Count > 0)
                {
                    // Afficher les versions dans le DataGrid
                    VersionsDataGrid.ItemsSource = _documentVersions.Versions;
                    VersionCountText.Text = $"{_documentVersions.TotalVersions} version(s)";
                }
                else
                {
                    VersionCountText.Text = "Historique indisponible (API endpoint 404)";
                    
                    // Créer une version factice pour expliquer le problème
                    var dummyVersions = new List<object>
                    {
                        new
                        {
                            Version = "❌ Non disponible",
                            CommitSha = "N/A",
                            Author = "API endpoint manquant",
                            Date = DateTime.Now,
                            Message = "L'endpoint /versions retourne 404 Not Found",
                            ChangesCount = 0
                        }
                    };
                    VersionsDataGrid.ItemsSource = dummyVersions;
                }
            }
            catch (Exception ex)
            {
                VersionCountText.Text = $"Erreur: {ex.Message}";
            }
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDocumentDetailsAsync();
        }

        private void CopyContentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(DocumentContentTextBox.Text))
                {
                    Clipboard.SetText(DocumentContentTextBox.Text);
                    SetStatus("Contenu copié dans le presse-papiers");
                }
                else
                {
                    SetStatus("Aucun contenu à copier");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de la copie: {ex.Message}");
            }
        }

        private async void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Construire l'URL GitHub avec la racine configurable
                if (!string.IsNullOrEmpty(_document.GitPath) && !string.IsNullOrEmpty(_document.RepositoryName))
                {
                    SetStatus("Construction de l'URL GitHub avec racine configurable...");
                    
                    // Créer un objet Repository temporaire pour la construction d'URL
                    var repository = new Repository
                    {
                        Id = _document.RepositoryId,
                        Name = _document.RepositoryName
                    };
                    
                    // Utiliser la nouvelle méthode qui tient compte de la racine configurable
                    var githubUrl = await _apiService.BuildGitHubUrlAsync(repository, _document.GitPath);
                    
                    if (!string.IsNullOrEmpty(githubUrl))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = githubUrl,
                            UseShellExecute = true
                        });
                        
                        SetStatus($"Ouverture GitHub: {_document.RepositoryName}/{_document.GitPath}");
                    }
                    else
                    {
                        SetStatus("Erreur lors de la construction de l'URL GitHub");
                        MessageBox.Show("Impossible de construire l'URL GitHub. Veuillez vérifier la configuration du repository.", 
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    SetStatus("Informations GitHub incomplètes");
                    MessageBox.Show($"Impossible de construire l'URL GitHub.\n\nRepository: {_document.RepositoryName ?? "Non spécifié"}\nChemin Git: {_document.GitPath ?? "Non spécifié"}", 
                                  "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de l'ouverture: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ouverture du navigateur:\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Méthodes pour le mode édition - SUPPRIMÉES - Interface d'édition retirée du XAML
        // private void EditButton_Click(object sender, RoutedEventArgs e)
        // {
        //     try
        //     {
        //         // Sauvegarder les valeurs originales
        //         _originalTitle = _document.Title ?? "";
        //         _originalContent = DocumentContentTextBox.Text ?? "";
                
        //         // Basculer en mode édition
        //         SetEditMode(true);
                
        //         // Mode édition activé (plus de champ titre séparé, édition du contenu seulement)
                
        //         SetStatus("Mode édition activé");
        //     }
        //     catch (Exception ex)
        //     {
        //         SetStatus($"Erreur lors de l'activation du mode édition: {ex.Message}");
        //         MessageBox.Show($"Erreur lors de l'activation du mode édition:\n{ex.Message}", 
        //                       "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        //     }
        // }

        // private async void SaveButton_Click(object sender, RoutedEventArgs e)
        // {
        //     try
        //     {
        //         SaveButton.IsEnabled = false;
        //         SetStatus("Sauvegarde en cours...");

        //         var newTitle = _originalTitle; // Pas d'édition de titre pour l'instant
        //         var newContent = DocumentContentTextBox.Text;

        //         // Vérifications
        //         if (string.IsNullOrEmpty(newTitle))
        //         {
        //             MessageBox.Show("Le titre ne peut pas être vide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        //             SaveButton.IsEnabled = true;
        //             return;
        //         }

        //         if (string.IsNullOrEmpty(newContent))
        //         {
        //             MessageBox.Show("Le contenu ne peut pas être vide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        //             SaveButton.IsEnabled = true;
        //             return;
        //         }

        //         // Vérifier s'il y a eu des modifications
        //         bool titleChanged = newTitle != _originalTitle;
        //         bool contentChanged = newContent != _originalContent;

        //         if (!titleChanged && !contentChanged)
        //         {
        //             MessageBox.Show("Aucune modification détectée.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        //             CancelEditButton_Click(sender, e);
        //             return;
        //         }

        //         // Tenter de mettre à jour via l'API
        //         try 
        //         {
        //             var updatedDocument = await _apiService.UpdateDocumentAsync(
        //                 _document.Id,
        //                 DEFAULT_AUTHOR,
        //                 titleChanged ? newTitle : null,
        //                 contentChanged ? newContent : null
        //             );

        //             if (updatedDocument != null)
        //             {
        //                 // Mettre à jour l'objet document local
        //                 _document = updatedDocument;
                        
        //                 // Sortir du mode édition
        //                 SetEditMode(false);
                        
        //                 // Recharger complètement les détails du document avec les nouvelles données
        //                 await LoadDocumentDetailsAsync();
                        
        //                 // FORCER le rechargement du contenu depuis l'API avec la nouvelle version
        //                 _documentContent = null; // Réinitialiser pour forcer le rechargement
        //                 await LoadDocumentContent();
                        
        //                 // Notifier la fenêtre parent (MainWindow) pour rafraîchir l'arbre et les versions
        //                 if (Owner is MainWindow mainWindow)
        //                 {
        //                     // Rafraîchir l'arbre des documents pour mettre à jour les métadonnées
        //                     await mainWindow.RefreshDocumentsAsync();
                            
        //                     // Re-sélectionner le document modifié dans l'arbre pour qu'il reste actif
        //                     mainWindow.SelectDocumentInTree(_document.Id);
        //                 }
                        
        //                 // Mettre à jour le titre de la fenêtre avec le nouveau titre du document
        //                 DocumentTitleText.Text = _document.Title ?? "Document sans titre";
        //                 Title = $"Détails du Document - {_document.Title}";
                        
        //                 SetStatus($"Document mis à jour avec succès! Nouveau commit: {updatedDocument.CurrentCommitSha}");
        //                 MessageBox.Show($"Document mis à jour avec succès!\n\nNouveau commit Git: {updatedDocument.CurrentCommitSha?.Substring(0, 8)}", 
        //                               "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        //             }
        //             else
        //             {
        //                 throw new Exception("La réponse de l'API est nulle");
        //             }
        //         }
        //         catch (HttpRequestException httpEx) when (httpEx.Message.Contains("InternalServerError") || httpEx.Message.Contains("GitHubAPIService"))
        //         {
        //             // L'endpoint UPDATE n'est pas encore implémenté côté serveur
        //             SetStatus("⚠️ Fonctionnalité d'édition temporairement indisponible");
                    
        //             var result = MessageBox.Show(
        //                 "🔧 Fonctionnalité temporairement indisponible\n\n" +
        //                 "L'édition de documents n'est pas encore fully implémentée côté serveur.\n" +
        //                 "L'équipe technique travaille sur cette fonctionnalité.\n\n" +
        //                 "📋 Voulez-vous copier vos modifications dans le presse-papier?\n" +
        //                 "Vous pourrez les coller manuellement dans GitHub.",
        //                 "Fonctionnalité en développement", 
        //                 MessageBoxButton.YesNo, 
        //                 MessageBoxImage.Information);
                    
        //             if (result == MessageBoxResult.Yes)
        //             {
        //                 // Copier les modifications dans le presse-papier
        //                 var modifications = $"=== MODIFICATIONS DOCUMENT ===\n\n";
        //                 modifications += $"📄 Titre: {newTitle}\n\n";
        //                 modifications += $"📝 Contenu:\n{newContent}\n\n";
        //                 modifications += $"🆔 ID Document: {_document.Id}\n";
        //                 modifications += $"📁 Chemin Git: {_document.GitPath}\n";
        //                 modifications += $"🔗 Repository: {_document.RepositoryName}\n";
                        
        //                 Clipboard.SetText(modifications);
                        
        //                 MessageBox.Show(
        //                     "📋 Modifications copiées dans le presse-papier!\n\n" +
        //                     "Vous pouvez maintenant ouvrir le document sur GitHub\n" +
        //                     "et coller vos modifications manuellement.",
        //                     "Copié", MessageBoxButton.OK, MessageBoxImage.Information);
        //             }
                    
        //             // Rester en mode édition pour permettre d'autres actions
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         SetStatus($"Erreur lors de la sauvegarde: {ex.Message}");
        //         MessageBox.Show($"Erreur lors de la sauvegarde:\n{ex.Message}", 
        //                       "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        //     }
        //     finally
        //     {
        //         SaveButton.IsEnabled = true;
        //     }
        // }

        // private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        // {
        //     try
        //     {
        //         // Restaurer les valeurs originales
        //         DocumentContentTextBox.Text = _originalContent;
                
        //         // Sortir du mode édition
        //         SetEditMode(false);
                
        //         SetStatus("Édition annulée");
        //     }
        //     catch (Exception ex)
        //     {
        //         SetStatus($"Erreur lors de l'annulation: {ex.Message}");
        //     }
        // }

        // private void SetEditMode(bool editMode)
        // {
        //     _isEditMode = editMode;
            
        //     if (editMode)
        //     {
        //         // Mode édition
        //         ReadModeButtons.Visibility = Visibility.Collapsed;
        //         EditModeButtons.Visibility = Visibility.Visible;
        //         TitleEditPanel.Visibility = Visibility.Visible;
        //         EditModeIndicator.Visibility = Visibility.Visible;
                
        //         DocumentContentTextBox.IsReadOnly = false;
        //         DocumentContentTextBox.Background = Brushes.White;
        //         DocumentContentTextBox.BorderThickness = new Thickness(1);
        //         DocumentContentTextBox.BorderBrush = Brushes.LightGray;
        //     }
        //     else
        //     {
        //         // Mode lecture
        //         ReadModeButtons.Visibility = Visibility.Visible;
        //         EditModeButtons.Visibility = Visibility.Collapsed;
        //         TitleEditPanel.Visibility = Visibility.Collapsed;
        //         EditModeIndicator.Visibility = Visibility.Collapsed;
                
        //         DocumentContentTextBox.IsReadOnly = true;
        //         DocumentContentTextBox.Background = Brushes.Transparent;
        //         DocumentContentTextBox.BorderThickness = new Thickness(0);
        //     }
        // }

        // ===== GESTION DES VERSIONS =====

        private void VersionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedVersion = VersionsDataGrid.SelectedItem as DocumentVersion;
                bool hasSelection = selectedVersion != null;
                
                // Activer/désactiver les boutons selon la sélection
                ViewVersionButton.IsEnabled = hasSelection;
                RestoreVersionButton.IsEnabled = hasSelection && !_isViewingSpecificVersion;
                CompareVersionButton.IsEnabled = hasSelection;
                CopyVersionButton.IsEnabled = hasSelection;
                
                // Afficher les informations de la version sélectionnée
                if (hasSelection && selectedVersion != null)
                {
                    SelectedVersionInfo.Visibility = Visibility.Visible;
                    SelectedVersionText.Text = selectedVersion.Version;
                    SelectedVersionAuthorText.Text = selectedVersion.Author ?? "Inconnu";
                    SelectedVersionMessageText.Text = selectedVersion.Message ?? "Aucun message";
                }
                else
                {
                    SelectedVersionInfo.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de la sélection: {ex.Message}");
            }
        }

        private async void ViewVersionButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewSelectedVersion();
        }

        private async void ViewVersionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await ViewSelectedVersion();
        }

        private async Task ViewSelectedVersion()
        {
            try
            {
                var selectedVersion = VersionsDataGrid.SelectedItem as DocumentVersion;
                if (selectedVersion == null)
                {
                    MessageBox.Show("Veuillez sélectionner une version à visualiser.", 
                                   "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SetStatus("Ouverture de la version...");
                
                // Ouvrir une nouvelle fenêtre avec cette version spécifique
                var versionWindow = new DocumentDetailsWindow(_document, _apiService, selectedVersion, selectedVersion.CommitSha ?? "");
                versionWindow.Show();
                
                SetStatus($"Version {selectedVersion.Version} ouverte dans une nouvelle fenêtre");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de l'ouverture de la version: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ouverture de la version:\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RestoreVersionButton_Click(object sender, RoutedEventArgs e)
        {
            await RestoreSelectedVersion();
        }

        private async void RestoreVersionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await RestoreSelectedVersion();
        }

        private async Task RestoreSelectedVersion()
        {
            try
            {
                var selectedVersion = VersionsDataGrid.SelectedItem as DocumentVersion;
                if (selectedVersion == null)
                {
                    MessageBox.Show("Veuillez sélectionner une version à restaurer.", 
                                   "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Confirmation de restauration
                var result = MessageBox.Show(
                    $"⏮️ Confirmer la restauration\n\n" +
                    $"Version: {selectedVersion.Version}\n" +
                    $"Auteur: {selectedVersion.Author}\n" +
                    $"Date: {selectedVersion.Date:dd/MM/yyyy HH:mm}\n" +
                    $"Message: {selectedVersion.Message}\n\n" +
                    $"Cette action créera une nouvelle version avec le contenu de la version sélectionnée.\n" +
                    $"L'historique sera préservé.\n\n" +
                    $"Continuer?",
                    "Confirmer la restauration", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                SetStatus("Restauration en cours...");
                RestoreVersionButton.IsEnabled = false;

                // Effectuer la restauration
                var commitSha = selectedVersion.CommitSha ?? selectedVersion.Version;
                var restoreResult = await _apiService.RestoreDocumentVersionAsync(
                    _document.Id, 
                    commitSha, 
                    "TextLab Client User",
                    $"Restauration de la version {selectedVersion.Version}"
                );

                if (restoreResult != null)
                {
                    SetStatus("Version restaurée avec succès!");
                    
                    MessageBox.Show(
                        $"✅ Version restaurée avec succès!\n\n" +
                        $"Une nouvelle version a été créée avec le contenu de la version {selectedVersion.Version}.\n" +
                        $"Le document va maintenant se recharger avec la nouvelle version.",
                        "Restauration réussie", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);

                    // Recharger le document et les versions
                    await LoadDocumentDetailsAsync();
                }
                else
                {
                    throw new Exception("La restauration a échoué - aucune donnée retournée");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de la restauration: {ex.Message}");
                MessageBox.Show($"Erreur lors de la restauration:\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RestoreVersionButton.IsEnabled = true;
            }
        }

        private async void CompareVersionButton_Click(object sender, RoutedEventArgs e)
        {
            await CompareSelectedVersion();
        }

        private async void CompareVersionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await CompareSelectedVersion();
        }

        private async Task CompareSelectedVersion()
        {
            try
            {
                var selectedVersion = VersionsDataGrid.SelectedItem as DocumentVersion;
                if (selectedVersion == null)
                {
                    MessageBox.Show("Veuillez sélectionner une version à comparer.", 
                                   "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SetStatus("Comparaison en cours...");
                CompareVersionButton.IsEnabled = false;

                // Pour l'instant, on affiche les informations de base
                // TODO: Implémenter une vraie comparaison diff si l'API le supporte
                var currentVersion = _document.CurrentCommitSha ?? "HEAD";
                var compareVersion = selectedVersion.CommitSha ?? selectedVersion.Version;

                var message = $"🔍 Comparaison de versions\n\n" +
                             $"Version actuelle: {currentVersion}\n" +
                             $"Version sélectionnée: {selectedVersion.Version}\n" +
                             $"Auteur: {selectedVersion.Author}\n" +
                             $"Date: {selectedVersion.Date:dd/MM/yyyy HH:mm}\n" +
                             $"Message: {selectedVersion.Message}\n\n" +
                             $"Fonctionnalité de comparaison détaillée en développement.\n" +
                             $"Utilisez 'Voir' pour ouvrir la version dans une nouvelle fenêtre.";

                MessageBox.Show(message, "Comparaison de versions", 
                               MessageBoxButton.OK, MessageBoxImage.Information);

                SetStatus("Informations de comparaison affichées");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de la comparaison: {ex.Message}");
                MessageBox.Show($"Erreur lors de la comparaison:\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CompareVersionButton.IsEnabled = true;
            }
        }

        private async void CopyVersionButton_Click(object sender, RoutedEventArgs e)
        {
            await CopySelectedVersionContent();
        }

        private async void CopyVersionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await CopySelectedVersionContent();
        }

        private async Task CopySelectedVersionContent()
        {
            try
            {
                var selectedVersion = VersionsDataGrid.SelectedItem as DocumentVersion;
                if (selectedVersion == null)
                {
                    MessageBox.Show("Veuillez sélectionner une version à copier.", 
                                   "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SetStatus("Récupération du contenu...");
                CopyVersionButton.IsEnabled = false;

                // Récupérer le contenu de cette version
                var versionContent = await _apiService.GetDocumentContentVersionAsync(_document.Id, selectedVersion.CommitSha ?? selectedVersion.Version);
                
                if (versionContent?.Content != null)
                {
                    System.Windows.Clipboard.SetText(versionContent.Content);
                    
                    SetStatus($"Contenu de la version {selectedVersion.Version} copié");
                    MessageBox.Show(
                        $"📋 Contenu copié!\n\n" +
                        $"Le contenu de la version {selectedVersion.Version} a été copié dans le presse-papier.\n" +
                        $"Taille: {versionContent.Content.Length} caractères",
                        "Contenu copié", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception("Impossible de récupérer le contenu de cette version");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de la copie: {ex.Message}");
                MessageBox.Show($"Erreur lors de la copie du contenu:\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CopyVersionButton.IsEnabled = true;
            }
        }

        private async void ViewOnGitHubMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedVersion = VersionsDataGrid.SelectedItem as DocumentVersion;
                if (selectedVersion == null)
                {
                    MessageBox.Show("Veuillez sélectionner une version à voir sur GitHub.", 
                                   "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Construire l'URL GitHub pour cette version spécifique
                var commitSha = selectedVersion.CommitSha;
                if (!string.IsNullOrEmpty(commitSha) && !string.IsNullOrEmpty(_document.GitPath))
                {
                    // Format: https://github.com/owner/repo/blob/commitsha/path
                    var baseUrl = "https://github.com/jfgaudy"; // TODO: Récupérer depuis la config du repository
                    var repoName = _document.RepositoryName ?? "gaudylab"; // TODO: Améliorer la détection
                    var githubUrl = $"{baseUrl}/{repoName}/blob/{commitSha}/{_document.GitPath}";
                    
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = githubUrl,
                        UseShellExecute = true
                    });
                    
                    SetStatus($"Version {selectedVersion.Version} ouverte sur GitHub");
                }
                else
                {
                    MessageBox.Show("Informations insuffisantes pour ouvrir cette version sur GitHub.", 
                                   "Informations manquantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de l'ouverture GitHub: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'ouverture sur GitHub:\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyIdButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_document?.Id))
                {
                    System.Windows.Clipboard.SetText(_document.Id);
                    SetStatus($"ID copié: {_document.Id}");
                    MessageBox.Show($"ID du document copié dans le presse-papiers:\n\n{_document.Id}", 
                                   "ID Copié", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Aucun ID de document disponible à copier.", 
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur lors de la copie: {ex.Message}");
                MessageBox.Show($"Erreur lors de la copie de l'ID:\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 