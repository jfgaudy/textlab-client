#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TextLabClient.Models;

namespace TextLabClient.Services
{
    public class TextLabAdminService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        private readonly string _baseUrl;

        public TextLabAdminService(string baseUrl = "https://textlab-api.onrender.com")
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        // ===== GESTION DES REPOSITORIES =====

        /// <summary>
        /// Récupère la liste de tous les repositories configurés
        /// </summary>
        public async Task<List<Repository>> GetRepositoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/repositories");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Repository>>(content) ?? new List<Repository>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetRepositoriesAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crée un nouveau repository
        /// </summary>
        public async Task<Repository?> CreateRepositoryAsync(string name, string type, string? localPath = null, string? remoteUrl = null, string? description = null, bool isDefault = false)
        {
            try
            {
                var repoData = new
                {
                    name = name,
                    type = type,
                    local_path = localPath,
                    remote_url = remoteUrl,
                    description = description,
                    is_default = isDefault
                };

                var json = JsonConvert.SerializeObject(repoData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/admin/repositories", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Repository>(responseContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur CreateRepositoryAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Active un repository existant
        /// </summary>
        public async Task<bool> ActivateRepositoryAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/activate", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ActivateRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Active un repository par son nom
        /// </summary>
        public async Task<bool> ActivateRepositoryByNameAsync(string repositoryName)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryName}/activate-by-name", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ActivateRepositoryByNameAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Définit un repository comme repository par défaut
        /// </summary>
        public async Task<bool> SetDefaultRepositoryAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/set-default", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur SetDefaultRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprime un repository
        /// </summary>
        public async Task<bool> DeleteRepositoryAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur DeleteRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        // ===== CONFIGURATION GIT =====

        /// <summary>
        /// Configure un repository Git local
        /// </summary>
        public async Task<bool> ConfigureLocalRepositoryAsync(LocalRepoConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/admin/git/configure/local", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ConfigureLocalRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configure un repository GitHub
        /// </summary>
        public async Task<bool> ConfigureGitHubRepositoryAsync(GitHubRepoConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/admin/git/configure/github", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ConfigureGitHubRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crée un nouveau repository Git local
        /// </summary>
        public async Task<bool> CreateLocalRepositoryAsync(LocalRepoConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/admin/git/create-local", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur CreateLocalRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Valide un repository avant configuration
        /// </summary>
        public async Task<bool> ValidateRepositoryAsync(string repoType, string repoPath)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/git/validate/{repoType}?repo_path={Uri.EscapeDataString(repoPath)}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ValidateRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        // ===== OPÉRATIONS PULL =====

        /// <summary>
        /// Effectue un pull sur le repository actuel
        /// </summary>
        public async Task<PullResponse?> PullCurrentRepositoryAsync(PullRequest? pullRequest = null)
        {
            try
            {
                var json = JsonConvert.SerializeObject(pullRequest ?? new PullRequest());
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/admin/git/pull", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PullResponse>(responseContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur PullCurrentRepositoryAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Effectue un pull sur un repository spécifique
        /// </summary>
        public async Task<PullResponse?> PullRepositoryAsync(string repositoryId, PullRequest? pullRequest = null)
        {
            try
            {
                var json = JsonConvert.SerializeObject(pullRequest ?? new PullRequest());
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/pull", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PullResponse>(responseContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur PullRepositoryAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère le statut de pull du repository actuel
        /// </summary>
        public async Task<PullStatus?> GetPullStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/git/pull/status");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PullStatus>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetPullStatusAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère le statut de pull d'un repository spécifique
        /// </summary>
        public async Task<PullStatus?> GetRepositoryPullStatusAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/pull/status");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PullStatus>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetRepositoryPullStatusAsync: {ex.Message}");
                return null;
            }
        }

        // ===== CREDENTIALS =====

        /// <summary>
        /// Configure les credentials d'un repository
        /// </summary>
        public async Task<bool> SetRepositoryCredentialsAsync(string repositoryId, object credentials)
        {
            try
            {
                var json = JsonConvert.SerializeObject(credentials);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/credentials", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur SetRepositoryCredentialsAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprime les credentials d'un repository
        /// </summary>
        public async Task<bool> ClearRepositoryCredentialsAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/credentials");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ClearRepositoryCredentialsAsync: {ex.Message}");
                return false;
            }
        }

        // ===== STATUT ET DIAGNOSTICS =====

        /// <summary>
        /// Récupère l'état général du système
        /// </summary>
        public async Task<object?> GetSystemStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/status");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetSystemStatusAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère le statut de la configuration Git
        /// </summary>
        public async Task<object?> GetGitStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/git/status");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetGitStatusAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère les statistiques Git détaillées
        /// </summary>
        public async Task<object?> GetGitStatisticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/git/stats");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetGitStatisticsAsync: {ex.Message}");
                return null;
            }
        }
    }
} 