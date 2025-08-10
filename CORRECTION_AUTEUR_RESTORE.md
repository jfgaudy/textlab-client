# 🔧 Correction : Auteur incorrect lors du restore de versions

## 📋 **Problème identifié**

Lors de la restauration d'une version, l'auteur affiché est "TextLab Client" au lieu de l'auteur original de la version restaurée.

### **Comportement actuel (incorrect) :**
```
Version originale v3.0 : Auteur = "Jeff" (2025-01-05)
↓ Restore de v3.0
Nouvelle version v10.0 : Auteur = "TextLab Client User" ❌
```

### **Comportement attendu (correct) :**
```
Version originale v3.0 : Auteur = "Jeff" (2025-01-05) 
↓ Restore de v3.0
Nouvelle version v10.0 : Auteur = "Jeff" ✅
Message : "Restauration de la version v3.0 par CurrentUser via TextLab Client"
```

## 🔍 **Analyse technique**

### **Code problématique côté client :**
```csharp
// DocumentDetailsWindow.xaml.cs ligne 871
var restoreResult = await _apiService.RestoreDocumentVersionAsync(
    _document.Id, 
    versionToRestore, 
    "TextLab Client User",  // ❌ AUTEUR HARDCODÉ
    $"Restauration de la version {selectedVersion.Version}"
);
```

### **Logique métier attendue :**
- **Auteur du commit restore** = Auteur original de la version restaurée
- **Message de commit** = Indication claire de qui a fait le restore et pourquoi
- **Traçabilité** = Préserver l'identité originale tout en indiquant l'action de restore

## ✅ **Solutions proposées**

### **Solution 1 : Correction côté client (immédiate)**

```csharp
private async Task RestoreSelectedVersion()
{
    var selectedVersion = VersionsDataGrid.SelectedItem as DocumentVersion;
    if (selectedVersion == null) return;

    // ✅ Récupérer le vrai utilisateur actuel
    var userInfo = await _authService.GetCurrentUserAsync();
    var currentUser = userInfo?.Username ?? "Utilisateur inconnu";
    
    // ✅ Utiliser l'auteur original avec message détaillé
    var restoreResult = await _apiService.RestoreDocumentVersionAsync(
        _document.Id, 
        versionToRestore, 
        selectedVersion.Author,  // ✅ Auteur original de la version
        $"Restauration de la version {selectedVersion.Version} par {currentUser} via TextLab Client"
    );
}
```

### **Solution 2 : Amélioration côté serveur (recommandée)**

```python
@router.post("/api/v1/documents/{document_id}/versions/restore")
async def restore_document_version(
    document_id: str,
    restore_request: RestoreRequest,
    current_user: str = Depends(get_current_user)
):
    # ✅ Récupérer les informations de la version originale
    original_version = await git_service.get_commit_info(restore_request.commit_sha)
    original_author = original_version.author_name
    
    # ✅ Composer un message de commit informatif
    commit_message = (
        f"Restauration de la version {restore_request.version}\n\n"
        f"• Version originale: {restore_request.version} par {original_author}\n"
        f"• Restauré par: {current_user}\n"
        f"• Raison: {restore_request.reason}\n"
        f"• Date: {datetime.now().isoformat()}"
    )
    
    # ✅ Créer le commit avec l'auteur original
    new_commit = await git_service.create_commit(
        content=original_version.content,
        author=original_author,  # ✅ Préserver l'auteur original
        message=commit_message
    )
    
    return {
        "success": True,
        "restored_version": restore_request.version,
        "original_author": original_author,
        "restored_by": current_user,
        "new_commit_sha": new_commit.sha
    }
```

### **Solution 3 : Hybride (optimal)**

**Côté client :**
```csharp
// Ne pas spécifier d'auteur, laisser le serveur décider
var restoreResult = await _apiService.RestoreDocumentVersionAsync(
    _document.Id, 
    versionToRestore, 
    null,  // ✅ Laisser le serveur gérer l'auteur
    $"Restauration de la version {selectedVersion.Version}"
);
```

**Côté serveur :**
```python
async def restore_document_version(restore_request):
    if restore_request.author is None:
        # ✅ Logique automatique : utiliser l'auteur original
        original_commit = await git_service.get_commit(restore_request.commit_sha)
        author = original_commit.author
    else:
        # Utiliser l'auteur spécifié (rétrocompatibilité)
        author = restore_request.author
    
    # Créer le commit avec le bon auteur
    return await create_restore_commit(content, author, message)
```

## 🚀 **Recommandation d'implémentation**

### **Phase 1 : Correction immédiate côté client**
```csharp
// Remplacer ligne 871 dans DocumentDetailsWindow.xaml.cs
var restoreResult = await _apiService.RestoreDocumentVersionAsync(
    _document.Id, 
    versionToRestore, 
    selectedVersion.Author,  // ✅ FIX IMMÉDIAT
    $"Restauration de la version {selectedVersion.Version} par {await GetCurrentUsername()}"
);
```

### **Phase 2 : Amélioration serveur (optionnelle)**
- Logique intelligente d'auteur
- Messages de commit enrichis
- Traçabilité complète

## 🧪 **Test de validation**

```bash
# 1. Créer un document avec Jeff
POST /api/v1/documents/ 
Author: Jeff

# 2. Modifier avec Alice  
PUT /api/v1/documents/{id}
Author: Alice

# 3. Restaurer v1.0 (créée par Jeff)
POST /api/v1/documents/{id}/versions/restore
version: v1.0

# 4. Vérifier l'auteur de la nouvelle version
GET /api/v1/documents/{id}/versions
# Doit montrer: Auteur = "Jeff" (pas "TextLab Client")
```

## 📊 **Impact**

### **Avant correction :**
- ❌ Perte d'information sur l'auteur original
- ❌ Historique Git confus
- ❌ Traçabilité limitée

### **Après correction :**
- ✅ Préservation de l'identité originale
- ✅ Historique Git cohérent
- ✅ Traçabilité complète des actions

## 🎯 **Priorisation**

**Urgent** : Correction côté client (5 minutes)
**Souhaitable** : Amélioration côté serveur (développement futur)

---

**Statut** : 🔴 Bug identifié  
**Solution** : ✅ Correction simple côté client  
**Impact** : 🔧 Amélioration UX et traçabilité