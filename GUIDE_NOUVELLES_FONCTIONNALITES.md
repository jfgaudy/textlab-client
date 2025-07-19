# 🚀 Guide des Nouvelles Fonctionnalités API TextLab

## 📋 Vue d'Ensemble

Le client TextLab a été mis à jour pour supporter toutes les nouvelles fonctionnalités de l'API v2 avec **architecture adaptative**. Cette mise à jour apporte une gestion complète des repositories multiples, la synchronisation Git, et de nombreuses améliorations.

## 🆕 Nouvelles Fonctionnalités Principales

### 1. **🗂️ Gestion Multi-Repositories**

#### Accès
- **Menu** : `Repositories > Gestion des Repositories`
- **Fonctionnalités** :
  - ✅ Création de repositories locaux et GitHub
  - ✅ Activation/Désactivation des repositories
  - ✅ Configuration du repository par défaut
  - ✅ Validation avant configuration
  - ✅ Suppression sécurisée

#### Utilisation
1. Cliquez sur `Repositories > Gestion des Repositories`
2. Onglet **Configuration** pour créer un nouveau repository
3. Choisissez le type (Local/GitHub)
4. Configurez les paramètres
5. Cliquez sur **Valider** puis **Créer Repository**

### 2. **🔄 Synchronisation Git Avancée**

#### Fonctionnalités Pull
- **Pull individuel** : Synchronise un repository spécifique
- **Pull global** : `Repositories > Synchroniser Tous`
- **Résolution automatique** des conflits (optionnelle)
- **Statut de synchronisation** en temps réel

#### Utilisation
1. **Pull individuel** : Sélectionnez un repository → `Pull`
2. **Pull global** : Menu `Repositories > Synchroniser Tous`
3. **Surveillance** : Onglet `Synchronisation` pour voir les statuts

### 3. **🔐 Gestion des Credentials**

#### Configuration Sécurisée
- Credentials chiffrés côté serveur
- Support GitHub Personal Access Tokens
- Configuration par repository

#### Utilisation
1. Dans la gestion des repositories
2. Sélectionnez un repository
3. Cliquez sur `🔐 Credentials`
4. Entrez username et token
5. Sauvegarde automatique chiffrée

### 4. **📊 Diagnostics et Monitoring**

#### Outils Disponibles
- **Architecture Diagnostics** : État de l'architecture adaptative
- **Statistiques Environnement** : Métriques système
- **Health Check** : Vérification de santé complète
- **Statut Système** : État général du serveur

#### Utilisation
1. Onglet **Diagnostics** dans la gestion des repositories
2. Boutons pour chaque type de diagnostic
3. Affichage JSON formaté dans la console

## 🛠️ Nouveaux Services et API

### TextLabAdminService
Nouveau service pour les opérations d'administration :

```csharp
var adminService = new TextLabAdminService();

// Gestion des repositories
var repos = await adminService.GetRepositoriesAsync();
await adminService.ActivateRepositoryAsync(repoId);
await adminService.SetDefaultRepositoryAsync(repoId);

// Synchronisation
var pullResponse = await adminService.PullRepositoryAsync(repoId);
var pullStatus = await adminService.GetPullStatusAsync();

// Configuration
await adminService.ConfigureLocalRepositoryAsync(config);
await adminService.ConfigureGitHubRepositoryAsync(config);
```

### Extensions TextLabApiService
Nouvelles méthodes pour les endpoints publics :

```csharp
// Nouveaux endpoints publics
var repos = await apiService.GetPublicRepositoriesAsync();
var repoDetails = await apiService.GetRepositoryDetailsAsync(repoId);
var docCount = await apiService.GetRepositoryDocumentCountAsync(repoId);

// Nouvelles fonctionnalités documents
await apiService.UpdateDocumentAsync(docId, author, updateData);
await apiService.DeleteDocumentAsync(docId, author, softDelete: true);
await apiService.ArchiveDocumentAsync(docId, author, reason);

// Diagnostics
var diagnostics = await apiService.GetArchitectureDiagnosticsAsync();
var envStats = await apiService.GetEnvironmentStatsAsync();
var health = await apiService.GetDocumentsHealthAsync();
```

## 🎯 Modèles de Données Étendus

### Repository (Étendu)
```csharp
public class Repository
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // "local", "github"
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Propriétés Git
    public string? CurrentCommitSha { get; set; }
    public bool HasCredentials { get; set; }
    public DateTime? LastPullDate { get; set; }
    
    // Propriétés UI
    public string DisplayName { get; }
    public string TypeDisplay { get; }
    public string StatusDisplay { get; }
}
```

### Nouveaux Modèles Pull
```csharp
public class PullStatus
{
    public bool CanPull { get; set; }
    public int BehindCommits { get; set; }
    public int AheadCommits { get; set; }
    public bool LocalChanges { get; set; }
    public string StatusMessage { get; set; }
}

public class PullResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public PullChanges Changes { get; set; }
    public string? Error { get; set; }
}
```

## 🔧 Configuration Repository

### Repository Local
```csharp
var localConfig = new LocalRepoConfig
{
    RepoPath = @"C:\MonProjet\textlab-docs",
    Name = "Documentation Locale",
    Description = "Repository de documentation",
    ValidateStructure = true
};

await adminService.ConfigureLocalRepositoryAsync(localConfig);
```

### Repository GitHub
```csharp
var githubConfig = new GitHubRepoConfig
{
    RepoUrl = "https://github.com/user/repository",
    LocalPath = @"C:\Repos\github-docs",
    Name = "Docs GitHub",
    BranchName = "main",
    CloneIfMissing = true
};

await adminService.ConfigureGitHubRepositoryAsync(githubConfig);
```

## 🎨 Interface Utilisateur

### Nouvelle Fenêtre : RepositoryManagementWindow
- **4 Onglets** : Repositories, Configuration, Synchronisation, Diagnostics
- **Interface moderne** avec boutons colorés et feedback visuel
- **Opérations en temps réel** avec statut dans la barre inférieure
- **Validation** avant toute opération critique

### Menu Principal Étendu
- **Nouveau menu "Repositories"** avec :
  - Gestion des Repositories
  - Synchroniser Tous

## 🚦 Flux de Travail Recommandé

### 1. **Configuration Initiale**
1. Ouvrir `Repositories > Gestion des Repositories`
2. Créer vos repositories (local/GitHub)
3. Définir le repository par défaut
4. Configurer les credentials si nécessaire

### 2. **Utilisation Quotidienne**
1. Synchroniser automatiquement au démarrage
2. Utiliser le pull global régulièrement
3. Surveiller les statuts de synchronisation
4. Créer des documents dans le bon repository

### 3. **Maintenance**
1. Vérifier les diagnostics périodiquement
2. Surveiller les conflits de merge
3. Mettre à jour les credentials si expiré

## 🔍 Dépannage

### Problèmes Courants

**Repository non accessible**
- Vérifier les credentials
- Valider le chemin local
- Tester la connexion réseau

**Conflits de merge**
- Utiliser le pull avec résolution automatique
- Ou résoudre manuellement via Git

**Synchronisation échouée**
- Vérifier les diagnostics
- Contrôler l'état système
- Réactiver le repository si nécessaire

### Logs et Debug
- Tous les logs sont sauvegardés automatiquement
- Utiliser les diagnostics pour identifier les problèmes
- Health check pour vérifier l'état général

## 📈 Avantages de la Nouvelle Architecture

### Pour les Développeurs
- **API standardisée** avec endpoints RESTful
- **Architecture adaptative** qui s'adapte à l'environnement
- **Gestion d'erreurs robuste** avec messages explicites
- **Monitoring intégré** pour le débogage

### Pour les Utilisateurs
- **Interface unifiée** pour tous les repositories
- **Synchronisation intelligente** avec résolution automatique
- **Feedback visuel** en temps réel
- **Sécurité renforcée** avec credentials chiffrés

## 🔮 Prochaines Étapes

Cette intégration des nouvelles fonctionnalités API prépare le client pour :
- Support de repositories Git avancés (GitLab, Bitbucket)
- Collaboration en temps réel
- Synchronisation bi-directionnelle
- Interface de résolution de conflits avancée

---

**Profitez des nouvelles fonctionnalités !** 🎉

Pour plus d'informations, consultez la [documentation API](https://textlab-api.onrender.com/docs). 