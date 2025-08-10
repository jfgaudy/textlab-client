# 🎯 Demande API : Vue Hiérarchique par Tags

## 📋 Contexte

L'implémentation actuelle de la vue hiérarchique par tags présente des problèmes de performance et d'UX :

### ❌ Problèmes actuels :
- **Performance** : 1 appel API par document (`GetDocumentTagsAsync`) = très lent
- **UX** : Perte du repository sélectionné lors du changement de vue
- **Efficacité** : Appels multiples pour reconstituer une hiérarchie

## 🎯 Proposition d'API

### Endpoint demandé :
```
GET /api/v1/repositories/{repositoryId}/tags/hierarchy
```

### Réponse attendue :
```json
{
  "repository": {
    "id": "uuid",
    "name": "gaudylab"
  },
  "hierarchy": [
    {
      "type": "client",
      "type_display_name": "Clients",
      "type_icon": "🏢",
      "tags": [
        {
          "id": "tag-uuid-1",
          "name": "PAC",
          "slug": "pac",
          "color": "#FF5722",
          "icon": "🏢",
          "document_count": 2,
          "documents": [
            {
              "id": "doc-uuid-1",
              "title": "Pac proposition for TextLab",
              "path": "docs/pac_proposal.md",
              "last_modified": "2025-01-05T10:30:00Z"
            }
          ]
        }
      ]
    },
    {
      "type": "technology",
      "type_display_name": "Technologies",
      "type_icon": "⚙️",
      "tags": [
        {
          "id": "tag-uuid-2", 
          "name": "TextLab",
          "slug": "textlab",
          "color": "#2196F3",
          "icon": "📝",
          "document_count": 3,
          "documents": [
            {
              "id": "doc-uuid-2",
              "title": "jeff test du 24",
              "path": "tests/jeff_test.md",
              "last_modified": "2025-01-04T15:20:00Z"
            }
          ]
        }
      ]
    }
  ],
  "total_documents": 5,
  "total_tags": 4
}
```

## ✅ Avantages :

1. **⚡ Performance** : 1 seul appel API au lieu de N+1
2. **🎯 Précision** : Données spécifiques au repository sélectionné
3. **📊 Complétude** : Tags + documents + métadonnées en une fois
4. **🔧 Maintenabilité** : Logique serveur centralisée
5. **🚀 Scalabilité** : Optimisations possibles côté serveur (cache, index)

## 🛠️ Implémentation côté client :

Remplace la logique actuelle :
```csharp
// AVANT : Multiple appels
var docs = await GetDocumentsAsync(repoId);
foreach(doc in docs) {
  var tags = await GetDocumentTagsAsync(doc.Id); // N appels !
}

// APRÈS : Un seul appel
var hierarchy = await GetRepositoryTagHierarchyAsync(repoId);
```

## 🎯 Priorité : **HAUTE**

Cette API résoudrait immédiatement :
- ❌ Lenteur actuelle 
- ❌ Complexité du code client
- ❌ Problèmes de synchronisation repository/vue
- ✅ UX fluide pour la navigation par tags

---

**Question pour l'équipe serveur :** 
Pouvez-vous implémenter cet endpoint ? Cela transformerait la vue hiérarchique d'un hack client lourd en une fonctionnalité native efficace.