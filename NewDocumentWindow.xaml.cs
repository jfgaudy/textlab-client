#nullable enable
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TextLabClient.Models;
using TextLabClient.Services;

namespace TextLabClient
{
    public partial class NewDocumentWindow : Window
    {
        private readonly TextLabApiService _apiService;
        private List<Repository> _repositories = new List<Repository>();
        private string? _selectedRepositoryId;
        private string? _importedFileContent;

        public bool DocumentCreated { get; private set; } = false;
        public Document? CreatedDocument { get; private set; }

        public NewDocumentWindow(TextLabApiService apiService, string? preSelectedRepositoryId = null)
        {
            try
            {
                InitializeComponent();
                _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
                _selectedRepositoryId = preSelectedRepositoryId;
                
                // Attacher les événements seulement après InitializeComponent
                this.Loaded += NewDocumentWindow_Loaded;
                
                // Les événements pour la mise à jour dynamique seront attachés dans Loaded
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors de l'initialisation de la fenêtre:\n\n{ex.Message}\n\nDétails: {ex.StackTrace}", 
                               "Erreur d'Initialisation", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private async void NewDocumentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Attacher les événements maintenant que les contrôles sont chargés
                if (DocumentTitleTextBox != null)
                    DocumentTitleTextBox.TextChanged += UpdateFinalPath;
                if (CategoryComboBox != null)
                    CategoryComboBox.SelectionChanged += UpdateFinalPath;
                if (FilePathTextBox != null)
                    FilePathTextBox.TextChanged += UpdateFinalPath;
                if (TextInputRadio != null)
                    TextInputRadio.Checked += ContentSource_Changed;
                if (FileInputRadio != null)
                    FileInputRadio.Checked += ContentSource_Changed;
                
                await LoadRepositoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors du chargement de la fenêtre:\n\n{ex.Message}", 
                               "Erreur de Chargement", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadRepositoriesAsync()
        {
            try
            {
                CreateButton.IsEnabled = false;
                CreateButton.Content = "⏳ Chargement...";

                var repositories = await _apiService.GetRepositoriesAsync();
                if (repositories != null)
                {
                    _repositories = repositories;
                    RepositoryComboBox.ItemsSource = _repositories;

                    // Pré-sélectionner le repository si spécifié
                    if (!string.IsNullOrEmpty(_selectedRepositoryId))
                    {
                        var preSelected = _repositories.FirstOrDefault(r => r.Id == _selectedRepositoryId);
                        if (preSelected != null)
                        {
                            RepositoryComboBox.SelectedItem = preSelected;
                        }
                    }
                    
                    // Sinon sélectionner le premier
                    if (RepositoryComboBox.SelectedItem == null && _repositories.Count > 0)
                    {
                        RepositoryComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    MessageBox.Show("❌ Impossible de charger les repositories. Vérifiez la connexion à l'API.",
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors du chargement des repositories:\n{ex.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateButton.IsEnabled = true;
                CreateButton.Content = "✅ Créer Document";
                UpdateFinalPath();
            }
        }

        private void RepositoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RepositoryComboBox.SelectedItem is Repository selectedRepo)
            {
                _selectedRepositoryId = selectedRepo.Id;
            }
            UpdateFinalPath();
        }

        private void GeneratePathButton_Click(object sender, RoutedEventArgs e)
        {
            var title = DocumentTitleTextBox.Text?.Trim();
            // 🔧 CORRECTION: Gestion spéciale pour le premier item "(aucune - à la racine)"
            string? category = null;
            if (CategoryComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Si c'est le premier item (avec TextBlock), considérer comme null
                if (selectedItem.Content is TextBlock)
                {
                    category = null;
                }
                // Sinon récupérer le contenu string
                else if (selectedItem.Content is string categoryContent && !string.IsNullOrWhiteSpace(categoryContent))
                {
                    category = categoryContent;
                }
            }
            else
            {
                // L'utilisateur a tapé quelque chose dans la ComboBox
                category = CategoryComboBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(category))
                {
                    category = null;
                }
            }

            if (!string.IsNullOrEmpty(title))
            {
                // Nettoyer le titre pour en faire un nom de fichier valide
                var fileName = title
                    .Replace(" ", "-")
                    .Replace("'", "")
                    .Replace("\"", "")
                    .Replace("/", "-")
                    .Replace("\\", "-")
                    .Replace(":", "-")
                    .Replace("*", "")
                    .Replace("?", "")
                    .Replace("<", "")
                    .Replace(">", "")
                    .Replace("|", "")
                    .ToLower();

                // 🔧 CORRECTION: Ne pas inclure "documents/" dans git_path (géré par repository config)
                string generatedPath;
                if (!string.IsNullOrEmpty(category))
                {
                    generatedPath = $"{category}/{fileName}.md";
                }
                else
                {
                    generatedPath = $"{fileName}.md";
                }

                FilePathTextBox.Text = generatedPath;
            }
        }

        private void ContentSource_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                // Vérifier que tous les contrôles sont initialisés
                if (TextInputRadio == null || FileInputRadio == null || 
                    ContentTextBox == null || FileImportPanel == null)
                {
                    return; // Les contrôles ne sont pas encore prêts
                }

                if (TextInputRadio.IsChecked == true)
                {
                    ContentTextBox.Visibility = Visibility.Visible;
                    FileImportPanel.Visibility = Visibility.Collapsed;
                }
                else if (FileInputRadio.IsChecked == true)
                {
                    ContentTextBox.Visibility = Visibility.Collapsed;
                    FileImportPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs pendant l'initialisation
                System.Diagnostics.Debug.WriteLine($"Erreur ContentSource_Changed: {ex.Message}");
            }
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionner un fichier à importer",
                Filter = "Fichiers texte|*.md;*.txt;*.yaml;*.yml;*.json;*.xml|" +
                        "Markdown (*.md)|*.md|" +
                        "Fichiers texte (*.txt)|*.txt|" +
                        "YAML (*.yaml;*.yml)|*.yaml;*.yml|" +
                        "JSON (*.json)|*.json|" +
                        "XML (*.xml)|*.xml|" +
                        "Tous les fichiers (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = openFileDialog.FileName;
                    _importedFileContent = File.ReadAllText(filePath);
                    
                    SelectedFileTextBox.Text = Path.GetFileName(filePath);
                    
                    var fileInfo = new FileInfo(filePath);
                    FileInfoTextBlock.Text = $"📄 {fileInfo.Length:N0} octets - Encodage détecté automatiquement";

                    // Proposition automatique du titre basé sur le nom de fichier
                    if (string.IsNullOrEmpty(DocumentTitleTextBox.Text))
                    {
                        var suggestedTitle = Path.GetFileNameWithoutExtension(filePath)
                            .Replace("-", " ")
                            .Replace("_", " ");
                        
                        // Capitaliser la première lettre
                        if (!string.IsNullOrEmpty(suggestedTitle))
                        {
                            suggestedTitle = char.ToUpper(suggestedTitle[0]) + suggestedTitle.Substring(1);
                            DocumentTitleTextBox.Text = suggestedTitle;
                        }
                    }

                    UpdateFinalPath();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Erreur lors de la lecture du fichier:\n{ex.Message}",
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    SelectedFileTextBox.Text = "";
                    FileInfoTextBlock.Text = "";
                    _importedFileContent = null;
                }
            }
        }

        private void UpdateFinalPath(object? sender = null, EventArgs? e = null)
        {
            try
            {
                // Vérifier que tous les contrôles sont initialisés
                if (RepositoryComboBox == null || FilePathTextBox == null || 
                    DocumentTitleTextBox == null || CategoryComboBox == null || FinalPathTextBlock == null)
                {
                    return; // Les contrôles ne sont pas encore prêts
                }

                var repository = RepositoryComboBox.SelectedItem as Repository;
                var repositoryName = repository?.Name ?? "[Sélectionner repository]";
                var filePath = FilePathTextBox.Text?.Trim();

                if (string.IsNullOrEmpty(filePath))
                {
                    var title = DocumentTitleTextBox.Text?.Trim();
                    // 🔧 CORRECTION: Gestion spéciale pour le premier item "(aucune - à la racine)"
                    string? category = null;
                    if (CategoryComboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        // Si c'est le premier item (avec TextBlock), considérer comme null
                        if (selectedItem.Content is TextBlock)
                        {
                            category = null;
                        }
                        // Sinon récupérer le contenu string
                        else if (selectedItem.Content is string categoryContent && !string.IsNullOrWhiteSpace(categoryContent))
                        {
                            category = categoryContent;
                        }
                    }
                    else
                    {
                        // L'utilisateur a tapé quelque chose dans la ComboBox
                        category = CategoryComboBox.Text?.Trim();
                        if (string.IsNullOrWhiteSpace(category))
                        {
                            category = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(title))
                    {
                        var fileName = title
                            .Replace(" ", "-")
                            .Replace("'", "")
                            .Replace("\"", "")
                            .Replace("/", "-")
                            .Replace("\\", "-")
                            .Replace(":", "-")
                            .Replace("*", "")
                            .Replace("?", "")
                            .Replace("<", "")
                            .Replace(">", "")
                            .Replace("|", "")
                            .ToLower();

                        // 🔧 CORRECTION: Ne pas inclure "documents/" dans git_path (géré par repository config)
                        if (!string.IsNullOrEmpty(category))
                        {
                            filePath = $"{category}/{fileName}.md";
                        }
                        else
                        {
                            filePath = $"{fileName}.md";
                        }
                    }
                    else
                    {
                        filePath = "[nom-document].md";
                    }
                }

                FinalPathTextBlock.Text = $"{repositoryName} → {filePath}";
            }
            catch
            {
                FinalPathTextBlock.Text = "Chemin invalide";
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                var title = DocumentTitleTextBox.Text?.Trim();
                if (string.IsNullOrEmpty(title))
                {
                    MessageBox.Show("⚠️ Le nom du document est obligatoire.", 
                                   "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DocumentTitleTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(_selectedRepositoryId))
                {
                    MessageBox.Show("⚠️ Vous devez sélectionner un repository.", 
                                   "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RepositoryComboBox.Focus();
                    return;
                }

                // Récupérer le contenu
                string content;
                if (TextInputRadio.IsChecked == true)
                {
                    content = ContentTextBox.Text ?? "";
                }
                else if (FileInputRadio.IsChecked == true)
                {
                    if (string.IsNullOrEmpty(_importedFileContent))
                    {
                        MessageBox.Show("⚠️ Aucun fichier sélectionné pour l'import.", 
                                       "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        BrowseFileButton.Focus();
                        return;
                    }
                    content = _importedFileContent;
                }
                else
                {
                    content = "";
                }

                if (string.IsNullOrEmpty(content))
                {
                    var result = MessageBox.Show("⚠️ Le document sera créé avec un contenu vide. Continuer ?", 
                                                 "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // Préparation de la création
                CreateButton.IsEnabled = false;
                CreateButton.Content = "⏳ Création...";

                // 🔧 CORRECTION: Gestion spéciale pour le premier item "(aucune - à la racine)"
                string? category = null;
                if (CategoryComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    // Si c'est le premier item (avec TextBlock), considérer comme null
                    if (selectedItem.Content is TextBlock)
                    {
                        category = null;
                    }
                    // Sinon récupérer le contenu string
                    else if (selectedItem.Content is string categoryContent && !string.IsNullOrWhiteSpace(categoryContent))
                    {
                        category = categoryContent;
                    }
                }
                else
                {
                    // L'utilisateur a tapé quelque chose dans la ComboBox
                    category = CategoryComboBox.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(category))
                    {
                        category = null;
                    }
                }

                var filePath = FilePathTextBox.Text?.Trim();
                if (string.IsNullOrEmpty(filePath))
                {
                    // Auto-génération si vide
                    var fileName = title
                        .Replace(" ", "-")
                        .Replace("'", "")
                        .Replace("\"", "")
                        .Replace("/", "-")
                        .Replace("\\", "-")
                        .Replace(":", "-")
                        .Replace("*", "")
                        .Replace("?", "")
                        .Replace("<", "")
                        .Replace(">", "")
                        .Replace("|", "")
                        .ToLower();

                    // 🔧 CORRECTION: Ne pas inclure "documents/" dans git_path (géré par repository config)
                    if (!string.IsNullOrEmpty(category))
                    {
                        filePath = $"{category}/{fileName}.md";
                    }
                    else
                    {
                        filePath = $"{fileName}.md";
                    }
                }

                // Créer le document
                // Créer le document avec la nouvelle signature de l'API
                var createdDoc = await _apiService.CreateDocumentAsync(
                    title: title,
                    content: content,
                    repositoryId: _selectedRepositoryId,
                    category: category,
                    visibility: "private",
                    createdBy: null
                );

                if (createdDoc != null)
                {
                    CreatedDocument = createdDoc;
                    DocumentCreated = true;

                    MessageBox.Show($"✅ Document '{title}' créé avec succès !\n\n" +
                                   $"📁 Repository: {RepositoryComboBox.Text}\n" +
                                   $"📄 Chemin: {filePath}\n" +
                                   $"🆔 ID: {createdDoc.Id}",
                                   "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("❌ Échec de la création du document.\n\n" +
                                   "Vérifiez:\n" +
                                   "• La connexion à l'API\n" +
                                   "• Les permissions sur le repository\n" +
                                   "• Que le chemin ne contient pas de caractères invalides\n" +
                                   "• Consultez les logs de débogage pour plus de détails",
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors de la création du document:\n\n{ex.Message}",
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateButton.IsEnabled = true;
                CreateButton.Content = "✅ Créer Document";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 