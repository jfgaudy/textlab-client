#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TextLabClient.Models;
using TextLabClient.Services;

namespace TextLabClient
{
    public partial class TagEditorWindow : Window
    {
        private readonly TextLabApiService _apiService;
        private readonly TagHierarchyManager _hierarchyManager = new();
        private readonly ObservableCollection<TagHierarchyNode> _filteredNodes = new();
        private TagHierarchyNode? _selectedNode;
        private Tag? _currentEditingTag;
        private bool _isEditMode = false;
        private bool _isLoading = false;
        
        // Fonctionnalité de sélection pour associer des tags
        private readonly Action<List<Tag>>? _onTagsSelected;
        private readonly List<Tag> _selectedTags = new();
        private bool _isSelectionMode = false;

        public TagEditorWindow(TextLabApiService apiService)
        {
            try
            {
                InitializeComponent();
                _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
                
                TagsTreeView.ItemsSource = _filteredNodes;
                
                // Configuration initiale
                SetupUI();
                _ = LoadTagsHierarchyAsync();
                
                _ = LoggingService.LogInfoAsync("✅ TagEditorWindow initialisé avec succès");
            }
            catch (Exception ex)
            {
                _ = LoggingService.LogErrorAsync($"❌ Erreur dans constructeur TagEditorWindow: {ex.Message}");
                _ = LoggingService.LogErrorAsync($"❌ StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Constructeur pour le mode sélection de tags
        /// </summary>
        public TagEditorWindow(TextLabApiService apiService, Action<List<Tag>> onTagsSelected) : this(apiService)
        {
            _onTagsSelected = onTagsSelected;
            _isSelectionMode = true;
            
            // Modifier l'interface pour le mode sélection
            Title = "🏷️ Select Tags to Associate";
            SetupSelectionMode();
        }

        #region UI Setup and Navigation

        private void SetupUI()
        {
            try
            {
                _ = LoggingService.LogInfoAsync("🔧 Début SetupUI");
                
                // Vérifications des références
                if (DetailTagTypeComboBox == null)
                    throw new InvalidOperationException("DetailTagTypeComboBox est null");
                if (SearchTextBox == null)
                    throw new InvalidOperationException("SearchTextBox est null");
                
                // Configurer les combobox
                DetailTagTypeComboBox.SelectedIndex = 5; // custom par défaut
                
                // Mode initial : pas d'édition
                ClearEditor();
                UpdateButtonStates();
            
            // Placeholder pour la recherche
            SearchTextBox.GotFocus += (s, e) =>
            {
                if (SearchTextBox.Text == SearchTextBox.Tag?.ToString())
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = Brushes.Black;
                }
            };
            
            SearchTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    SearchTextBox.Text = SearchTextBox.Tag?.ToString() ?? "";
                    SearchTextBox.Foreground = Brushes.Gray;
                }
            };
            
            // Définir le placeholder initial
            SearchTextBox.Text = SearchTextBox.Tag?.ToString() ?? "Rechercher tags...";
            SearchTextBox.Foreground = Brushes.Gray;
            
            _ = LoggingService.LogInfoAsync("✅ SetupUI terminé avec succès");
            }
            catch (Exception ex)
            {
                _ = LoggingService.LogErrorAsync($"❌ Erreur dans SetupUI: {ex.Message}");
                _ = LoggingService.LogErrorAsync($"❌ StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void UpdateButtonStates()
        {
            var hasSelection = _selectedNode != null;
            var canEdit = hasSelection && !(_selectedNode?.Tag.IsSystem ?? false);
            
            EditTagButton.IsEnabled = canEdit;
            DeleteTagButton.IsEnabled = canEdit;
            CreateChildButton.IsEnabled = hasSelection;
        }

        private void ClearEditor()
        {
            _currentEditingTag = null;
            _isEditMode = false;
            
            TagIdTextBox.Text = "";
            TagNameTextBox.Text = "";
            TagSlugTextBox.Text = "";
            DetailTagTypeComboBox.SelectedIndex = 5;
            ParentTagComboBox.SelectedIndex = -1;
            ColorTextBox.Text = "#6C757D";
            IconTextBox.Text = "";
            DescriptionTextBox.Text = "";
            IsPublicCheckBox.IsChecked = true;
            IsSystemCheckBox.IsChecked = false;
            IsActiveCheckBox.IsChecked = true;
            
            TagIdPanel.Visibility = Visibility.Collapsed;
            DocumentInfoPanel.Visibility = Visibility.Collapsed;
            
            UpdateColorPreview();
            UpdateIconPreview();
        }

        #endregion

        #region Data Loading

        private async Task LoadTagsHierarchyAsync()
        {
            if (_isLoading) return;
            
            try
            {
                _isLoading = true;
                TagsSummaryText.Text = "Chargement des tags...";
                
                // Charger tous les tags
                var tags = await _apiService.GetTagsAsync();
                if (tags == null)
                {
                    TagsSummaryText.Text = "❌ API tags non disponible - Mode démo activé";
                    await LoggingService.LogWarningAsync("⚠️ API tags non disponible, chargement de tags de démonstration");
                    
                    // Charger des tags de démonstration
                    tags = CreateDemoTags();
                }
                
                // Construire la hiérarchie
                _hierarchyManager.BuildHierarchy(tags);
                
                // Mettre à jour l'affichage
                RefreshFilteredNodes();
                await LoadParentTagOptionsAsync();
                
                // Mettre à jour le résumé
                TagsSummaryText.Text = $"📊 {tags.Count} tags • {_hierarchyManager.RootNodes.Count} racines";
                
                await LoggingService.LogInfoAsync($"✅ {tags.Count} tags chargés avec succès");
            }
            catch (Exception ex)
            {
                TagsSummaryText.Text = $"❌ Erreur: {ex.Message}";
                await LoggingService.LogErrorAsync($"❌ Erreur LoadTagsHierarchyAsync: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadParentTagOptionsAsync()
        {
            try
            {
                var options = new List<TagHierarchyNode>();
                
                // Ajouter option "Aucun parent" 
                var noneOption = new TagHierarchyNode(new Tag { Id = "", Name = "Aucun parent (tag racine)" });
                options.Add(noneOption);
                
                // Ajouter tous les nœuds sauf celui en cours d'édition
                var allNodes = _hierarchyManager.NodesByTagId.Values;
                if (_currentEditingTag != null)
                {
                    allNodes = allNodes.Where(n => n.Tag.Id != _currentEditingTag.Id);
                }
                
                options.AddRange(allNodes.OrderBy(n => n.FullPath));
                
                ParentTagComboBox.ItemsSource = options;
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur LoadParentTagOptionsAsync: {ex.Message}");
            }
        }

        private void RefreshFilteredNodes()
        {
            _filteredNodes.Clear();
            
            var searchText = SearchTextBox.Text?.ToLower() ?? "";
            if (searchText == SearchTextBox.Tag?.ToString()?.ToLower())
                searchText = "";
            
            var rootNodes = _hierarchyManager.RootNodes;
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Pas de filtre : afficher tout
                foreach (var node in rootNodes)
                {
                    _filteredNodes.Add(node);
                }
            }
            else
            {
                // Filtre : afficher les nœuds correspondants
                foreach (var node in rootNodes)
                {
                    if (NodeMatchesSearch(node, searchText))
                    {
                        _filteredNodes.Add(node);
                    }
                }
            }
        }

        private bool NodeMatchesSearch(TagHierarchyNode node, string searchText)
        {
            // Vérifier le nœud actuel
            if (node.Tag.Name.ToLower().Contains(searchText) ||
                node.Tag.Type.ToLower().Contains(searchText) ||
                (node.Tag.Description?.ToLower().Contains(searchText) ?? false))
            {
                return true;
            }
            
            // Vérifier récursivement les enfants
            return node.Children.Any(child => NodeMatchesSearch(child, searchText));
        }

        #endregion

        #region Tag CRUD Operations

        private async Task<bool> SaveCurrentTagAsync()
        {
            try
            {
                if (!ValidateTagData(out var validationMessage))
                {
                    MessageBox.Show(validationMessage, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                
                var tag = CreateTagFromForm();
                Tag? savedTag = null;
                
                if (_isEditMode && _currentEditingTag != null)
                {
                    // Mise à jour
                    savedTag = await _apiService.UpdateTagAsync(_currentEditingTag.Id, tag);
                    if (savedTag != null)
                    {
                        await LoggingService.LogInfoAsync($"✅ Tag mis à jour: {savedTag.Name}");
                    }
                }
                else
                {
                    // Création
                    savedTag = await _apiService.CreateTagAsync(tag);
                    if (savedTag != null)
                    {
                        await LoggingService.LogInfoAsync($"✅ Tag créé: {savedTag.Name}");
                    }
                }
                
                if (savedTag != null)
                {
                    // Recharger la hiérarchie
                    await LoadTagsHierarchyAsync();
                    
                    // Sélectionner le tag sauvé
                    var savedNode = _hierarchyManager.FindNode(savedTag.Id);
                    if (savedNode != null)
                    {
                        SelectNodeInTree(savedNode);
                    }
                    
                    ClearEditor();
                    return true;
                }
                else
                {
                    MessageBox.Show("Erreur lors de la sauvegarde du tag", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur SaveCurrentTagAsync: {ex.Message}");
                MessageBox.Show($"Erreur lors de la sauvegarde: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ValidateTagData(out string validationMessage)
        {
            validationMessage = "";
            
            if (string.IsNullOrWhiteSpace(TagNameTextBox.Text))
            {
                validationMessage = "Le nom du tag est obligatoire";
                return false;
            }
            
            if (DetailTagTypeComboBox.SelectedItem is not ComboBoxItem selectedTypeItem)
            {
                validationMessage = "Le type du tag est obligatoire";
                return false;
            }
            
            // Validation couleur
            if (!string.IsNullOrWhiteSpace(ColorTextBox.Text) && !IsValidHexColor(ColorTextBox.Text))
            {
                validationMessage = "Le format de couleur doit être #RRGGBB";
                return false;
            }
            
            return true;
        }

        private Tag CreateTagFromForm()
        {
            var tag = new Tag();
            
            if (_isEditMode && _currentEditingTag != null)
            {
                tag.Id = _currentEditingTag.Id;
                tag.CreatedAt = _currentEditingTag.CreatedAt;
            }
            
            tag.Name = TagNameTextBox.Text.Trim();
            tag.Slug = !string.IsNullOrWhiteSpace(TagSlugTextBox.Text) ? TagSlugTextBox.Text.Trim() : GenerateSlug(tag.Name);
            tag.Type = ((ComboBoxItem)DetailTagTypeComboBox.SelectedItem).Tag?.ToString() ?? "custom";
            tag.Color = !string.IsNullOrWhiteSpace(ColorTextBox.Text) ? ColorTextBox.Text.Trim() : null;
            tag.Icon = !string.IsNullOrWhiteSpace(IconTextBox.Text) ? IconTextBox.Text.Trim() : null;
            tag.Description = !string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? DescriptionTextBox.Text.Trim() : null;
            tag.IsPublic = IsPublicCheckBox.IsChecked ?? true;
            tag.IsSystem = IsSystemCheckBox.IsChecked ?? false;
            tag.IsActive = IsActiveCheckBox.IsChecked ?? true;
            
            // Parent
            if (ParentTagComboBox.SelectedItem is TagHierarchyNode parentNode && !string.IsNullOrEmpty(parentNode.Tag.Id))
            {
                tag.ParentId = parentNode.Tag.Id;
            }
            
            return tag;
        }

        #endregion

        #region Event Handlers - Toolbar

        private async void CreateTagButton_Click(object sender, RoutedEventArgs e)
        {
            ClearEditor();
            
            // Préremplir le type depuis la combobox toolbar
            if (TagTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedType = selectedItem.Content?.ToString();
                for (int i = 0; i < DetailTagTypeComboBox.Items.Count; i++)
                {
                    if (DetailTagTypeComboBox.Items[i] is ComboBoxItem item && 
                        item.Tag?.ToString() == selectedType)
                    {
                        DetailTagTypeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            
            _isEditMode = false;
            TagIdPanel.Visibility = Visibility.Collapsed;
            
            await LoggingService.LogInfoAsync("🆕 Création d'un nouveau tag");
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;
            
            LoadTagIntoEditor(_selectedNode.Tag);
            _isEditMode = true;
            TagIdPanel.Visibility = Visibility.Visible;
        }

        private async void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;
            
            var tag = _selectedNode.Tag;
            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le tag '{tag.Name}' ?\n\n" +
                "Cette action supprimera aussi toutes ses associations avec les documents et ne peut pas être annulée.",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                var success = await _apiService.DeleteTagAsync(tag.Id);
                if (success)
                {
                    await LoggingService.LogInfoAsync($"✅ Tag supprimé: {tag.Name}");
                    await LoadTagsHierarchyAsync();
                    ClearEditor();
                }
                else
                {
                    MessageBox.Show("Erreur lors de la suppression du tag", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CreateChildButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;
            
            ClearEditor();
            
            // Prédéfinir le parent
            ParentTagComboBox.SelectedItem = _selectedNode;
            
            // Prédéfinir le même type que le parent
            var parentType = _selectedNode.Tag.Type;
            for (int i = 0; i < DetailTagTypeComboBox.Items.Count; i++)
            {
                if (DetailTagTypeComboBox.Items[i] is ComboBoxItem item && 
                    item.Tag?.ToString() == parentType)
                {
                    DetailTagTypeComboBox.SelectedIndex = i;
                    break;
                }
            }
            
            _isEditMode = false;
            TagIdPanel.Visibility = Visibility.Collapsed;
            
            await LoggingService.LogInfoAsync($"🆕 Création d'un tag enfant pour: {_selectedNode.Tag.Name}");
        }

        private async void RefreshHierarchyButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTagsHierarchyAsync();
        }

        private void ExpandAllButton_Click(object sender, RoutedEventArgs e)
        {
            _hierarchyManager.ExpandToLevel(int.MaxValue);
        }

        private void CollapseAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in _hierarchyManager.RootNodes)
            {
                node.IsExpanded = false;
            }
        }

        #endregion

        #region Event Handlers - Search and Tree

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilteredNodes();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = SearchTextBox.Tag?.ToString() ?? "";
            SearchTextBox.Foreground = Brushes.Gray;
            RefreshFilteredNodes();
        }

        private void TagsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedNode = e.NewValue as TagHierarchyNode;
            UpdateButtonStates();
            
            if (_selectedNode != null && !_isEditMode)
            {
                LoadTagIntoEditor(_selectedNode.Tag);
                DocumentInfoPanel.Visibility = Visibility.Visible;
                DocumentCountText.Text = $"{_selectedNode.DocumentCount} documents associés";
            }
        }

        #endregion

        #region Event Handlers - Editor Form

        private void TagNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-générer le slug si vide
            if (string.IsNullOrWhiteSpace(TagSlugTextBox.Text) || 
                TagSlugTextBox.Text == GenerateSlug(_currentEditingTag?.Name ?? ""))
            {
                TagSlugTextBox.Text = GenerateSlug(TagNameTextBox.Text);
            }
        }

        private async void DetailTagTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await LoadParentTagOptionsAsync();
        }

        private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateColorPreview();
        }

        private void IconTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateIconPreview();
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string color)
            {
                ColorTextBox.Text = color;
            }
        }

        private void PresetIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                IconTextBox.Text = button.Content?.ToString() ?? "";
            }
        }

        private async void SaveTagButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveCurrentTagAsync();
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode != null)
            {
                LoadTagIntoEditor(_selectedNode.Tag);
            }
            else
            {
                ClearEditor();
            }
        }

        #endregion

        #region Event Handlers - Window

        private async void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stats = await _apiService.GetTagSystemStatsAsync();
                if (stats != null)
                {
                    var message = $"📊 Statistiques des Tags\n\n" +
                                $"• Total des tags: {stats.TotalTags}\n" +
                                $"• Total des associations: {stats.TotalAssociations}\n\n" +
                                $"Répartition par type:\n";
                    
                    foreach (var kvp in stats.TagsByType)
                    {
                        message += $"• {kvp.Key}: {kvp.Value}\n";
                    }
                    
                    MessageBox.Show(message, "Statistiques", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur StatsButton_Click: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEditingTag != null)
            {
                await SaveCurrentTagAsync();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ViewDocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;
            
            // TODO: Ouvrir une fenêtre de recherche avec ce tag pré-sélectionné
            MessageBox.Show($"Fonctionnalité à implémenter:\nRecherche documents avec tag '{_selectedNode.Tag.Name}'", 
                          "À venir", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        private void LoadTagIntoEditor(Tag tag)
        {
            _currentEditingTag = tag;
            
            TagIdTextBox.Text = tag.Id;
            TagNameTextBox.Text = tag.Name;
            TagSlugTextBox.Text = tag.Slug;
            DescriptionTextBox.Text = tag.Description ?? "";
            ColorTextBox.Text = tag.Color ?? "#6C757D";
            IconTextBox.Text = tag.Icon ?? "";
            IsPublicCheckBox.IsChecked = tag.IsPublic;
            IsSystemCheckBox.IsChecked = tag.IsSystem;
            IsActiveCheckBox.IsChecked = tag.IsActive;
            
            // Sélectionner le type
            for (int i = 0; i < DetailTagTypeComboBox.Items.Count; i++)
            {
                if (DetailTagTypeComboBox.Items[i] is ComboBoxItem item && 
                    item.Tag?.ToString() == tag.Type)
                {
                    DetailTagTypeComboBox.SelectedIndex = i;
                    break;
                }
            }
            
            // Sélectionner le parent
            if (!string.IsNullOrEmpty(tag.ParentId))
            {
                var parentNode = _hierarchyManager.FindNode(tag.ParentId);
                if (parentNode != null)
                {
                    ParentTagComboBox.SelectedItem = parentNode;
                }
            }
            else
            {
                ParentTagComboBox.SelectedIndex = 0; // "Aucun parent"
            }
            
            UpdateColorPreview();
            UpdateIconPreview();
        }

        private void SelectNodeInTree(TagHierarchyNode node)
        {
            // TODO: Implémenter la sélection programmatique dans le TreeView
            // Ceci nécessite de naviguer dans la hiérarchie du TreeView
        }

        private void UpdateColorPreview()
        {
            // Vérifier que les contrôles sont initialisés
            if (ColorPreview == null || ColorTextBox == null)
                return;
                
            try
            {
                if (IsValidHexColor(ColorTextBox.Text))
                {
                    ColorPreview.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorTextBox.Text));
                }
                else
                {
                    ColorPreview.Fill = new SolidColorBrush(Colors.Gray);
                }
            }
            catch
            {
                ColorPreview.Fill = new SolidColorBrush(Colors.Gray);
            }
        }

        private void UpdateIconPreview()
        {
            // Vérifier que les contrôles sont initialisés
            if (IconPreview == null || IconTextBox == null)
                return;
                
            IconPreview.Text = !string.IsNullOrWhiteSpace(IconTextBox.Text) ? IconTextBox.Text : "🏷️";
        }

        private static bool IsValidHexColor(string color)
        {
            return !string.IsNullOrWhiteSpace(color) && 
                   Regex.IsMatch(color, @"^#[0-9A-Fa-f]{6}$");
        }

        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            
            return Regex.Replace(name.ToLower()
                .Replace(" ", "-")
                .Replace("à", "a").Replace("é", "e").Replace("è", "e")
                .Replace("ù", "u").Replace("ç", "c"), @"[^\w\-]", "");
        }

        /// <summary>
        /// Crée des tags de démonstration quand l'API n'est pas disponible
        /// </summary>
        private static List<Tag> CreateDemoTags()
        {
            return new List<Tag>
            {
                // Tags Clients
                new Tag { Id = "demo-client-acme", Name = "ACME Corp", Type = "client", Color = "#007BFF", Icon = "🏢", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-client-tech", Name = "TechStartup", Type = "client", Color = "#28A745", Icon = "🚀", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-client-pac", Name = "PAC Consulting", Type = "client", Color = "#6F42C1", Icon = "💼", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                
                // Tags Technologies
                new Tag { Id = "demo-tech-react", Name = "React", Type = "technology", Color = "#61DAFB", Icon = "⚛️", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-tech-vue", Name = "Vue.js", Type = "technology", Color = "#4FC08D", Icon = "🟢", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-tech-node", Name = "Node.js", Type = "technology", Color = "#339933", Icon = "🟢", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-tech-python", Name = "Python", Type = "technology", Color = "#3776AB", Icon = "🐍", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                
                // Tags Statuts
                new Tag { Id = "demo-status-progress", Name = "En cours", Type = "status", Color = "#FFC107", Icon = "⏳", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-status-done", Name = "Terminé", Type = "status", Color = "#28A745", Icon = "✅", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-status-review", Name = "En révision", Type = "status", Color = "#FD7E14", Icon = "👀", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-status-archived", Name = "Archivé", Type = "status", Color = "#6C757D", Icon = "📦", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                
                // Tags hiérarchiques - Projets sous clients
                new Tag { Id = "demo-proj-acme-alpha", Name = "Projet Alpha", Type = "custom", ParentId = "demo-client-acme", Color = "#007BFF", Icon = "📁", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-proj-acme-beta", Name = "Projet Beta", Type = "custom", ParentId = "demo-client-acme", Color = "#007BFF", Icon = "📁", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now },
                new Tag { Id = "demo-proj-tech-mvp", Name = "MVP", Type = "custom", ParentId = "demo-client-tech", Color = "#28A745", Icon = "🎯", IsActive = true, IsPublic = true, CreatedAt = DateTime.Now }
            };
        }

        #endregion

        #region Selection Mode

        /// <summary>
        /// Configure l'interface pour le mode sélection
        /// </summary>
        private void SetupSelectionMode()
        {
            // Afficher le bouton "Associate Selected"
            AssociateSelectedButton.Visibility = Visibility.Visible;
            
            // Modifier le titre de la fenêtre
            Title = "🏷️ Select Tags to Associate";
        }

        /// <summary>
        /// Handler pour le double-clic sur le TreeView - sélectionne un tag en mode sélection
        /// </summary>
        private async void TagsTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelectionMode) 
            {
                await LoggingService.LogDebugAsync("🚫 Double-clic ignoré: pas en mode sélection");
                return;
            }
            
            var treeView = sender as TreeView;
            var selectedItem = treeView?.SelectedItem as TagHierarchyNode;
            
            if (selectedItem?.Tag != null)
            {
                await LoggingService.LogInfoAsync($"🎯 Double-clic sur tag: {selectedItem.Tag.Name}");
                ToggleTagSelection(selectedItem.Tag);
                e.Handled = true;
            }
            else
            {
                await LoggingService.LogDebugAsync("🚫 Double-clic: aucun tag trouvé");
            }
        }

        /// <summary>
        /// Bascule la sélection d'un tag
        /// </summary>
        private async void ToggleTagSelection(Tag tag)
        {
            if (!_isSelectionMode) return;
            
            if (_selectedTags.Any(t => t.Id == tag.Id))
            {
                _selectedTags.RemoveAll(t => t.Id == tag.Id);
                await LoggingService.LogInfoAsync($"❌ Tag désélectionné: {tag.Name} (Total: {_selectedTags.Count})");
            }
            else
            {
                _selectedTags.Add(tag);
                await LoggingService.LogInfoAsync($"✅ Tag sélectionné: {tag.Name} (Total: {_selectedTags.Count})");
            }
            
            // Mettre à jour l'affichage visuel
            UpdateTagVisualSelection();
        }

        /// <summary>
        /// Met à jour l'affichage visuel des tags sélectionnés
        /// </summary>
        private void UpdateTagVisualSelection()
        {
            // Mettre à jour le texte du bouton pour indiquer le nombre de tags sélectionnés
            if (_isSelectionMode && AssociateSelectedButton != null)
            {
                AssociateSelectedButton.Content = $"✅ Associate Selected ({_selectedTags.Count})";
            }
        }

        /// <summary>
        /// Handler pour le bouton "Associate Selected"
        /// </summary>
        private async void AssociateSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            await LoggingService.LogInfoAsync($"🎯 Clic sur Associate Selected - Mode sélection: {_isSelectionMode}, Tags sélectionnés: {_selectedTags.Count}");
            
            if (!_isSelectionMode || _selectedTags.Count == 0)
            {
                await LoggingService.LogWarningAsync($"⚠️ Sélection invalide - Mode: {_isSelectionMode}, Count: {_selectedTags.Count}");
                MessageBox.Show("Veuillez sélectionner au moins un tag à associer.", 
                               "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await LoggingService.LogInfoAsync($"🏷️ Association de {_selectedTags.Count} tag(s) sélectionné(s)");
            
            // Invoquer le callback avec les tags sélectionnés
            _onTagsSelected?.Invoke(_selectedTags.ToList());
            
            // Fermer la fenêtre
            this.Close();
        }

        /// <summary>
        /// Confirme la sélection et ferme la fenêtre
        /// </summary>
        private void ConfirmSelection()
        {
            if (_isSelectionMode && _onTagsSelected != null && _selectedTags.Count > 0)
            {
                _onTagsSelected(_selectedTags.ToList());
                Close();
            }
        }

        #endregion
    }
}