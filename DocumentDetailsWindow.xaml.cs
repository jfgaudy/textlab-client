#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using TextLabClient.Models;
using TextLabClient.Services;

namespace TextLabClient
{
    public partial class DocumentDetailsWindow : Window
    {
        private readonly TextLabApiService _apiService;
        private readonly Document _document;
        private DocumentContent? _documentContent;
        private DocumentVersions? _documentVersions;
        
        // Nouveaux champs pour la gestion des versions spécifiques
        private readonly DocumentVersion? _specificVersion;
        private readonly string? _specificVersionSha;
        private readonly bool _isViewingSpecificVersion;

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
                
                // Choisir la méthode de chargement selon le contexte
                if (_isViewingSpecificVersion && !string.IsNullOrEmpty(_specificVersionSha))
                {
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
                    }
                }
                else
                {
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
                    }
                }
                
                if (_documentContent != null)
                {
                    // Afficher le contenu
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

        private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Construire l'URL GitHub basée sur les informations du document
                if (!string.IsNullOrEmpty(_document.GitPath) && !string.IsNullOrEmpty(_document.RepositoryName))
                {
                    // Construire l'URL GitHub selon le repository
                    var githubUrl = "";
                    
                    if (_document.RepositoryName.ToLower() == "gaudylab")
                    {
                        githubUrl = $"https://github.com/jfgaudy/gaudylab/blob/main/{_document.GitPath}";
                    }
                    else if (_document.RepositoryName.ToLower().Contains("pac"))
                    {
                        // Adapter selon le repository PAC_Repo
                        githubUrl = $"https://github.com/jfgaudy/PAC_Repo/blob/main/{_document.GitPath}";
                    }
                    else
                    {
                        // Repository générique
                        githubUrl = $"https://github.com/jfgaudy/{_document.RepositoryName}/blob/main/{_document.GitPath}";
                    }
                    
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = githubUrl,
                        UseShellExecute = true
                    });
                    
                    SetStatus($"Ouverture GitHub: {_document.RepositoryName}/{_document.GitPath}");
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