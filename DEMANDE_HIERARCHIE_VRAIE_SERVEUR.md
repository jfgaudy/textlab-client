# 🌳 DEMANDE : Hiérarchie Vraie des Tags (Parent-Enfant)

## ✅ AVANCEMENT

✅ **Endpoint optimisé** `/repositories/{id}/tags/hierarchy` implémenté - MERCI !  
❌ **Hiérarchie parent-enfant** manque encore (toujours groupé par catégories)

## 🎯 PROBLÈME RESTANT

L'endpoint `/repositories/{id}/tags/hierarchy` retourne les tags **groupés par catégorie** au lieu de la **vraie hiérarchie parent-enfant**.

### ❌ Structure Actuelle
```json
{
  "hierarchy": [
    {
      "type": "client",
      "tags": ["PAC"]
    },
    {
      "type": "custom", 
      "tags": ["AITM", "text lab", "Sales proposals", "poem"]
    }
  ]
}
```

### ✅ Structure Souhaitée
```json
{
  "hierarchy": [
    {
      "id": "technical-specs-id",
      "name": "Technical specs",
      "type": "technology",
      "parent_id": null,
      "level": 0,
      "path": "Technical specs",
      "document_count": 0,  // Peut être 0 si aucun doc direct
      "children": [
        {
          "id": "text-lab-id", 
          "name": "text lab",
          "type": "custom",
          "parent_id": "technical-specs-id",
          "level": 1,
          "path": "Technical specs > text lab", 
          "document_count": 2,
          "children": [
            {
              "id": "aitm-id",
              "name": "AITM", 
              "type": "custom",
              "parent_id": "text-lab-id",
              "level": 2,
              "path": "Technical specs > text lab > AITM",
              "document_count": 1,
              "children": []
            }
          ]
        }
      ]
    },
    {
      "id": "pac-id",
      "name": "PAC",
      "type": "client", 
      "parent_id": null,
      "level": 0,
      "path": "PAC",
      "document_count": 2,
      "children": []
    }
  ]
}
```

## 🚀 MODIFICATION ENDPOINT REQUISE

### URL
`GET /repositories/{repository_id}/tags/hierarchy`

### Réponse Attendue
```json
{
  "repository": {
    "id": "repo-id",
    "name": "repo-name"
  },
  "hierarchy": [
    {
      "id": "tag-id",
      "name": "Tag Name", 
      "slug": "tag-slug",
      "type": "tag-type",
      "color": "#color",
      "icon": "🏷️",
      "parent_id": "parent-tag-id", // null si racine
      "level": 0,                   // 0 = racine, 1 = enfant, etc.
      "path": "Parent > Child",     // Chemin complet
      "document_count": 5,          // Documents associés à CE tag
      "total_descendants_count": 10, // Documents de ce tag + descendants
      "children": [...]             // Tags enfants (récursif)
    }
  ],
  "total_documents": 15,
  "total_tags": 8,
  "max_depth": 3
}
```

## 🎯 POINTS CRITIQUES

### 1. **TOUS LES TAGS** doivent apparaître
- Même ceux sans documents (`document_count: 0`)
- Exemple : "Technical specs" doit être visible comme parent

### 2. **Relations Parent-Enfant Vraies**  
- `parent_id` : ID du tag parent (null si racine)
- `path` : Chemin hiérarchique complet
- `level` : Niveau de profondeur (0 = racine)

### 3. **Structure Récursive**
- `children` : Array des tags enfants directs
- Permet reconstruction arborescente côté client

### 4. **Compteurs Intelligents**
- `document_count` : Documents de CE tag uniquement
- `total_descendants_count` : Ce tag + tous descendants

## 🔧 PARAMÈTRES OPTIONNELS

```
GET /repositories/{id}/tags/hierarchy?
  mode=compact          # Pas de documents dans réponse
  &include_empty=true   # Inclure tags sans documents (défaut: true)
  &max_depth=3          # Limiter profondeur 
  &tag_limit=100        # Pagination tags par niveau
```

## 🎪 EXEMPLE CONCRET

Pour gaudylab, on devrait voir :
```
📁 Technical specs (0) 
  └── 🏷️ text lab (2)
      └── 🏷️ AITM (1)
          └── 📄 AITM detailed analyse
      └── 📄 Pac proposition for TextLab
          
📁 PAC (2)
  └── 📄 jeff test du 24  
  └── 📄 Pac proposition for TextLab

📁 Sales proposals (1)
  └── 📄 Pac proposition for TextLab

📁 poem (1) 
  └── 📄 jeff test du 24
```

## ❓ QUESTIONS

1. **Faut-il créer un nouvel endpoint** `/repositories/{id}/tags/tree` ?
2. **Préférez-vous structure plate** avec `parent_id` ou **récursive** avec `children` ?
3. **Comment gérer la pagination** dans un arbre hiérarchique ?

L'approche récursive avec `children` semble plus naturelle pour l'affichage arborescent côté client.