using System;
using System.Windows;
using System.Windows.Input;
using TextLabClient.Services;

namespace TextLabClient
{
    public partial class LoginWindow : Window
    {
        private readonly LLMCenterAuthService _authService;
        private bool _loginSuccessful = false;

        public bool LoginSuccessful => _loginSuccessful;

        public LoginWindow(LLMCenterAuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            
            // Focus sur le champ email ou password selon ce qui est rempli
            if (string.IsNullOrEmpty(EmailTextBox.Text))
            {
                EmailTextBox.Focus();
            }
            else
            {
                PasswordBox.Focus();
            }
            
            // Gérer Enter dans PasswordBox
            PasswordBox.KeyDown += PasswordBox_KeyDown;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformLoginAsync();
        }

        private async System.Threading.Tasks.Task PerformLoginAsync()
        {
            try
            {
                // 🎯 NOUVELLE LOGIQUE: Vérifier quel mode utiliser
                string tokenDirect = TokenTextBox.Text.Trim();
                
                if (!string.IsNullOrWhiteSpace(tokenDirect))
                {
                    // MODE TOKEN DIRECT - Bypass LLM Center
                    ShowLoading("Application du token direct...");
                    
                    // Sauvegarder directement le token sans appeler LLM Center
                    bool tokenSet = await _authService.SetTokenDirectlyAsync(tokenDirect);
                    
                    if (tokenSet)
                    {
                        ShowSuccess("Token appliqué avec succès ! Connexion directe.");
                        await System.Threading.Tasks.Task.Delay(1000);
                        _loginSuccessful = true;
                        DialogResult = true;
                        Close();
                        return;
                    }
                    else
                    {
                        ShowError("Erreur: Token JWT invalide ou malformé");
                        return;
                    }
                }
                else
                {
                    // MODE AUTHENTIFICATION CLASSIQUE LLM CENTER
                    // Validation des champs
                    if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                    {
                        ShowError("Veuillez saisir votre email OU un token direct");
                        EmailTextBox.Focus();
                        return;
                    }

                    if (string.IsNullOrEmpty(PasswordBox.Password))
                    {
                        ShowError("Veuillez saisir votre mot de passe OU un token direct");
                        PasswordBox.Focus();
                        return;
                    }

                    // Affichage du loading
                    ShowLoading("Connexion LLM Center en cours...");

                    // Tentative de connexion
                    var success = await _authService.LoginAsync(EmailTextBox.Text.Trim(), PasswordBox.Password);

                    if (success)
                    {
                        // Récupérer les infos utilisateur pour affichage
                        var userInfo = await _authService.GetCurrentUserAsync();
                        
                        ShowSuccess($"Connexion LLM Center réussie ! Bienvenue {userInfo?.Username ?? "utilisateur"}");
                        
                        // Attendre un peu pour que l'utilisateur voit le message
                        await System.Threading.Tasks.Task.Delay(1000);
                        
                        _loginSuccessful = true;
                        
                        // Sauvegarder les credentials si demandé
                        if (RememberMeCheckBox.IsChecked == true)
                        {
                            SaveCredentials();
                        }
                        else
                        {
                            ClearSavedCredentials();
                        }
                        
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        ShowError("Échec connexion LLM Center. Vérifiez vos identifiants ou utilisez un token direct.");
                        PasswordBox.Clear();
                        PasswordBox.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erreur de connexion: {ex.Message}");
            }
            finally
            {
                HideLoading();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _loginSuccessful = false;
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            StatusTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowSuccess(string message)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
            StatusTextBlock.Visibility = Visibility.Visible;
        }

        private void ShowLoading(string message)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
            StatusTextBlock.Visibility = Visibility.Visible;
            LoadingProgressBar.Visibility = Visibility.Visible;
            
            // Désactiver les contrôles pendant le chargement
            LoginButton.IsEnabled = false;
            EmailTextBox.IsEnabled = false;
            PasswordBox.IsEnabled = false;
        }

        private void HideLoading()
        {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            
            // Réactiver les contrôles
            LoginButton.IsEnabled = true;
            EmailTextBox.IsEnabled = true;
            PasswordBox.IsEnabled = true;
        }

        private void SaveCredentials()
        {
            try
            {
                // Sauvegarder seulement l'email (pas le mot de passe pour la sécurité)
                if (Properties.Settings.Default != null)
                {
                    Properties.Settings.Default.LastUserEmail = EmailTextBox.Text.Trim();
                    Properties.Settings.Default.RememberUser = true;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                // Log mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"❌ Erreur sauvegarde credentials: {ex.Message}");
            }
        }

        private void ClearSavedCredentials()
        {
            try
            {
                if (Properties.Settings.Default != null)
                {
                    Properties.Settings.Default.LastUserEmail = string.Empty;
                    Properties.Settings.Default.RememberUser = false;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                // Log mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"❌ Erreur nettoyage credentials: {ex.Message}");
            }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                // Vérification de sécurité pour éviter les erreurs d'initialisation
                if (Properties.Settings.Default != null && Properties.Settings.Default.RememberUser)
                {
                    var savedEmail = Properties.Settings.Default.LastUserEmail;
                    if (!string.IsNullOrEmpty(savedEmail))
                    {
                        EmailTextBox.Text = savedEmail;
                        RememberMeCheckBox.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log mais ne pas bloquer l'application
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement credentials: {ex.Message}");
                // Réinitialiser aux valeurs par défaut
                EmailTextBox.Text = "jfgaudy@outlook.com";
                RememberMeCheckBox.IsChecked = false;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Temporairement désactivé pour éviter les blocages
            // LoadSavedCredentials();
        }
    }
} 