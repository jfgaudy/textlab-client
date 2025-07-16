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

        public async Task<List<Document>?> GetDocumentsAsync(string repositoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/?repository_id={repositoryId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Réponse documents pour repo {repositoryId}: {content}");
                    
                    // D'après la documentation, la réponse a cette structure avec pagination
                    try
                    {
                        var documentResponse = JsonConvert.DeserializeObject<DocumentsResponse>(content);
                        if (documentResponse?.Documents != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Documents trouvés: {documentResponse.Documents.Count}");
                            
                            // Debug des premiers documents pour voir les champs
                            foreach (var doc in documentResponse.Documents.Take(2))
                            {
                                System.Diagnostics.Debug.WriteLine($"Document {doc.Title}:");
                                System.Diagnostics.Debug.WriteLine($"  FileSizeBytes={doc.FileSizeBytes}");
                                System.Diagnostics.Debug.WriteLine($"  RepositoryName='{doc.RepositoryName}'");
                                System.Diagnostics.Debug.WriteLine($"  GitPath='{doc.GitPath}'");
                                System.Diagnostics.Debug.WriteLine($"  CurrentCommitSha='{doc.CurrentCommitSha}'");
                                
                                // Afficher aussi dans la console pour debug
                                Console.WriteLine($"DEBUG: Document {doc.Title} - Taille: {doc.FileSizeBytes} octets");
                            }
                            
                            return documentResponse.Documents;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur désérialisation DocumentsResponse: {ex.Message}");
                    }
                    
                    // Fallback : essayer avec ApiResponse
                    try
                    {
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<Document>>>(content);
                        if (apiResponse?.Data != null)
                        {
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
                    System.Diagnostics.Debug.WriteLine($"Erreur HTTP documents: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception GetDocumentsAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<Document?> GetDocumentAsync(string repositoryId, string documentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/repositories/{repositoryId}/documents/{documentId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<Document>>(content);
                    return apiResponse?.Data;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Document?> UpdateDocumentAsync(string repositoryId, string documentId, Document document)
        {
            try
            {
                var json = JsonConvert.SerializeObject(document);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/repositories/{repositoryId}/documents/{documentId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<Document>>(responseContent);
                    return apiResponse?.Data;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Document?> CreateDocumentAsync(string repositoryId, Document document)
        {
            try
            {
                var json = JsonConvert.SerializeObject(document);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/repositories/{repositoryId}/documents", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse<Document>>(responseContent);
                    return apiResponse?.Data;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string repositoryId, string documentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/repositories/{repositoryId}/documents/{documentId}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Nouvelles méthodes pour la visualisation détaillée des documents
        
        public async Task<Document?> GetDocumentDetailsAsync(string documentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Détails document {documentId}: {content}");
                    
                    var document = JsonConvert.DeserializeObject<Document>(content);
                    return document;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur récupération détails: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception GetDocumentDetailsAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<DocumentContent?> GetDocumentContentAsync(string documentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/content");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ Contenu document {documentId}: {content.Length} caractères");
                    
                    var documentContent = JsonConvert.DeserializeObject<DocumentContent>(content);
                    return documentContent;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur récupération contenu: {response.StatusCode} - {response.ReasonPhrase}");
                    
                    // Tentative avec l'endpoint /raw en fallback
                    try
                    {
                        var rawResponse = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/raw");
                        if (rawResponse.IsSuccessStatusCode)
                        {
                            var rawContent = await rawResponse.Content.ReadAsStringAsync();
                            var rawData = JsonConvert.DeserializeObject<dynamic>(rawContent);
                            
                            return new DocumentContent
                            {
                                Content = rawData?.raw_content?.ToString() ?? "Contenu non disponible",
                                GitPath = "Chemin non disponible",
                                Version = "Version non disponible",
                                LastModified = DateTime.Now,
                                RepositoryName = "Repository non disponible",
                                FileSizeBytes = rawData?.size_bytes ?? 0
                            };
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Fallback /raw aussi échoué: {fallbackEx.Message}");
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception GetDocumentContentAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<DocumentVersions?> GetDocumentVersionsAsync(string documentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/versions");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ Versions document {documentId}: {content.Length} caractères");
                    
                    var versions = JsonConvert.DeserializeObject<DocumentVersions>(content);
                    return versions;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Erreur récupération versions: {response.StatusCode} - {response.ReasonPhrase}");
                    
                    // Retourner une version vide plutôt que null pour éviter les erreurs UI
                    return new DocumentVersions
                    {
                        DocumentId = documentId,
                        TotalVersions = 0,
                        Versions = new List<DocumentVersion>()
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception GetDocumentVersionsAsync: {ex.Message}");
                
                // Retourner une version vide plutôt que null
                return new DocumentVersions
                {
                    DocumentId = documentId,
                    TotalVersions = 0,
                    Versions = new List<DocumentVersion>()
                };
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