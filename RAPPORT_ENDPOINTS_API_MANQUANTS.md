# 📋 RAPPORT TECHNIQUE - Endpoints API Manquants TextLab

**Date :** 16 janvier 2025  
**Rapporteur :** Équipe Client Windows TextLab  
**Destinataire :** Équipe API TextLab  
**Priorité :** HAUTE - Fonctionnalités bloquantes pour le client  

---

## 🎯 **RÉSUMÉ EXÉCUTIF**

Le client Windows TextLab est **100% fonctionnel** pour la navigation et les métadonnées, mais **2 endpoints critiques** documentés dans la Phase 6 retournent actuellement **404 Not Found** sur l'API de production.

**Impact utilisateur :** Impossible d'afficher le contenu des documents et l'historique des versions.

---

## 🔍 **DIAGNOSTIC COMPLET**

### **✅ Endpoints FONCTIONNELS (confirmés)**
| Endpoint | Statut | Utilisation |
|----------|--------|-------------|
| `GET /health` | ✅ **Opérationnel** | Test de connexion |
| `GET /api/v1/repositories` | ✅ **Opérationnel** | Liste des 3 repositories |
| `GET /api/v1/documents/?repository_id={id}` | ✅ **Opérationnel** | 40 documents gaudylab |

### **❌ Endpoints MANQUANTS (404 Not Found)**
| Endpoint | Statut | Impact |
|----------|--------|--------|
| `GET /api/v1/documents/{id}/content` | ❌ **404 Not Found** | **BLOQUANT** - Affichage contenu |
| `GET /api/v1/documents/{id}/versions` | ❌ **404 Not Found** | **BLOQUANT** - Historique Git |
| `GET /api/v1/documents/{id}/raw` | ❌ **Non testé** | Contenu alternatif |

---

## 📖 **SPÉCIFICATIONS DÉTAILLÉES - Endpoints à Implémenter**

### **1. 🔥 PRIORITÉ ABSOLUE : GET `/api/v1/documents/{id}/content`**

#### **Description**
Récupère le contenu complet d'un document avec ses métadonnées.

#### **Paramètres**
- **`{id}`** : UUID du document (exemple : `73ede97b-872f-434f-bc0b-1f788bd1e9a9`)
- **Query optionnel** : `?version={commit_sha}` pour une version spécifique

#### **Réponse Attendue (JSON)**
```json
{
  "id": "73ede97b-872f-434f-bc0b-1f788bd1e9a9",
  "title": "Document Test Intégré",
  "content": "# Test Intégré Local + Render\n\n**Date :** 15/01/2025 01:00:53\n**Type :** Test d'intégration complet\n\nVoici le contenu complet du document...",
  "git_path": "integrated-tests/test_integrated_local+render_20250715_010053.md",
  "version": "b344ff95e7f8a9012b3c4567890def123456789a",
  "last_modified": "2025-01-15T01:00:53.234567Z",
  "repository_name": "gaudylab",
  "repository_id": "49f31bcb-8c5d-47ce-a992-3cbaf40c03dc",
  "file_size_bytes": 393,
  "encoding": "utf-8"
}
```

#### **Exemples d'Appels**
```bash
# Contenu actuel
curl "https://textlab-api.onrender.com/api/v1/documents/73ede97b-872f-434f-bc0b-1f788bd1e9a9/content"

# Version spécifique
curl "https://textlab-api.onrender.com/api/v1/documents/73ede97b-872f-434f-bc0b-1f788bd1e9a9/content?version=b344ff95e7f8a9012b3c4567890def123456789a"
```

#### **Logique d'Implémentation Suggérée**
1. **Vérifier** que le document existe dans la base de données
2. **Récupérer** le fichier depuis le repository Git (local ou GitHub)
3. **Lire** le contenu du fichier (UTF-8)
4. **Assembler** la réponse JSON avec métadonnées + contenu
5. **Gérer** le paramètre `version` si présent (checkout Git temporaire)

---

### **2. 🔥 PRIORITÉ ABSOLUE : GET `/api/v1/documents/{id}/versions`**

#### **Description**
Récupère l'historique complet des versions Git d'un document.

#### **Paramètres**
- **`{id}`** : UUID du document

#### **Réponse Attendue (JSON)**
```json
{
  "document_id": "73ede97b-872f-434f-bc0b-1f788bd1e9a9",
  "document_title": "Document Test Intégré",
  "git_path": "integrated-tests/test_integrated_local+render_20250715_010053.md",
  "total_versions": 5,
  "versions": [
    {
      "version": "v5.0",
      "commit_sha": "b344ff95e7f8a9012b3c4567890def123456789a",
      "commit_sha_short": "b344ff9",
      "author": "TextLab Integration Test",
      "author_email": "textlab@example.com",
      "date": "2025-01-15T01:00:53.234567Z",
      "message": "Mise à jour automatique du document",
      "changes_count": 15,
      "additions": 10,
      "deletions": 5,
      "is_current": true
    },
    {
      "version": "v4.0",
      "commit_sha": "a234bf85d6e7c8f90123456789abcdef01234567",
      "commit_sha_short": "a234bf8",
      "author": "TextLab User",
      "author_email": "user@example.com", 
      "date": "2025-01-14T18:30:22.123456Z",
      "message": "Ajout nouvelles sections",
      "changes_count": 8,
      "additions": 8,
      "deletions": 0,
      "is_current": false
    }
  ]
}
```

#### **Exemple d'Appel**
```bash
curl "https://textlab-api.onrender.com/api/v1/documents/73ede97b-872f-434f-bc0b-1f788bd1e9a9/versions"
```

#### **Logique d'Implémentation Suggérée**
1. **Récupérer** le document et son `git_path`
2. **Exécuter** `git log --follow --oneline --stat {git_path}` sur le repository
3. **Parser** la sortie Git pour extraire les métadonnées
4. **Formater** en JSON avec structure hiérarchique
5. **Marquer** la version actuelle (`is_current: true`)

---

### **3. 📄 OPTIONNEL : GET `/api/v1/documents/{id}/raw`**

#### **Description**
Retourne le contenu brut du document (text/plain ou JSON simple).

#### **Réponse Attendue (JSON simple)**
```json
{
  "document_id": "73ede97b-872f-434f-bc0b-1f788bd1e9a9",
  "raw_content": "# Test Intégré Local + Render\n\n**Date :** 15/01/2025 01:00:53\n**Type :** Test d'intégration complet\n\nVoici le contenu...",
  "encoding": "utf-8",
  "size_bytes": 393,
  "content_type": "text/markdown"
}
```

#### **Alternative text/plain**
```
Content-Type: text/plain; charset=utf-8

# Test Intégré Local + Render

**Date :** 15/01/2025 01:00:53
**Type :** Test d'intégration complet

Voici le contenu...
```

---

## 🚀 **ENDPOINTS AVANCÉS (Phase Future)**

### **4. GET `/api/v1/documents/{id}/versions/{commit_sha}/content`**
Contenu d'une version spécifique (alternative à `?version=`).

### **5. GET `/api/v1/documents/{id}/versions/{v1}/compare/{v2}`**
Comparaison détaillée entre deux versions (diff).

### **6. POST `/api/v1/documents/{id}/versions/{commit_sha}/restore`**
Restauration d'une version antérieure.

---

## 🧪 **TESTS DE VALIDATION**

### **Documents de Test Disponibles**
| Repository | Document ID | Titre | Statut |
|------------|-------------|-------|--------|
| gaudylab | `73ede97b-872f-434f-bc0b-1f788bd1e9a9` | "Document Test Intégré" | ✅ Disponible |
| gaudylab | `[tout autre UUID]` | Documents variés | ✅ 40 documents |

### **Tests à Effectuer**
```bash
# Test 1 : Contenu document existant
curl -v "https://textlab-api.onrender.com/api/v1/documents/73ede97b-872f-434f-bc0b-1f788bd1e9a9/content"
# Attendu : 200 OK + JSON contenu

# Test 2 : Historique document existant  
curl -v "https://textlab-api.onrender.com/api/v1/documents/73ede97b-872f-434f-bc0b-1f788bd1e9a9/versions"
# Attendu : 200 OK + JSON versions

# Test 3 : Document inexistant
curl -v "https://textlab-api.onrender.com/api/v1/documents/00000000-0000-0000-0000-000000000000/content"
# Attendu : 404 Not Found + message d'erreur

# Test 4 : Version spécifique
curl -v "https://textlab-api.onrender.com/api/v1/documents/73ede97b-872f-434f-bc0b-1f788bd1e9a9/content?version=b344ff95e7f8a9012b3c4567890def123456789a"
# Attendu : 200 OK + contenu de cette version
```

---

## 💻 **IMPLÉMENTATION CÔTÉ CLIENT (Confirmée Opérationnelle)**

Le client Windows a **déjà implémenté** la logique complète :

### **Code Appelant (TextLabApiService.cs)**
```csharp
// Méthode existante - prête à fonctionner
public async Task<DocumentContent?> GetDocumentContentAsync(string documentId)
{
    // Essaie /content puis /raw en fallback
    var response = await _httpClient.GetAsync($"/api/v1/documents/{documentId}/content");
    if (response.IsSuccessStatusCode) {
        var content = await response.Content.ReadAsStringAsync();
        var contentObj = JsonConvert.DeserializeObject<dynamic>(content);
        return new DocumentContent {
            Content = contentObj?.content?.ToString(),
            FileSizeBytes = contentObj?.file_size_bytes ?? 0,
            RepositoryName = contentObj?.repository_name?.ToString()
        };
    }
}

public async Task<DocumentVersions?> GetDocumentVersionsAsync(string documentId)
{
    // Méthode prête pour l'historique Git
    var response = await _httpClient.GetAsync($"/api/v1/documents/{documentId}/versions");
    // ... parsing JSON en objets .NET
}
```

### **Interface Utilisateur (DocumentDetailsWindow)**
- ✅ **Onglet "Contenu"** : Prêt à afficher le contenu Markdown
- ✅ **Onglet "Versions"** : Tableau préparé pour l'historique Git
- ✅ **Boutons d'action** : Copier, Actualiser, GitHub

---

## 🔧 **CONSIDÉRATIONS TECHNIQUES**

### **Performance**
- **Cache Git** : Considérer un cache pour `git log` (coûteux)
- **Streaming** : Pour gros documents, envisager le streaming
- **Parallélisme** : Appels simultanés content + versions possibles

### **Sécurité**  
- **Validation UUID** : Vérifier le format des `{id}` 
- **Path Traversal** : Sécuriser l'accès aux fichiers Git
- **Rate Limiting** : Limiter les appels pour éviter la surcharge

### **Compatibilité**
- **GitHub vs Local** : Supporter les deux modes (déjà implémenté)
- **Encodage** : UTF-8 par défaut, détecter autres encodages
- **Gros Fichiers** : Limite de taille (ex: 10MB) pour éviter timeout

---

## 📋 **PLAN D'IMPLÉMENTATION SUGGÉRÉ**

### **🎯 Phase 1 : Endpoints Critiques (1-2 jours)**
1. **Jour 1** : Implémenter `GET /api/v1/documents/{id}/content`
2. **Jour 2** : Implémenter `GET /api/v1/documents/{id}/versions`  
3. **Test** : Validation avec client Windows

### **🎯 Phase 2 : Optimisations (optionnel)**
1. Implémenter `GET /api/v1/documents/{id}/raw`
2. Ajouter support `?version=` pour content
3. Cache et optimisations performance

### **🎯 Phase 3 : Fonctions Avancées (futur)**
1. Comparaison de versions
2. Restauration de versions
3. Endpoints administratifs

---

## 🆘 **SUPPORT ET CONTACT**

### **Tests en Temps Réel**
L'équipe client Windows peut **tester immédiatement** dès que les endpoints sont déployés sur `https://textlab-api.onrender.com`.

### **Feedback Rapide**
- **Interface prête** : Résultats visibles instantanément dans l'application
- **Logs détaillés** : Debug disponible côté client pour diagnostics
- **Tests automatisés** : Validation complète en < 5 minutes

### **Contact Technique**
- **Repository Client** : `https://github.com/jfgaudy/textlab-client`
- **Documentation** : Rapport complet dans le repository
- **Tests** : Scripts PowerShell disponibles pour validation API

---

## 🏆 **BÉNÉFICES ATTENDUS**

### **Pour les Utilisateurs**
- ✅ **Visualisation complète** des documents Markdown
- ✅ **Historique Git détaillé** avec navigation temporelle  
- ✅ **Copie/export** du contenu pour réutilisation
- ✅ **Interface moderne** et intuitive

### **Pour le Projet TextLab**
- ✅ **API REST complète** conforme à la documentation Phase 6
- ✅ **Client Windows 100% fonctionnel** 
- ✅ **Écosystème cohérent** entre documentation et implémentation
- ✅ **Base solide** pour futurs clients (web, mobile)

---

**📞 EN ATTENTE DE VOTRE RETOUR POUR PLANNING DE DÉPLOIEMENT**

*Merci pour votre attention. L'équipe client est prête à tester dès que les endpoints seront disponibles !* 