using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TextLabClient.Models;

namespace TextLabClient.Services
{
    public class TextLabApiService
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl = "http://localhost:8000";

        public bool IsConnected { get; private set; }

        public TextLabApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient.BaseAddress = new Uri(_baseUrl);
            IsConnected = false; // Reset connection status
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
                throw; // Re-lancer l'exception pour que l'UI puisse la gérer
            }
        }

        public async Task<List<Repository>?> GetRepositoriesAsync()
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Pas de connexion établie avec l'API");

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/repositories");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var repositories = JsonConvert.DeserializeObject<List<Repository>>(content);
                    return repositories ?? new List<Repository>();
                }
                else
                {
                    throw new HttpRequestException($"Erreur API: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception)
            {
                throw; // Re-lancer pour gestion par l'UI
            }
        }

        public async Task<List<Document>?> GetDocumentsAsync(string? repositoryId = null)
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Pas de connexion établie avec l'API");

                var url = $"{_baseUrl}/api/v1/documents/";
                if (!string.IsNullOrEmpty(repositoryId))
                {
                    url += $"?repository_id={repositoryId}";
                }

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var documents = JsonConvert.DeserializeObject<List<Document>>(content);
                    return documents ?? new List<Document>();
                }
                else
                {
                    throw new HttpRequestException($"Erreur API: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Document?> GetDocumentAsync(string documentId)
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Pas de connexion établie avec l'API");

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var document = JsonConvert.DeserializeObject<Document>(content);
                    return document;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw new HttpRequestException($"Erreur API: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string?> GetDocumentContentAsync(string documentId)
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Pas de connexion établie avec l'API");

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/documents/{documentId}/content");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var contentResponse = JsonConvert.DeserializeObject<dynamic>(content);
                    return contentResponse?.content?.ToString();
                }
                else
                {
                    throw new HttpRequestException($"Erreur API: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Document?> CreateDocumentAsync(string title, string content, string category, string repositoryId, string? filePath = null)
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Pas de connexion établie avec l'API");

                var createRequest = new
                {
                    repository_id = repositoryId,
                    title = title,
                    content = content,
                    category = category,
                    file_path = filePath
                };

                var json = JsonConvert.SerializeObject(createRequest);
                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/v1/documents/", stringContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var document = JsonConvert.DeserializeObject<Document>(responseContent);
                    return document;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Erreur création document: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Document?> UpdateDocumentAsync(string documentId, string title, string content, string category)
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Pas de connexion établie avec l'API");

                var updateRequest = new
                {
                    title = title,
                    content = content,
                    category = category
                };

                var json = JsonConvert.SerializeObject(updateRequest);
                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/api/v1/documents/{documentId}", stringContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var document = JsonConvert.DeserializeObject<Document>(responseContent);
                    return document;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Erreur mise à jour document: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string documentId)
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("Pas de connexion établie avec l'API");

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/v1/documents/{documentId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
} 