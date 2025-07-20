# 🎯 Nouveauté : Racine Configurable des Documents

## 📋 **Vue d'ensemble**

TextLab a introduit une **configuration flexible de la racine des documents** pour chaque repository. Fini les chemins fixes !

### Avant (racine fixe)
```
documents/projects/mon-document.md
documents/guides/tutoriel.md
```

### Maintenant (racine configurable)
```yaml
# .textlab.yaml dans le repository
documents_root: "mes_docs/"
```

Résultat :
```
mes_docs/projects/mon-document.md  
mes_docs/guides/tutoriel.md
```

## 🔧 **Configuration par Repository**

### Fichier `.textlab.yaml`
Chaque repository peut avoir sa propre configuration :

```yaml
# Repository A
documents_root: "documents/"  # Racine classique

# Repository B  
documents_root: "content/"    # Racine personnalisée

# Repository C
documents_root: "mes_docs/"   # Racine française
```

### Valeur par défaut
Si aucune configuration n'est trouvée : `documents/`

## 🔗 **Endpoints API**

### Récupérer la configuration d'un repository
```http
GET /api/v1/repositories/{repository_id}
```

**Réponse avec nouveauté :**
```json
{
  "id": "uuid-repository",
  "name": "mon-repo",
  "type": "github",
  "root_documents": "content/",    ← NOUVEAUTÉ
  "url": "https://github.com/...",
  "is_active": true
}
```

### Construction automatique des chemins
```http
POST /api/v1/documents/
{
  "title": "Mon Document",
  "content": "# Contenu",
  "category": "guides",
  "repository_id": "uuid-repository"
}
```

L'API utilise automatiquement la racine configurée !

## 🖥️ **Client Windows - Modifications**

### Nouveau champ dans Repository.cs
```csharp
[JsonProperty("root_documents")]
public string? RootDocuments { get; set; }

// Propriété avec fallback automatique
public string DocumentsRoot => RootDocuments ?? "documents/";
```

### Construction d'URL GitHub intelligente
```csharp
// AVANT (racine fixe)
var url = $"https://github.com/user/repo/blob/main/{document.GitPath}";

// MAINTENANT (racine dynamique)
var fullRepo = await apiService.GetRepositoryDetailsFullAsync(repositoryId);
var documentsRoot = fullRepo?.DocumentsRoot ?? "documents/";
var url = $"https://github.com/user/repo/blob/main/{documentsRoot}{document.GitPath}";
```

### Nouvelle méthode API
```csharp
// Récupère les détails complets avec la configuration
var repository = await apiService.GetRepositoryDetailsFullAsync(repositoryId);

// Construit l'URL GitHub avec la racine correcte  
var githubUrl = await apiService.BuildGitHubUrlAsync(repository, gitPath);
```

## 🎯 **Exemples Concrets**

### Repository "docs-fr" avec racine "contenu/"
```json
{
  "name": "docs-fr",
  "root_documents": "contenu/"
}
```

Document créé : `guides/installation.md`  
Chemin GitHub : `contenu/guides/installation.md`

### Repository "blog" avec racine "posts/"
```json
{
  "name": "blog", 
  "root_documents": "posts/"
}
```

Document créé : `tutorials/setup.md`  
Chemin GitHub : `posts/tutorials/setup.md`

### Repository sans configuration (fallback)
```json
{
  "name": "default-repo"
  // Pas de root_documents
}
```

Document créé : `projects/demo.md`  
Chemin GitHub : `documents/projects/demo.md` ← fallback automatique

## ✅ **Compatibilité**

### Rétrocompatibilité assurée
- Les repositories existants sans `.textlab.yaml` utilisent `documents/`
- Aucune migration nécessaire
- Le client fonctionne avec les anciennes et nouvelles APIs

### Fallback intelligent
```csharp
// Si l'API ne retourne pas root_documents
var documentsRoot = repository.RootDocuments ?? "documents/";

// Le client continue de fonctionner normalement
```

## 🚀 **Déploiement**

### Côté API
- ✅ Lecture de `.textlab.yaml` dans chaque repository
- ✅ Retour du champ `root_documents` dans les endpoints
- ✅ Construction automatique des chemins lors de la création

### Côté Client Windows
- ✅ Support du champ `root_documents`
- ✅ Construction dynamique des URLs GitHub
- ✅ Fallback automatique pour la compatibilité

---

🎉 **Cette nouveauté permet une flexibilité totale tout en maintenant la simplicité d'utilisation !** 