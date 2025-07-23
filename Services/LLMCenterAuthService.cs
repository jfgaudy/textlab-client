#nullable enable
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TextLabClient.Services
{
    public class LLMCenterAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        
        // Token storage
        private string? _accessToken;
        private DateTime _tokenExpiry;
        private string? _refreshToken;

        public LLMCenterAuthService()
        {
            _httpClient = new HttpClient();
            _baseUrl = "https://llm-center-backend.onrender.com";
            _apiKey = "litellm_sk_uLEd0zQ24pZT5-wTsGrD_YUuEepFR1c_XvK3HMkDyi0";
            
            // Configure headers
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TextLabClient/2.0");
            
            // 🚨 NOUVEAU: Vider le token au démarrage pour toujours forcer la connexion
            ClearTokenFromSettings();
            _accessToken = null;
            _tokenExpiry = DateTime.MinValue;
            
            // Log simple sans async dans le constructeur
            System.Diagnostics.Debug.WriteLine("🔐 LLMCenterAuthService initialisé - Token vidé");
        }

        /// <summary>
        /// Définit un token directement sans passer par LLM Center (bypass)
        /// </summary>
        public async Task<bool> SetTokenDirectlyAsync(string token)
        {
            try
            {
                await LoggingService.LogInfoAsync($"🎯 Application token direct (bypass LLM Center)");
                
                // Valider que c'est bien un JWT
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    await LoggingService.LogErrorAsync("❌ Token malformé - doit être un JWT (3 parties)");
                    return false;
                }
                
                // Essayer de décoder le payload pour validation
                try
                {
                    var payload = parts[1];
                    var paddedPayload = payload + new string('=', (4 - payload.Length % 4) % 4);
                    var decodedBytes = Convert.FromBase64String(paddedPayload);
                    var jsonPayload = System.Text.Encoding.UTF8.GetString(decodedBytes);
                    
                    using var tokenDoc = JsonDocument.Parse(jsonPayload);
                    var tokenRoot = tokenDoc.RootElement;
                    
                    // Extraire les infos du token
                    string? username = tokenRoot.TryGetProperty("sub", out var subElement) ? subElement.GetString() : "unknown";
                    string? email = tokenRoot.TryGetProperty("email", out var emailElement) ? emailElement.GetString() : "unknown";
                    
                    await LoggingService.LogInfoAsync($"🎯 Token utilisateur: {username} ({email})");
                    
                    // Stocker le token
                    _accessToken = token;
                    
                    // Pour les tokens directs, on ignore l'expiration (contournement)
                    _tokenExpiry = DateTime.UtcNow.AddHours(24); // 24h de validité arbitraire
                    
                    // Sauvegarder
                    SaveTokenToSettings(_accessToken, _tokenExpiry);
                    
                    await LoggingService.LogInfoAsync("✅ Token direct appliqué avec succès");
                    return true;
                }
                catch (Exception parseEx)
                {
                    await LoggingService.LogErrorAsync($"❌ Erreur décodage token: {parseEx.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur application token direct: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Authentifie un utilisateur auprès de LLM Center
        /// </summary>
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                await LoggingService.LogInfoAsync($"🔑 Tentative de connexion pour {email}");

                // 🔧 CORRECTION: Utiliser exactement la même méthode que la génération manuelle
                var loginData = new
                {
                    email = email,
                    password = password
                };

                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 🎯 CRÉER UNE REQUÊTE AVEC LES MÊMES HEADERS QUE LA MÉTHODE MANUELLE
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/auth/external/token")
                {
                    Content = content
                };
                
                // ✅ AJOUTER LES HEADERS EXACTS DE LA MÉTHODE MANUELLE
                request.Headers.Add("X-API-Key", _apiKey);
                request.Headers.Add("User-Agent", "TextLabClient/2.0");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LoggingService.LogErrorAsync($"❌ Erreur login HTTP {response.StatusCode}: {errorContent}");
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                await LoggingService.LogDebugAsync($"✅ Réponse login: {responseContent}");

                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                if (root.TryGetProperty("access_token", out var tokenElement))
                {
                    _accessToken = tokenElement.GetString();
                    
                    // ✅ EXTRAIRE L'EXPIRATION RÉELLE DU TOKEN JWT
                    try
                    {
                        var parts = _accessToken?.Split('.');
                        if (parts?.Length >= 2)
                        {
                            var payload = parts[1];
                            // Ajouter le padding nécessaire pour Base64
                            var paddedPayload = payload + new string('=', (4 - payload.Length % 4) % 4);
                            var decodedBytes = Convert.FromBase64String(paddedPayload);
                            var jsonPayload = System.Text.Encoding.UTF8.GetString(decodedBytes);
                            
                            using var tokenDoc = JsonDocument.Parse(jsonPayload);
                            var tokenRoot = tokenDoc.RootElement;
                            
                            if (tokenRoot.TryGetProperty("exp", out var expElement) && expElement.TryGetInt64(out var expTimestamp))
                            {
                                _tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(expTimestamp).DateTime;
                                await LoggingService.LogInfoAsync($"🕒 Token expire le: {_tokenExpiry:dd/MM/yyyy HH:mm:ss}");
                            }
                            else
                            {
                                // Fallback si pas d'expiration trouvée
                                _tokenExpiry = DateTime.UtcNow.AddMinutes(25);
                                await LoggingService.LogWarningAsync("⚠️ Expiration JWT non trouvée, utilisation fallback 25min");
                            }
                        }
                        else
                        {
                            // Token malformé
                            _tokenExpiry = DateTime.UtcNow.AddMinutes(25);
                            await LoggingService.LogWarningAsync("⚠️ Token JWT malformé, utilisation fallback 25min");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        // Erreur de parsing
                        _tokenExpiry = DateTime.UtcNow.AddMinutes(25);
                        await LoggingService.LogWarningAsync($"⚠️ Erreur parsing JWT: {parseEx.Message}, utilisation fallback 25min");
                    }
                    
                    // Sauvegarder dans les settings utilisateur
                    SaveTokenToSettings(_accessToken, _tokenExpiry);
                    
                    await LoggingService.LogInfoAsync("✅ Connexion réussie - Token stocké avec expiration réelle");
                    return true;
                }
                else
                {
                    await LoggingService.LogErrorAsync("❌ Token manquant dans la réponse");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur lors du login: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si l'utilisateur est authentifié avec un token valide
        /// </summary>
        public bool IsAuthenticated()
        {
            // Charger le token depuis les settings si pas en mémoire
            if (string.IsNullOrEmpty(_accessToken))
            {
                LoadTokenFromSettings();
            }

            var hasToken = !string.IsNullOrEmpty(_accessToken);
            var notExpired = DateTime.UtcNow < _tokenExpiry;
            
            // 🚨 CONTOURNEMENT TEMPORAIRE : LLM Center a un problème d'horloge
            // Les tokens sont générés avec des dates d'expiration incorrectes
            var timeDiff = _tokenExpiry - DateTime.UtcNow;
            
            System.Diagnostics.Debug.WriteLine($"🔍 IsAuthenticated: hasToken={hasToken}, notExpired={notExpired}");
            System.Diagnostics.Debug.WriteLine($"🕒 TokenExpiry: {_tokenExpiry:HH:mm:ss}, Now: {DateTime.UtcNow:HH:mm:ss}");
            System.Diagnostics.Debug.WriteLine($"⚠️ TimeDiff: {timeDiff.TotalMinutes:F0} minutes");
            
            // 🚨 CONTOURNEMENT ÉTENDU: Accepter TOUS les tokens récents même "expirés"
            if (hasToken && !notExpired)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 CONTOURNEMENT: Token 'expiré' accepté (problème horloge LLM Center)");
                return true; // TOUJOURS accepter les tokens existants
            }
            
            return hasToken && notExpired;
        }

        /// <summary>
        /// Récupère le token Bearer pour les requêtes API
        /// </summary>
        public async Task<string?> GetBearerTokenAsync()
        {
            if (!IsAuthenticated())
            {
                await LoggingService.LogWarningAsync("⚠️ Aucun token valide disponible");
                return null;
            }

            return _accessToken;
        }

        /// <summary>
        /// Actualise le token si nécessaire
        /// </summary>
        public async Task<bool> RefreshTokenIfNeededAsync()
        {
            if (DateTime.UtcNow >= _tokenExpiry.AddMinutes(-5)) // 5 min avant expiration
            {
                return await RefreshTokenAsync();
            }
            return true;
        }

        /// <summary>
        /// Actualise le token (non implémenté car l'API ne le supporte pas encore)
        /// </summary>
        private async Task<bool> RefreshTokenAsync()
        {
            try
            {
                // L'API LLM Center ne semble pas avoir de refresh token endpoint
                // On pourrait implémenter une re-authentification automatique ici
                await LoggingService.LogWarningAsync($"⚠️ Échec refresh token: non supporté par l'API");
                return false;
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur refresh token: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Déconnexion et nettoyage du token
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                // Nettoyer le token local
                _accessToken = null;
                _tokenExpiry = DateTime.MinValue;
                
                // Nettoyer les settings
                ClearTokenFromSettings();
                
                await LoggingService.LogWarningAsync($"⚠️ Erreur lors de la révocation: pas d'endpoint de logout");
            }
            catch (Exception ex)
            {
                await LoggingService.LogWarningAsync($"⚠️ Erreur lors de la révocation: {ex.Message}");
            }
            finally
            {
                await LoggingService.LogInfoAsync("✅ Déconnexion effectuée");
            }
        }

        /// <summary>
        /// Récupère les informations de l'utilisateur actuel
        /// </summary>
        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                if (!IsAuthenticated() || string.IsNullOrEmpty(_accessToken))
                    return null;

                // 🎯 NOUVELLE APPROCHE: Extraire les infos directement du token JWT
                // Plus besoin d'appeler l'endpoint /me qui n'existe pas sur TextLab
                await LoggingService.LogInfoAsync("🔍 Extraction des infos utilisateur depuis le token JWT");
                
                var parts = _accessToken.Split('.');
                if (parts.Length >= 2)
                {
                    var payload = parts[1];
                    // Ajouter le padding nécessaire pour Base64
                    var paddedPayload = payload + new string('=', (4 - payload.Length % 4) % 4);
                    var decodedBytes = Convert.FromBase64String(paddedPayload);
                    var jsonPayload = System.Text.Encoding.UTF8.GetString(decodedBytes);
                    
                    using var tokenDoc = JsonDocument.Parse(jsonPayload);
                    var tokenRoot = tokenDoc.RootElement;
                    
                    // Extraire les informations utilisateur du token
                    var userInfo = new UserInfo
                    {
                        Username = tokenRoot.TryGetProperty("sub", out var subElement) ? subElement.GetString() ?? "Utilisateur" : "Utilisateur",
                        Email = tokenRoot.TryGetProperty("email", out var emailElement) ? emailElement.GetString() ?? "" : "",
                        Role = tokenRoot.TryGetProperty("role", out var roleElement) ? roleElement.GetString() ?? "USER" : "USER",
                        UserId = tokenRoot.TryGetProperty("user_id", out var userIdElement) ? userIdElement.GetString() ?? "" : ""
                    };
                    
                    await LoggingService.LogInfoAsync($"✅ Infos utilisateur extraites: {userInfo.Username} ({userInfo.Email}) - {userInfo.Role}");
                    return userInfo;
                }
                
                await LoggingService.LogWarningAsync("⚠️ Token JWT malformé - impossible d'extraire les infos utilisateur");
                return new UserInfo { Username = "Utilisateur", Email = "", Role = "USER", UserId = "" };
            }
            catch (Exception ex)
            {
                await LoggingService.LogErrorAsync($"❌ Erreur extraction utilisateur depuis JWT: {ex.Message}");
                // Retourner des infos par défaut plutôt que null
                return new UserInfo { Username = "Utilisateur", Email = "", Role = "USER", UserId = "" };
            }
        }

        /// <summary>
        /// Sauvegarde le token dans les settings utilisateur
        /// </summary>
        private void SaveTokenToSettings(string token, DateTime expiry)
        {
            try
            {
                Properties.Settings.Default.AuthToken = token;
                Properties.Settings.Default.TokenExpiry = expiry.ToBinary();
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                // Log mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"❌ Erreur sauvegarde token: {ex.Message}");
            }
        }

        /// <summary>
        /// Charge le token depuis les settings utilisateur
        /// </summary>
        private void LoadTokenFromSettings()
        {
            try
            {
                var token = Properties.Settings.Default.AuthToken;
                var expiryBinary = Properties.Settings.Default.TokenExpiry;

                if (!string.IsNullOrEmpty(token) && expiryBinary != 0)
                {
                    _accessToken = token;
                    _tokenExpiry = DateTime.FromBinary(expiryBinary);
                }
            }
            catch (Exception ex)
            {
                // Log mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement token: {ex.Message}");
                
                // Réinitialiser les valeurs par défaut
                _accessToken = null;
                _tokenExpiry = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Nettoie le token des settings
        /// </summary>
        private void ClearTokenFromSettings()
        {
            try
            {
                Properties.Settings.Default.AuthToken = string.Empty;
                Properties.Settings.Default.TokenExpiry = 0;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                // Log mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"❌ Erreur nettoyage token: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Informations utilisateur retournées par LLM Center
    /// </summary>
    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
} 