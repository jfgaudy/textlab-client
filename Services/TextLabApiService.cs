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
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
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

        public async Task<string> GetEnvironmentStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/stats/environment");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"Erreur stats environnement: {ex.Message}";
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

        public void Dispose()
        {
            // HttpClient statique, pas de dispose nécessaire
        }
    }
} 