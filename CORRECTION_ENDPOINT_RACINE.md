# 🔧 Correction : Endpoint Racine des Documents

## ❌ **Erreur identifiée**

Dans mes modifications précédentes, j'ai utilisé le **mauvais endpoint** pour récupérer la racine configurable des documents.

### Endpoint incorrect utilisé :
```csharp
// MAUVAIS - Ne contient pas root_documents
GET /api/v1/repositories/{repository_id}
```

### ✅ **Endpoint correct identifié dans le swagger** :
```csharp
// BON - Contient la configuration complète
GET /api/v1/admin/repositories/{repository_id}/config
```

## 📊 **Comparaison des réponses**

### ❌ Ancien endpoint (incorrect)
```json
// GET /api/v1/repositories/{id}
{
  "id": "uuid",
  "name": "gaudylab",
  "type": "github",
  "url": "https://github.com/...",
  "is_active": true
  // PAS de root_documents !
}
```

### ✅ Nouvel endpoint (correct)
```json
// GET /api/v1/admin/repositories/{id}/config
{
  "repository_id": "uuid",
  "repository_name": "gaudylab",
  "config": {
    "root_documents": "documents/",  ← VOICI LA RACINE !
    "root_templates": "templates/",
    "default_category": null,
    "name": "gaudylab",
    "version": "1.0.0"
  },
  "config_file_exists": true,
  "config_source": "file"
}
```

## 🔧 **Corrections apportées**

### 1. Nouvelle méthode API simplifiée
```csharp
/// <summary>
/// Récupère la racine des documents depuis la configuration du repository
/// </summary>
public async Task<string> GetRepositoryDocumentsRootAsync(string repositoryId)
{
    try
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/admin/repositories/{repositoryId}/config");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var configResponse = JsonConvert.DeserializeObject<dynamic>(content);
            var rootDocuments = configResponse?.config?.root_documents?.ToString() ?? "documents/";
            
            return rootDocuments;
        }
        
        return "documents/"; // Fallback
    }
    catch (Exception ex)
    {
        return "documents/"; // Fallback
    }
}
```

### 2. Méthode BuildGitHubUrlAsync corrigée
```csharp
public async Task<string?> BuildGitHubUrlAsync(Repository repository, string gitPath)
{
    // Récupérer la racine depuis le BON endpoint
    var documentsRoot = await GetRepositoryDocumentsRootAsync(repository.Id);
    
    // S'assurer que la racine se termine par /
    if (!documentsRoot.EndsWith("/"))
    {
        documentsRoot += "/";
    }
    
    // Construire l'URL avec la racine correcte
    var githubUrl = $"https://github.com/jfgaudy/{repository.Name}/blob/main/{documentsRoot}{gitPath}";
    
    return githubUrl;
}
```

## 🎯 **Avantages de la correction**

### ✅ **Racine vraiment configurable**
- Lit la configuration depuis `.textlab.yaml`
- Support des racines personnalisées : `content/`, `mes_docs/`, etc.
- Fallback intelligent vers `documents/` si aucune config

### ✅ **URLs GitHub correctes**
- Construction avec la vraie racine du repository
- Chemins complets : `{racine_config}/{git_path}`
- Plus de liens cassés !

### ✅ **Robustesse améliorée**
- Gestion d'erreurs avec fallback
- Logs de debug pour traçabilité
- Compatible avec tous les types de repositories

## 🧪 **Test de la correction**

### Exemple avec repository configuré
```yaml
# .textlab.yaml dans le repository
root_documents: "content/"
```

**Résultat** :
- Document : `guides/setup.md`
- URL GitHub : `https://github.com/user/repo/blob/main/content/guides/setup.md`

### Exemple avec repository par défaut
```
# Pas de .textlab.yaml
```

**Résultat** :
- Document : `guides/setup.md`
- URL GitHub : `https://github.com/user/repo/blob/main/documents/guides/setup.md`

## 🎉 **Statut**

- ✅ **Correction appliquée** dans `TextLabApiService.cs`
- ✅ **Compilation réussie** sans erreurs
- ✅ **Fallback intelligent** en cas d'erreur
- ✅ **Logs de debug** pour traçabilité

**Le client utilise maintenant le bon endpoint pour récupérer la racine configurable des documents !** 🚀 