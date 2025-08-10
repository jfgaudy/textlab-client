# 🚀 Demande API : Pagination et Lazy Loading pour Hiérarchie Tags

## 📋 Contexte

L'endpoint `/repositories/{id}/tags/hierarchy` fonctionne parfaitement, mais nous devons l'optimiser pour les gros repositories qui peuvent avoir :
- **Milliers de documents** par repository
- **Centaines de tags** par type
- **Dizaines de documents** par tag

## 🎯 Problèmes de Performance Identifiés

### 🐌 Scénarios Problématiques :
1. **Repository de 10,000+ documents** → Réponse JSON de plusieurs MB
2. **100+ tags par type** → Interface surchargée 
3. **Interface bloquée** pendant le chargement complet
4. **Mémoire client saturée** avec toutes les données

## 💡 Solutions Proposées

### 1. 🔢 PAGINATION AU NIVEAU TAG
Ajouter des paramètres de pagination pour limiter le nombre de tags par type :

**Endpoint amélioré :**
```
GET /repositories/{repository_id}/tags/hierarchy?tag_limit=50&tag_offset=0
```

**Paramètres :**
- `tag_limit` : Nombre maximum de tags par type (défaut: 50, max: 200)
- `tag_offset` : Offset pour pagination des tags (défaut: 0)

**Réponse modifiée :**
```json
{
  "repository": {...},
  "hierarchy": [
    {
      "type": "client",
      "type_display_name": "Clients",
      "type_icon": "🏢",
      "total_tags": 150,          // ← NOUVEAU : Total des tags de ce type
      "displayed_tags": 50,       // ← NOUVEAU : Tags dans cette réponse
      "has_more": true,           // ← NOUVEAU : Y a-t-il plus de tags ?
      "tags": [...]               // ← Limité à tag_limit items
    }
  ],
  "pagination": {                 // ← NOUVEAU : Métadonnées pagination
    "tag_limit": 50,
    "tag_offset": 0,
    "total_types": 4
  }
}
```

### 2. 📄 LAZY LOADING DES DOCUMENTS
Les documents ne sont chargés QUE quand l'utilisateur expand un tag :

**Endpoint spécialisé :**
```
GET /repositories/{repository_id}/tags/{tag_id}/documents?limit=20&offset=0
```

**Hiérarchie sans documents :**
```json
{
  "tags": [
    {
      "id": "tag-uuid",
      "name": "PAC",
      "document_count": 234,     // ← Compte total
      "documents": null          // ← Null = pas encore chargé
    }
  ]
}
```

**Chargement à la demande :**
```json
{
  "tag_id": "tag-uuid",
  "documents": [...],           // ← Documents paginés
  "pagination": {
    "limit": 20,
    "offset": 0,
    "total": 234,
    "has_more": true
  }
}
```

### 3. 🎯 MODE COMPACT
Mode par défaut avec données minimales :

**Paramètre :**
```
GET /repositories/{repository_id}/tags/hierarchy?mode=compact
```

**Réponse compacte :**
```json
{
  "hierarchy": [
    {
      "type": "client",
      "tags": [
        {
          "id": "uuid",
          "name": "PAC",
          "document_count": 234,
          "documents": null     // ← Pas de documents en mode compact
        }
      ]
    }
  ]
}
```

### 4. 📊 STREAMING POUR TRÈS GROS REPOS
Pour les repositories massifs (50k+ documents) :

**Endpoint streaming :**
```
GET /repositories/{repository_id}/tags/hierarchy/stream
```

**Réponse Server-Sent Events (SSE) :**
```
data: {"type": "type_start", "type_name": "client"}
data: {"type": "tag", "tag": {...}, "document_count": 100}
data: {"type": "tag", "tag": {...}, "document_count": 200}
data: {"type": "type_end", "type_name": "client"}
```

## 🔧 Implémentation Côté Client

### A. Interface Progressive
```csharp
// 1. Charger structure de base (mode compact)
var hierarchy = await GetRepositoryTagHierarchyAsync(repoId, compact: true);

// 2. Afficher l'arbre avec placeholders
PopulateTreeWithHierarchy(hierarchy);

// 3. Charger documents à la demande (au clic expand)
private async void TagExpanded(object sender, ExpandedEventArgs e)
{
    var tag = (TagWithDocuments)e.Item.Tag;
    if (tag.Documents == null) // Pas encore chargé
    {
        tag.Documents = await GetTagDocumentsAsync(tag.Id);
        RefreshTagDisplay(tag);
    }
}
```

### B. Cache Intelligent
```csharp
private readonly Dictionary<string, List<Document>> _documentCache = new();

private async Task<List<Document>> GetTagDocumentsAsync(string tagId)
{
    if (_documentCache.ContainsKey(tagId))
        return _documentCache[tagId];
        
    var docs = await _apiService.GetTagDocumentsAsync(tagId);
    _documentCache[tagId] = docs;
    return docs;
}
```

### C. Pagination UI
```xaml
<!-- Bouton "Charger plus" en bas de chaque type -->
<Button Name="LoadMoreTagsButton" 
        Content="📄 Charger 50 tags de plus..." 
        Click="LoadMoreTags_Click" />
```

## 🎯 Avantages

### ⚡ Performance
- **Temps de réponse** : < 500ms au lieu de 5-10s
- **Taille réponse** : ~50KB au lieu de 5MB
- **Mémoire client** : ~10MB au lieu de 100MB+

### 🎨 UX
- **Chargement progressif** : Interface réactive immédiatement
- **Pagination fluide** : "Charger plus" intuitif
- **Cache intelligent** : Pas de rechargement inutile

### 📈 Scalabilité
- **Support 100k+ documents** par repository
- **1000+ tags** par type gérables
- **Croissance linéaire** des performances

## 📊 Métriques de Succès

- **< 1s** pour affichage initial de la hiérarchie
- **< 200ms** pour expansion d'un tag
- **< 50MB** d'utilisation mémoire max côté client
- **Support repositories** jusqu'à 100k documents

## 🔄 Migration Douce

1. **Phase 1** : Ajouter paramètres optionnels (backward compatible)
2. **Phase 2** : Client utilise nouveaux paramètres
3. **Phase 3** : Mode compact par défaut
4. **Phase 4** : Déprécier mode legacy (optionnel)

## 🚀 Priorité

**HAUTE** - Cette optimisation débloquera l'utilisation sur les vrais repositories de production.

---

**Question pour l'équipe serveur :** 
Quelle approche préférez-vous ? Pagination tags + lazy loading documents semble le plus équilibré ?