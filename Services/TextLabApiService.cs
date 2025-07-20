#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TextLabClient.Models;

namespace TextLabClient.Services
{
    public class TextLabApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        private string _baseUrl = "https://textlab-api.onrender.com";

        public bool IsConnected { get; private set; }

        public TextLabApiService()
        {
        }

        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            IsConnected = false;
        }

        public async Task<HealthInfo?> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var healthInfo = JsonConvert.DeserializeObject<HealthInfo>(content);
                    IsConnected = true;
                    return healthInfo;
                }
                else
                {
                    IsConnected = false;
                    return null;
                }
            }
            catch (Exception)
            {
                IsConnected = false;
                return null;
            }
        }

        /// <summary>
        /// Teste les nouveaux endpoints d'initialisation et diagnostics pour résoudre les problèmes Git Service
        /// </summary>
        public async Task<(bool Success, string Message)> TestInitializationEndpointsAsync()
        {
            var results = new StringBuilder();
            bool hasErrors = false;

            try
            {
                // Test 1: Diagnostics détaillés
                results.AppendLine("🔍 Test des endpoints d'initialisation:");
                
                try
                {
                    var diagnosticsResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/diagnostics");
                    if (diagnosticsResponse.IsSuccessStatusCode)
                    {
                        var diagnostics = await diagnosticsResponse.Content.ReadAsStringAsync();
                        results.AppendLine("✅ Diagnostics: OK");
                        
                        // Vérifier si le problème Git Service est présent
                        if (diagnostics.Contains("git_service") || diagnostics.Contains("ServiceInitializer"))
                        {
                            results.AppendLine("⚠️ Information Git Service détectée dans diagnostics");
                        }
                    }
                    else
                    {
                        results.AppendLine($"❌ Diagnostics: {diagnosticsResponse.StatusCode}");
                        hasErrors = true;
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Diagnostics: Exception - {ex.Message}");
                    hasErrors = true;
                }

                // Test 2: Stats environment (nouveau endpoint mentionné dans les logs)
                try
                {
                    var statsResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/stats/environment");
                    if (statsResponse.IsSuccessStatusCode)
                    {
                        results.AppendLine("✅ Stats Environment: OK");
                    }
                    else
                    {
                        results.AppendLine($"❌ Stats Environment: {statsResponse.StatusCode}");
                        hasErrors = true;
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Stats Environment: Exception - {ex.Message}");
                    hasErrors = true;
                }

                // Test 3: Admin Status (celui qui cause l'erreur Git Service)
                try
                {
                    var adminStatusResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/status");
                    if (adminStatusResponse.IsSuccessStatusCode)
                    {
                        results.AppendLine("✅ Admin Status: OK");
                    }
                    else
                    {
                        var errorContent = await adminStatusResponse.Content.ReadAsStringAsync();
                        results.AppendLine($"❌ Admin Status: {adminStatusResponse.StatusCode}");
                        
                        // Vérifier si c'est l'erreur Git Service spécifique
                        if (errorContent.Contains("get_git_service"))
                        {
                            results.AppendLine("🔧 Erreur Git Service détectée - problème côté serveur");
                            results.AppendLine("   → ServiceInitializer.get_git_service() manquant");
                        }
                        hasErrors = true;
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Admin Status: Exception - {ex.Message}");
                    hasErrors = true;
                }

                // Test 4: Health endpoint avec paramètres (vu dans les logs avec erreur 422)
                try
                {
                    var healthDetailedResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/health");
                    if (healthDetailedResponse.IsSuccessStatusCode)
                    {
                        results.AppendLine("✅ Documents Health: OK");
                    }
                    else
                    {
                        results.AppendLine($"⚠️ Documents Health: {healthDetailedResponse.StatusCode} (attendu selon logs)");
                        // 422 est attendu selon les logs, donc pas d'erreur
                    }
                }
                catch (Exception ex)
                {
                    results.AppendLine($"❌ Documents Health: Exception - {ex.Message}");
                }

                return (!hasErrors, results.ToString());
            }
            catch (Exception ex)
            {
                return (false, $"❌ Erreur générale lors du test: {ex.Message}");
            }
        }

        public async Task<List<Repository>?> GetRepositoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/repositories");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Debug: Afficher la réponse brute
                    System.Diagnostics.Debug.WriteLine($"Réponse brute repositories: {content}");
                    
                    // D'après la documentation, l'API retourne directement une liste de repositories
                    try
                    {
                    var repositories = JsonConvert.DeserializeObject<List<Repository>>(content);
                        if (repositories != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Repositories trouvés via Liste directe: {repositories.Count}");
                            return repositories;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur désérialisation Liste: {ex.Message}");
                    }
                    
                    // Fallback : essayer avec ApiResponse si la structure a changé
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<Repository>>>(content);
                        if (apiResponse?.Data != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Repositories trouvés via ApiResponse: {apiResponse.Data.Count}");
                            return apiResponse.Data;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur désérialisation ApiResponse: {ex.Message}");
                    }
                    
                    return null;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur HTTP: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception GetRepositoriesAsync: {ex.Message}");
                return null;
            }
        }

        // ===== LECTURE DES DOCUMENTS =====

        public async Task<List<Document>> GetDocumentsAsync(string? repositoryId = null)
        {
            try
            {
                var url = $"{_baseUrl}/api/v1/documents/";
                if (!string.IsNullOrEmpty(repositoryId))
                {
                    url += $"?repository_id={repositoryId}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 GetDocuments Response: {content}");

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<DocumentListResponse>>(content);
                if (apiResponse?.IsSuccess == true && apiResponse.Data?.Documents != null)
                {
                    return apiResponse.Data.Documents;
                }

                // Essayer la structure directe
                var directResponse = JsonConvert.DeserializeObject<DocumentListResponse>(content);
                if (directResponse?.Documents != null)
                {
                    return directResponse.Documents;
                }

                return new List<Document>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetDocumentsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Document?> GetDocumentAsync(string documentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 GetDocument Response: {content}");

                // Essayer d'abord avec ApiResponse wrapper
                try
                {
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<Document>>(content);
                    if (apiResponse?.IsSuccess == true && apiResponse.Data != null)
                    {
                        return apiResponse.Data;
                    }
                }
                catch
                {
                    // Si ça échoue, essayer directement
                }

                // Essayer la désérialisation directe
                return JsonConvert.DeserializeObject<Document>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetDocumentAsync: {ex.Message}");
                return null;
            }
        }

        // SUPPRIMÉ: UpdateDocumentAsync - Endpoint non disponible dans l'API

        public async Task<Document?> CreateDocumentAsync(string title, string content, string repositoryId, string? category = null, string? visibility = "private", string? createdBy = null)
        {
            try
            {
                var document = new
                {
                    title = title,
                    content = content,
                    repository_id = repositoryId,
                    category = category,
                    visibility = visibility ?? "private",
                    created_by = createdBy
                };

                var json = JsonConvert.SerializeObject(document);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/documents/", httpContent);
                
                System.Diagnostics.Debug.WriteLine($"🚀 CreateDocument - URL: {_baseUrl}/api/v1/documents/");
                System.Diagnostics.Debug.WriteLine($"🚀 CreateDocument - Status: {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🚀 CreateDocument - Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Essayer d'abord avec ApiResponse wrapper
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<Document>>(responseContent);
                        if (apiResponse?.IsSuccess == true && apiResponse.Data != null)
                        {
                            return apiResponse.Data;
                        }
                    }
                    catch
                    {
                        // Si ça échoue, essayer directement
                    }

                    // Essayer la désérialisation directe
                    return JsonConvert.DeserializeObject<Document>(responseContent);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur HTTP {response.StatusCode}: {responseContent}");
                    throw new HttpRequestException($"Erreur lors de la création du document: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur CreateDocumentAsync: {ex.Message}");
                throw;
            }
        }

        // SUPPRIMÉ: DeleteDocumentAsync - Endpoint non disponible dans l'API

        public async Task<Document?> GetDocumentWithContentAsync(string documentId, string? version = null)
        {
            try
            {
                var url = $"{_baseUrl}/api/v1/documents/{documentId}/content";
                if (!string.IsNullOrEmpty(version))
                {
                    url += $"?version={version}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Document>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetDocumentWithContentAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetDocumentRawContentAsync(string documentId)
        {
            try
            {
                var rawResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/raw");
                rawResponse.EnsureSuccessStatusCode();

                var rawContent = await rawResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 GetDocumentRawContent Response: {rawContent}");

                // Essayer de parser la réponse
                try
                {
                    var parsedResponse = JsonConvert.DeserializeObject<dynamic>(rawContent);
                    if (parsedResponse?.content != null)
                    {
                        return parsedResponse.content.ToString();
                    }
                }
                catch
                {
                    // Si le parsing JSON échoue, retourner le contenu brut
                    return rawContent;
                }

                return rawContent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetDocumentRawContentAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<int> GetDocumentVersionsCountAsync(string documentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/versions");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var versions = JsonConvert.DeserializeObject<DocumentVersions>(content);
                    return versions?.TotalVersions ?? 0;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // ===== CONTENU DES DOCUMENTS =====

        public async Task<DocumentContent?> GetDocumentContentAsync(string documentId)
        {
            try
            {
                await LoggingService.LogDebugAsync($"🔍 GetDocumentContent appelé avec documentId: '{documentId}'");
                await LoggingService.LogDebugAsync($"🔍 URL complète: {_baseUrl}/api/v1/documents/{documentId}/content");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/content");
                
                await LoggingService.LogDebugAsync($"🔍 Réponse HTTP: {response.StatusCode} - {response.ReasonPhrase}");
                
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                await LoggingService.LogDebugAsync($"🔍 GetDocumentContent Response: {content}");

                // L'API retourne directement le contenu, pas dans un wrapper
                var result = JsonConvert.DeserializeObject<DocumentContent>(content);
                return result;
            }
            catch (HttpRequestException ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur HTTP GetDocumentContent: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur GetDocumentContent: {ex.Message}");
                return null;
            }
        }

        public async Task<DocumentContent?> GetDocumentContentVersionAsync(string documentId, string commitSha)
        {
            try
            {
                // Utiliser l'endpoint content avec le paramètre version
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/content?version={commitSha}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 GetDocumentContentVersion Response: {content}");

                var result = JsonConvert.DeserializeObject<DocumentContent>(content);
                return result;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur HTTP GetDocumentContentVersion: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetDocumentContentVersion: {ex.Message}");
                return null;
            }
        }

        // ===== VERSIONING ET HISTORIQUE =====

        public async Task<DocumentVersions?> GetDocumentVersionsAsync(string documentId, int limit = 50)
        {
            try
            {
                await LoggingService.LogDebugAsync($"🔍 GetDocumentVersions appelé avec documentId: '{documentId}'");
                await LoggingService.LogDebugAsync($"🔍 URL complète: {_baseUrl}/api/v1/documents/{documentId}/versions?limit={limit}");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/versions?limit={limit}");
                
                await LoggingService.LogDebugAsync($"🔍 Réponse HTTP: {response.StatusCode} - {response.ReasonPhrase}");
                
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                await LoggingService.LogDebugAsync($"🔍 GetDocumentVersions Response: {content}");

                // Désérialiser directement vers DocumentVersions
                var documentVersions = JsonConvert.DeserializeObject<DocumentVersions>(content);
                await LoggingService.LogDebugAsync($"✅ Désérialisation réussie: {documentVersions?.TotalVersions ?? 0} versions trouvées");
                
                return documentVersions;
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur GetDocumentVersionsAsync: {ex.Message}");
                return null;
            }
        }

        // ===== MÉTHODES DE DIAGNOSTIC ET SUPPORT =====

        public async Task<string> GetDiagnosticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/diagnostics");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"Erreur diagnostic: {ex.Message}";
            }
        }



        // Méthode de test pour diagnostiquer les problèmes d'endpoints
        public async Task<string> TestEndpointsAsync(string documentId)
        {
            var results = new StringBuilder();
            results.AppendLine($"🔍 Test des endpoints pour le document: {documentId}");
            results.AppendLine();
            
            // Test endpoint /content
            try
            {
                var contentResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/content");
                results.AppendLine($"📄 GET /content → {contentResponse.StatusCode} {contentResponse.ReasonPhrase}");
                if (!contentResponse.IsSuccessStatusCode)
                {
                    var errorContent = await contentResponse.Content.ReadAsStringAsync();
                    results.AppendLine($"   Erreur: {errorContent.Substring(0, Math.Min(100, errorContent.Length))}...");
                }
            }
            catch (Exception ex)
            {
                results.AppendLine($"❌ GET /content → Exception: {ex.Message}");
            }
            
            // Test endpoint /versions
            try
            {
                var versionsResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/versions");
                results.AppendLine($"📚 GET /versions → {versionsResponse.StatusCode} {versionsResponse.ReasonPhrase}");
                if (!versionsResponse.IsSuccessStatusCode)
                {
                    var errorContent = await versionsResponse.Content.ReadAsStringAsync();
                    results.AppendLine($"   Erreur: {errorContent.Substring(0, Math.Min(100, errorContent.Length))}...");
                }
            }
            catch (Exception ex)
            {
                results.AppendLine($"❌ GET /versions → Exception: {ex.Message}");
            }
            
            // Test endpoint /raw
            try
            {
                var rawResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/raw");
                results.AppendLine($"📝 GET /raw → {rawResponse.StatusCode} {rawResponse.ReasonPhrase}");
                if (!rawResponse.IsSuccessStatusCode)
                {
                    var errorContent = await rawResponse.Content.ReadAsStringAsync();
                    results.AppendLine($"   Erreur: {errorContent.Substring(0, Math.Min(100, errorContent.Length))}...");
                }
            }
            catch (Exception ex)
            {
                results.AppendLine($"❌ GET /raw → Exception: {ex.Message}");
            }
            
            return results.ToString();
        }

        // ===== NOUVEAUX ENDPOINTS API =====

        /// <summary>
        /// Utilise les nouveaux endpoints publics pour récupérer les repositories
        /// </summary>
        public async Task<List<Repository>> GetPublicRepositoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/repositories/");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 GetPublicRepositories Response: {content}");

                return JsonConvert.DeserializeObject<List<Repository>>(content) ?? new List<Repository>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetPublicRepositoriesAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Récupère les détails d'un repository spécifique via l'endpoint public
        /// </summary>
        public async Task<Repository?> GetRepositoryDetailsAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/repositories/{repositoryId}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 GetRepositoryDetails Response: {content}");

                return JsonConvert.DeserializeObject<Repository>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetRepositoryDetailsAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère le nombre de documents dans un repository
        /// </summary>
        public async Task<object?> GetRepositoryDocumentCountAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/repositories/{repositoryId}/documents/count");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetRepositoryDocumentCountAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Supprime un document avec support d'archivage
        /// </summary>
        public async Task<object?> DeleteDocumentAsync(string documentId, string author, bool softDelete = false)
        {
            try
            {
                var url = $"{_baseUrl}/api/v1/documents/{documentId}?author={Uri.EscapeDataString(author)}&soft_delete={softDelete}";
                var response = await _httpClient.DeleteAsync(url);
                response.EnsureSuccessStatusCode();

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return new { Success = true, Message = "Document supprimé avec succès" };
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur DeleteDocumentAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Archive un document (suppression logique)
        /// </summary>
        public async Task<object?> ArchiveDocumentAsync(string documentId, string author, string? reason = null)
        {
            try
            {
                var requestData = new
                {
                    soft_delete = true,
                    reason = reason
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v1/documents/{documentId}/archive?author={Uri.EscapeDataString(author)}");
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(responseContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur ArchiveDocumentAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Met à jour un document via le nouvel endpoint PUT
        /// </summary>
        public async Task<object?> UpdateDocumentAsync(string documentId, string author, object updateData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(updateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/api/v1/documents/{documentId}?author={Uri.EscapeDataString(author)}", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"🔍 UpdateDocument Response: {responseContent}");

                return JsonConvert.DeserializeObject<object>(responseContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur UpdateDocumentAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Change le repository actif
        /// </summary>
        public async Task<bool> SwitchRepositoryAsync(int repositoryId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/documents/switch-repository/{repositoryId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur SwitchRepositoryAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Récupère les diagnostics de l'architecture adaptative
        /// </summary>
        public async Task<object?> GetArchitectureDiagnosticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/diagnostics");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetArchitectureDiagnosticsAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère les statistiques d'environnement
        /// </summary>
        public async Task<object?> GetEnvironmentStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/stats/environment");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetEnvironmentStatsAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Health check spécifique à l'architecture adaptative
        /// </summary>
        public async Task<object?> GetDocumentsHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/health");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<object>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetDocumentsHealthAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Récupère la configuration complète d'un repository incluant la racine des documents
        /// </summary>
        public async Task<string> GetRepositoryDocumentsRootAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/config");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"🔍 GetRepositoryConfig Response: {content}");
                    
                    var configResponse = JsonConvert.DeserializeObject<dynamic>(content);
                    var rootDocuments = configResponse?.config?.root_documents?.ToString() ?? "documents/";
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Repository {repositoryId}, RootDocuments: '{rootDocuments}'");
                    return rootDocuments;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur récupération config repository: {response.StatusCode}");
                    return "documents/"; // Fallback
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception GetRepositoryDocumentsRootAsync: {ex.Message}");
                return "documents/"; // Fallback
            }
        }

        /// <summary>
        /// Construit l'URL GitHub complète en tenant compte de la racine configurable
        /// </summary>
        public async Task<string?> BuildGitHubUrlAsync(Repository repository, string gitPath)
        {
            try
            {
                // Récupérer la racine des documents depuis la configuration
                var documentsRoot = await GetRepositoryDocumentsRootAsync(repository.Id);
                
                // S'assurer que la racine se termine par /
                if (!documentsRoot.EndsWith("/"))
                {
                    documentsRoot += "/";
                }
                
                // Construire l'URL GitHub complète
                var githubUrl = "";
                
                if (repository.Name.ToLower() == "gaudylab")
                {
                    githubUrl = $"https://github.com/jfgaudy/gaudylab/blob/main/{documentsRoot}{gitPath}";
                }
                else if (repository.Name.ToLower().Contains("pac"))
                {
                    githubUrl = $"https://github.com/jfgaudy/PAC_Repo/blob/main/{documentsRoot}{gitPath}";
                }
                else
                {
                    githubUrl = $"https://github.com/jfgaudy/{repository.Name}/blob/main/{documentsRoot}{gitPath}";
                }
                
                System.Diagnostics.Debug.WriteLine($"🔗 URL GitHub construite: {githubUrl}");
                System.Diagnostics.Debug.WriteLine($"📁 Racine utilisée: {documentsRoot}");
                
                return githubUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur construction URL GitHub: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            // HttpClient statique, pas de dispose nécessaire
        }
    }
} 