# 🚨 RÉGRESSION CRITIQUE SERVEUR - Chemins Git

## **🔴 PRIORITÉ CRITIQUE**

**Date :** 05/08/2025
**Impact :** BLOQUANT - Affichage incorrect des chemins de documents
**Composants affectés :** API Documents, Système de versions

---

## **📋 RÉSUMÉ DE LA RÉGRESSION**

Le serveur TextLab renvoie maintenant des chemins Git **COMPLETS** au lieu des chemins **RELATIFS**, causant une duplication du préfixe `documents/` dans l'affichage client.

## **🔍 PREUVES TECHNIQUES**

### **Endpoint Affecté :**
```
GET /api/v1/documents/{document_id}/versions
```

### **Comportement AVANT (Correct) :**
```json
{
  "git_path": "testjeff_4.md"
}
```

### **Comportement MAINTENANT (Incorrect) :**
```json
{
  "git_path": "documents/AITM_detailled_analyse.md"
}
```

### **Résultat Visuel :**
- **Affiché dans le client :** `documents/documents/AITM_detailled_analyse.md`
- **Chemin GitHub réel :** `documents/AITM_detailled_analyse.md`
- **Lien GitHub :** ✅ FONCTIONNE (confirme que le vrai chemin est correct)

## **📊 IMPACT UTILISATEUR**

1. **Chemins incorrects** dans l'interface client
2. **Confusion utilisateur** entre chemin affiché et chemin réel
3. **Sauvegarde de versions cassée** (problème connexe ?)

## **🔬 LOGS DE DIAGNOSTIC**

```
[2025-08-05 16:57:47.745] [DEBUG] 🔧 Chemin reconstruit: documents/ + documents/AITM_detailled_analyse.md = documents/documents/AITM_detailled_analyse.md
```

**Analyse :**
- `documentsRoot` = `"documents/"` ✅ Correct
- `gitPath` = `"documents/AITM_detailled_analyse.md"` ❌ **RÉGRESSION** (devrait être `"AITM_detailled_analyse.md"`)

## **⚡ DEMANDE DE CORRECTION**

### **Action Requise :**
Le champ `git_path` dans les réponses API doit retourner le chemin **RELATIF** par rapport à la racine des documents, **PAS** le chemin complet.

### **Exemples de Correction :**

**Document dans racine :**
- ❌ Actuel : `"git_path": "documents/AITM_detailled_analyse.md"`
- ✅ Attendu : `"git_path": "AITM_detailled_analyse.md"`

**Document dans sous-dossier :**
- ❌ Actuel : `"git_path": "documents/specs/technical_specs.md"`
- ✅ Attendu : `"git_path": "specs/technical_specs.md"`

## **🔧 ENDPOINTS À VÉRIFIER**

Vérifiez tous les endpoints qui retournent `git_path` :
- `GET /api/v1/documents/{document_id}/versions`
- `GET /api/v1/documents/{document_id}`
- `GET /api/v1/repositories/{repo_id}/documents`
- Tous les endpoints de recherche et vues

## **📝 QUESTIONS COMPLÉMENTAIRES**

1. **Quand cette régression a-t-elle été introduite ?**
2. **Y a-t-il eu des changements récents dans la gestion des chemins Git ?**
3. **Le problème de sauvegarde des versions est-il lié ?**

## **🆕 RÉGRESSION SUPPLÉMENTAIRE - Titres Corrompus**

### **Cas Documenté :**
**Document ID :** `f9a079f8-a3fb-4dd5-920d-d26fafb416e7`

**Symptômes :**
- **Fichier Git :** `vitor/version_tests.md`
- **Titre affiché :** "Untitled Document" ❌
- **Titre attendu :** "version_tests" (basé sur le nom de fichier)

**Historique de corruption :**
```json
// v1.0 (Création) - CORRECT
"message": "Création du document: version_tests"

// v2.0+ (Modifications) - CORROMPU  
"message": "feat: Mettre à jour 'Untitled Document'"
```

**Impact :** Les titres des documents sont corrompus lors des mises à jour, rendant l'identification des documents difficile.

## **⏰ URGENCE**

Cette régression **BLOQUE** l'utilisation normale du client. Merci de traiter en **PRIORITÉ MAXIMALE**.

**PROBLÈMES MULTIPLES :**
1. ✅ Chemins Git doublés (documents/documents/...) - **CORRIGÉ**
2. ❌ Titres corrompus lors des mises à jour
3. ❌ Structure arborescente pas respectée
4. ❌ **NOUVEAU** : Nom d'utilisateur corrompu en "TextLab Client"

## **🆘 RÉGRESSION CRITIQUE - Sauvegarde Versions**

### **⏰ CAS DE TEST DOCUMENTÉ :**

**Document :** "jeff test du 24" (ID: `42a7ce5c-270f-42b3-930e-80d2f77ae73a`)
**Date/Heure :** **05/08/2025 à 17:29:34**

### **🔍 FONCTIONS APPELÉES :**
1. `UpdateDocument()` - DocumentDetailsWindow.xaml.cs
2. `PUT /api/v1/documents/{id}?author=TextLab%20Client`
3. `GetDocumentVersions()` - Reload pour vérification

### **📊 LOGS CLIENT DÉTAILLÉS :**
```
[2025-08-05 17:29:34.194] 💾 Création nouvelle version document: 42a7ce5c-270f-42b3-930e-80d2f77ae73a
[2025-08-05 17:29:34.250] 🌐 PUT /documents/42a7ce5c-270f-42b3-930e-80d2f77ae73a?author=TextLab%20Client
[2025-08-05 17:29:38.327] ✅ Nouvelle version créée: commit a169a40732a9cf79c9eddbb6fb549334d5d7928c
```

### **❌ INCOHÉRENCE SERVEUR :**
- **Client confirme :** Commit `a169a40732a9cf79c9eddbb6fb549334d5d7928c` créé
- **Serveur retourne :** Toujours v15.0 avec commit `c7bd415080aa31f943dcf0595767ba2a2ff17fe7` (du 02/08/2025)
- **v16.0 MANQUANTE** dans l'historique des versions

### **🎯 IMPACT :**
Les nouvelles versions sont "acceptées" par le serveur mais **NE SONT PAS PERSISTÉES** dans l'historique !

## **🆕 RÉGRESSION UTILISATEUR - Nom d'Auteur Corrompu**

### **📊 CAS DOCUMENTÉ :**
**Document :** "test jeff 5" (ID: `f74b463c-02be-4367-b9ee-d046cc31ab37`)

**Évolution de l'auteur :**
- **v1.0 à v4.0 :** `"author":"jeff"` ✅ (correct)
- **v5.0 actuel :** `"author":"TextLab Client"` ❌ (générique)

### **🔍 CAUSE RACINE CÔTÉ CLIENT :**
```csharp
// DocumentDetailsWindow.xaml.cs ligne 1532
DEFAULT_AUTHOR,         // author ❌ PROBLÈME !
```

**Impact :** Perte de traçabilité - impossible de savoir qui a fait quoi !

**Solution :** Le client doit envoyer le nom d'utilisateur réel connecté.

---

**Reporter :** Client TextLab  
**Contact :** Équipe PAC