# 🚨 MESSAGE URGENT - Équipe Serveur TextLab

## Contradiction majeure entre documentation et API réelle

### 📋 **RÉSUMÉ**
Il y a une **incohérence critique** entre votre documentation fournie et le comportement réel de l'API sur notre serveur local.

### 🔍 **DÉTAILS TECHNIQUES**

**Endpoint testé :** `POST /api/v1/documents/{documentId}/tags`

**Payload envoyé (selon votre documentation) :**
```json
[{"tag_id": "e3c9a8fc-659e-4950-bf65-8880b0d68cf8"}]
```

**Erreur reçue :**
```
Status: 422 UnprocessableEntity
{
  "detail": [
    {
      "type": "value_error",
      "loc": ["body", 0, "tag_id"],
      "msg": "Value error, Fournir soit tag_id soit tag_name",
      "input": "e3c9a8fc-659e-4950-bf65-8880b0d68cf8",
      "ctx": {"error": {}}
    }
  ]
}
```

### ⚠️ **PROBLÈME IDENTIFIÉ**

1. **Votre documentation dit :** Format `[{"tag_id": "uuid"}]` devrait fonctionner
2. **L'API réelle répond :** `"Fournir soit tag_id soit tag_name"` → Erreur 422

### 🎯 **QUESTIONS URGENTES**

1. **Quelle est la version de l'API** sur notre serveur local `http://localhost:8000` ?
2. **Le format correct est-il :**
   - `[{"tag_id": "uuid"}]` (selon votre doc)
   - `[{"tag_name": "text lab"}]` (selon l'erreur)
   - Autre chose ?
3. **Y a-t-il une différence** entre l'API de production et celle de développement local ?

### 📊 **INFORMATIONS DEBUGGING**

- **Authentication :** ✅ Fonctionne (X-User-Token header)
- **Tag existant :** ✅ Confirmé ID `e3c9a8fc-659e-4950-bf65-8880b0d68cf8`
- **Document existant :** ✅ Confirmé ID `e72ab565-a5b0-4d21-9c26-5020809ecf67`
- **Endpoint disponible :** ✅ Retourne 422 (pas 404)

### 🔧 **DEMANDE D'ACTION**

**Merci de vérifier IMMÉDIATEMENT :**
1. Le format exact attendu par l'API locale
2. La cohérence entre documentation et implémentation
3. Si nous devons utiliser `tag_name` au lieu de `tag_id`

**Cette incohérence bloque complètement l'intégration des tags côté client.**

---
**Contact :** Équipe Client TextLab  
**Urgence :** CRITIQUE - Bloquant développement