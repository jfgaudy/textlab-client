#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TextLabClient.Models;
using TextLabClient.Services;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace TextLabClient
{
    public partial class RepositoryManagementWindow : Window
    {
        private readonly TextLabAdminService _adminService;
        private readonly TextLabApiService _apiService;
        private List<Repository> _repositories = new();
        private Repository? _selectedRepository;

        public RepositoryManagementWindow()
        {
            InitializeComponent();
            _adminService = new TextLabAdminService();
            _apiService = new TextLabApiService();
            LoadRepositories();
        }

        private async void LoadRepositories()
        {
            try
            {
                SetStatus("Chargement des repositories...");
                _repositories = await _adminService.GetRepositoriesAsync();
                LvRepositories.ItemsSource = _repositories;
                SetStatus($"{_repositories.Count} repositories chargés");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur lors du chargement des repositories: {ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetStatus(string message)
        {
            TxtStatus.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        // ===== EVENTS REPOSITORIES TAB =====

        private void BtnRefreshRepositories_Click(object sender, RoutedEventArgs e)
        {
            LoadRepositories();
        }

        private void BtnAddRepository_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Configuration tab
            var tabControl = (TabControl)((Grid)((TabItem)LvRepositories.Parent).Parent).Parent;
            tabControl.SelectedIndex = 1; // Configuration tab
        }

        private void LvRepositories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRepository = LvRepositories.SelectedItem as Repository;
            bool hasSelection = _selectedRepository != null;

            BtnActivateRepo.IsEnabled = hasSelection;
            BtnSetDefaultRepo.IsEnabled = hasSelection;
            BtnPullRepo.IsEnabled = hasSelection;
            BtnConfigCredentials.IsEnabled = hasSelection;
            BtnDeleteRepo.IsEnabled = hasSelection;
        }

        private async void BtnActivateRepo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null) return;

            try
            {
                SetStatus($"Activation du repository {_selectedRepository.Name}...");
                bool success = await _adminService.ActivateRepositoryAsync(_selectedRepository.Id);
                
                if (success)
                {
                    SetStatus($"Repository {_selectedRepository.Name} activé");
                    LoadRepositories();
                }
                else
                {
                    SetStatus("Échec de l'activation");
                    MessageBox.Show("Échec de l'activation du repository", "Erreur", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSetDefaultRepo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null) return;

            try
            {
                SetStatus($"Définition de {_selectedRepository.Name} comme repository par défaut...");
                bool success = await _adminService.SetDefaultRepositoryAsync(_selectedRepository.Id);
                
                if (success)
                {
                    SetStatus($"Repository {_selectedRepository.Name} défini par défaut");
                    LoadRepositories();
                }
                else
                {
                    SetStatus("Échec de la définition par défaut");
                    MessageBox.Show("Échec de la définition du repository par défaut", "Erreur", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnPullRepo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null) return;

            try
            {
                SetStatus($"Pull du repository {_selectedRepository.Name}...");
                var pullRequest = new PullRequest
                {
                    RepositoryId = _selectedRepository.Id,
                    AutoResolveConflicts = false,
                    ForcePull = false
                };

                var response = await _adminService.PullRepositoryAsync(_selectedRepository.Id, pullRequest);
                
                if (response?.Success == true)
                {
                    SetStatus($"Pull réussi: {response.Changes.CommitsPulled} commits, {response.Changes.FilesUpdated} fichiers");
                    if (response.Changes.Conflicts.Any())
                    {
                        MessageBox.Show($"Pull réussi mais {response.Changes.Conflicts.Count} conflits détectés:\n" +
                                       string.Join("\n", response.Changes.Conflicts), 
                                       "Conflits", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    SetStatus($"Échec du pull: {response?.Error ?? "Erreur inconnue"}");
                    MessageBox.Show($"Échec du pull: {response?.Error ?? "Erreur inconnue"}", 
                                   "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnConfigCredentials_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null) return;

            // Dialogue simple pour les credentials
            var dialog = new CredentialsDialog(_selectedRepository.Name);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    SetStatus("Configuration des credentials...");
                    var credentials = new
                    {
                        username = dialog.Username,
                        token = dialog.Token
                    };

                    bool success = await _adminService.SetRepositoryCredentialsAsync(_selectedRepository.Id, credentials);
                    SetStatus(success ? "Credentials configurés" : "Échec de la configuration");
                }
                catch (Exception ex)
                {
                    SetStatus($"Erreur: {ex.Message}");
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnDeleteRepo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRepository == null) return;

            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer le repository '{_selectedRepository.Name}' ?\n\n" +
                                        "Cette action est irréversible et supprimera toutes les configurations.",
                                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SetStatus($"Suppression du repository {_selectedRepository.Name}...");
                    bool success = await _adminService.DeleteRepositoryAsync(_selectedRepository.Id);
                    
                    if (success)
                    {
                        SetStatus("Repository supprimé");
                        LoadRepositories();
                    }
                    else
                    {
                        SetStatus("Échec de la suppression");
                        MessageBox.Show("Échec de la suppression du repository", "Erreur", 
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    SetStatus($"Erreur: {ex.Message}");
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnPullAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Pull de tous les repositories...");
                // Implementation pour pull all
                MessageBox.Show("Fonctionnalité en cours de développement", "Info", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnGetSystemStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Récupération du statut système...");
                var status = await _adminService.GetSystemStatusAsync();
                
                if (status != null)
                {
                    var json = JsonConvert.SerializeObject(status, Formatting.Indented);
                    MessageBox.Show(json, "Statut Système", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                SetStatus("Statut système récupéré");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== EVENTS CONFIGURATION TAB =====

        private void CbRepositoryType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbRepositoryType.SelectedItem is ComboBoxItem selected)
            {
                string type = selected.Tag?.ToString() ?? "";
                
                PnlLocalRepo.Visibility = type == "local" ? Visibility.Visible : Visibility.Collapsed;
                PnlGitHubRepo.Visibility = type == "github" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BtnBrowseLocal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Sélectionnez le dossier du repository Git local"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtLocalPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnBrowseGitHub_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Sélectionnez le dossier pour cloner le repository GitHub"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtGitHubLocalPath.Text = dialog.SelectedPath;
            }
        }

        private async void BtnValidateRepo_Click(object sender, RoutedEventArgs e)
        {
            if (CbRepositoryType.SelectedItem is not ComboBoxItem selected) return;
            
            string type = selected.Tag?.ToString() ?? "";
            string path = type == "local" ? TxtLocalPath.Text : TxtGitHubLocalPath.Text;

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Veuillez spécifier un chemin", "Validation", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SetStatus("Validation du repository...");
                bool isValid = await _adminService.ValidateRepositoryAsync(type, path);
                
                string message = isValid ? "Repository valide ✅" : "Repository invalide ❌";
                SetStatus(message);
                MessageBox.Show(message, "Validation", MessageBoxButton.OK, 
                               isValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCreateRepo_Click(object sender, RoutedEventArgs e)
        {
            if (CbRepositoryType.SelectedItem is not ComboBoxItem selected) return;

            string type = selected.Tag?.ToString() ?? "";
            string name = TxtRepoName.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Veuillez spécifier un nom", "Création", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SetStatus("Création du repository...");

                Repository? newRepo = null;

                if (type == "local")
                {
                    var config = new LocalRepoConfig
                    {
                        RepoPath = TxtLocalPath.Text.Trim(),
                        Name = name,
                        Description = TxtRepoDescription.Text.Trim(),
                        ValidateStructure = ChkValidateStructure.IsChecked ?? true
                    };

                    bool configured = await _adminService.ConfigureLocalRepositoryAsync(config);
                    if (configured)
                    {
                        newRepo = await _adminService.CreateRepositoryAsync(name, type, 
                                                                           config.RepoPath, null, 
                                                                           config.Description, 
                                                                           ChkSetAsDefault.IsChecked ?? false);
                    }
                }
                else if (type == "github")
                {
                    var config = new GitHubRepoConfig
                    {
                        RepoUrl = TxtGitHubUrl.Text.Trim(),
                        LocalPath = TxtGitHubLocalPath.Text.Trim(),
                        Name = name,
                        Description = TxtRepoDescription.Text.Trim(),
                        BranchName = TxtBranchName.Text.Trim(),
                        CloneIfMissing = ChkCloneIfMissing.IsChecked ?? true
                    };

                    bool configured = await _adminService.ConfigureGitHubRepositoryAsync(config);
                    if (configured)
                    {
                        newRepo = await _adminService.CreateRepositoryAsync(name, type, 
                                                                           config.LocalPath, config.RepoUrl, 
                                                                           config.Description, 
                                                                           ChkSetAsDefault.IsChecked ?? false);
                    }
                }

                if (newRepo != null)
                {
                    SetStatus($"Repository '{name}' créé avec succès");
                    MessageBox.Show($"Repository '{name}' créé avec succès!", "Succès", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                    BtnClearForm_Click(sender, e);
                    LoadRepositories();
                }
                else
                {
                    SetStatus("Échec de la création du repository");
                    MessageBox.Show("Échec de la création du repository", "Erreur", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearForm_Click(object sender, RoutedEventArgs e)
        {
            TxtRepoName.Clear();
            TxtRepoDescription.Clear();
            TxtLocalPath.Clear();
            TxtGitHubUrl.Clear();
            TxtGitHubLocalPath.Clear();
            TxtBranchName.Text = "main";
            ChkValidateStructure.IsChecked = true;
            ChkCloneIfMissing.IsChecked = true;
            ChkSetAsDefault.IsChecked = false;
            CbRepositoryType.SelectedIndex = -1;
        }

        // ===== EVENTS SYNCHRONIZATION TAB =====

        private async void BtnRefreshSync_Click(object sender, RoutedEventArgs e)
        {
            await LoadSyncStatus();
        }

        private async void BtnPullAllRepos_Click(object sender, RoutedEventArgs e)
        {
            // Implementation pour pull all repos
            MessageBox.Show("Fonctionnalité en cours de développement", "Info", 
                           MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task LoadSyncStatus()
        {
            try
            {
                SetStatus("Chargement du statut de synchronisation...");
                var syncStatuses = new List<PullStatus>();

                foreach (var repo in _repositories)
                {
                    try
                    {
                        var status = await _adminService.GetRepositoryPullStatusAsync(repo.Id);
                        if (status != null)
                        {
                            syncStatuses.Add(status);
                        }
                    }
                    catch
                    {
                        // Continue avec les autres repos
                    }
                }

                LvSyncStatus.ItemsSource = syncStatuses;
                SetStatus($"Statut de {syncStatuses.Count} repositories chargé");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== EVENTS DIAGNOSTICS TAB =====

        private async void BtnGetDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Récupération des diagnostics...");
                var diagnostics = await _apiService.GetArchitectureDiagnosticsAsync();
                
                if (diagnostics != null)
                {
                    var json = JsonConvert.SerializeObject(diagnostics, Formatting.Indented);
                    TxtDiagnostics.Text = $"=== DIAGNOSTICS ARCHITECTURE ===\n{DateTime.Now}\n\n{json}";
                }
                
                SetStatus("Diagnostics récupérés");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                TxtDiagnostics.Text = $"ERREUR: {ex.Message}";
            }
        }

        private async void BtnGetEnvironmentStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Récupération des statistiques d'environnement...");
                var stats = await _apiService.GetEnvironmentStatsAsync();
                
                if (stats != null)
                {
                    var json = JsonConvert.SerializeObject(stats, Formatting.Indented);
                    TxtDiagnostics.Text = $"=== STATISTIQUES ENVIRONNEMENT ===\n{DateTime.Now}\n\n{json}";
                }
                
                SetStatus("Statistiques récupérées");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                TxtDiagnostics.Text = $"ERREUR: {ex.Message}";
            }
        }

        private async void BtnHealthCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetStatus("Health check en cours...");
                var health = await _apiService.GetDocumentsHealthAsync();
                
                if (health != null)
                {
                    var json = JsonConvert.SerializeObject(health, Formatting.Indented);
                    TxtDiagnostics.Text = $"=== HEALTH CHECK ===\n{DateTime.Now}\n\n{json}";
                }
                
                SetStatus("Health check terminé");
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur: {ex.Message}");
                TxtDiagnostics.Text = $"ERREUR: {ex.Message}";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Dialogue simple pour les credentials
    public partial class CredentialsDialog : Window
    {
        public string Username { get; private set; } = string.Empty;
        public string Token { get; private set; } = string.Empty;

        public CredentialsDialog(string repositoryName)
        {
            InitializeComponent();
            Title = $"Credentials - {repositoryName}";
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Username = TxtUsername.Text.Trim();
            Token = TxtToken.Password;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Token))
            {
                MessageBox.Show("Veuillez remplir tous les champs", "Validation", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 