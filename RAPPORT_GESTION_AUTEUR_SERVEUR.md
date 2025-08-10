# 🔐 Rapport : Gestion de l'auteur côté serveur - Problème architectural

## 📋 **Problème identifié**

Le client TextLab détermine lui-même l'auteur des commits au lieu de laisser le serveur gérer cette information de manière sécurisée basée sur l'authentification.

## 🚨 **Situations concernées**

### **1. Sauvegarde de nouvelles versions**
```csharp
// ❌ PROBLÉMATIQUE ACTUELLE - Client décide l'auteur
var updatedDocument = await _apiService.UpdateDocumentAsync(
    _document.Id,
    realAuthor,             // ❌ Client envoie l'auteur
    _document.Title,
    newContent,
    _document.Visibility
);
```

### **2. Restauration de versions**
```csharp
// ❌ PROBLÉMATIQUE ACTUELLE - Client décide l'auteur  
var restoreResult = await _apiService.RestoreDocumentVersionAsync(
    _document.Id, 
    versionToRestore, 
    selectedVersion.Author,  // ❌ Client envoie l'auteur
    $"Restauration de la version {selectedVersion.Version}"
);
```

### **3. Création de nouveaux documents**
```csharp
// ❌ PROBLÉMATIQUE ACTUELLE - Client décide l'auteur
var document = await _apiService.CreateDocumentAsync(
    title, 
    content, 
    repositoryId, 
    visibility, 
    createdBy  // ❌ Client envoie l'auteur
);
```

## 🔒 **Problèmes de sécurité et cohérence**

### **Sécurité** :
- ❌ **Falsification d'identité** : Le client peut prétendre être n'importe qui
- ❌ **Pas de vérification** : Aucune validation côté serveur de l'auteur déclaré
- ❌ **Contournement d'auth** : Possible de créer des commits au nom d'autres utilisateurs

### **Cohérence** :
- ❌ **Sources multiples** : Client, auth service, sessions... peuvent diverger
- ❌ **Erreurs client** : Bugs client peuvent créer de mauvais auteurs
- ❌ **Incohérence temporelle** : Session expirée mais auteur encore envoyé

### **Auditabilité** :
- ❌ **Traçabilité compromise** : Impossible de faire confiance aux logs
- ❌ **Responsabilité floue** : Qui a vraiment fait l'action ?
- ❌ **Conformité** : Ne respecte pas les standards de sécurité

## ✅ **Architecture recommandée**

### **Principe** : Le serveur détermine TOUJOURS l'auteur basé sur l'authentification

```python
# ✅ ARCHITECTURE CIBLE - Serveur détermine l'auteur

@router.put("/api/v1/documents/{document_id}")
async def update_document(
    document_id: str,
    update_data: DocumentUpdate,
    current_user: User = Depends(get_current_authenticated_user)  # ✅ Auth automatique
):
    # ✅ Le serveur détermine l'auteur basé sur l'auth
    author = current_user.username
    author_email = current_user.email
    
    # Créer le commit avec l'utilisateur authentifié
    new_commit = await git_service.create_commit(
        content=update_data.content,
        author_name=author,
        author_email=author_email,
        message=f"Update document: {document.title}"
    )
    
    return updated_document

@router.post("/api/v1/documents/{document_id}/versions/restore")
async def restore_document_version(
    document_id: str,
    restore_request: RestoreRequest,
    current_user: User = Depends(get_current_authenticated_user)  # ✅ Auth automatique
):
    # ✅ Récupérer l'auteur original pour l'attribution
    original_version = await git_service.get_commit_info(restore_request.commit_sha)
    
    # ✅ Message détaillé avec traçabilité complète
    commit_message = (
        f"Restore to version {restore_request.version}\n\n"
        f"Original author: {original_version.author}\n"
        f"Restored by: {current_user.username}\n"
        f"Reason: {restore_request.reason}\n"
        f"Timestamp: {datetime.now().isoformat()}"
    )
    
    # ✅ DÉCISION MÉTIER : Qui doit être l'auteur du commit restore ?
    # Option A: L'utilisateur qui fait le restore
    author = current_user.username
    
    # Option B: L'auteur original (préserve l'attribution)
    # author = original_version.author
    
    new_commit = await git_service.create_commit(
        content=original_version.content,
        author_name=author,
        author_email=current_user.email,
        message=commit_message
    )
    
    return restore_result
```

## 🔧 **Modifications API nécessaires**

### **1. Supprimer les paramètres d'auteur des endpoints**

**AVANT** :
```http
PUT /api/v1/documents/{id}?author=TextLab%20Client  ❌
POST /api/v1/documents/{id}/versions/restore  
Body: {"author": "SomeUser", ...}  ❌
```

**APRÈS** :
```http
PUT /api/v1/documents/{id}  ✅ (pas de paramètre author)
POST /api/v1/documents/{id}/versions/restore  ✅
Body: {"reason": "...", "version": "..."}  ✅ (pas d'author)
```

### **2. Utiliser l'authentification pour déterminer l'auteur**

```python
# ✅ Middleware d'authentification requis
@app.middleware("http")
async def authentication_middleware(request: Request, call_next):
    # Extraire le token d'auth
    token = extract_auth_token(request)
    
    # Valider et décoder
    user = await validate_and_decode_token(token)
    
    # Injecter dans le context
    request.state.current_user = user
    
    return await call_next(request)

# ✅ Dependency pour récupérer l'utilisateur
async def get_current_user(request: Request) -> User:
    if not hasattr(request.state, 'current_user'):
        raise HTTPException(401, "Non authentifié")
    return request.state.current_user
```

### **3. Enrichir les réponses avec l'information d'auteur**

```python
# ✅ Réponse enrichie
{
    "id": "doc-123",
    "title": "Mon Document",
    "current_commit_sha": "abc123",
    "author_info": {
        "username": "jeff.martin",
        "display_name": "Jeff Martin", 
        "email": "jeff@company.com",
        "commit_timestamp": "2025-01-15T10:30:00Z"
    },
    "operation": "update|restore|create",
    "previous_version": "v7.0"  // Pour les restores
}
```

## 📊 **Impact sur le client**

### **Simplification du code client** :
```csharp
// ✅ APRÈS - Plus simple et sécurisé
var updatedDocument = await _apiService.UpdateDocumentAsync(
    _document.Id,
    // ✅ Plus de paramètre author - géré automatiquement
    _document.Title,
    newContent,
    _document.Visibility
);

var restoreResult = await _apiService.RestoreDocumentVersionAsync(
    _document.Id, 
    versionToRestore, 
    // ✅ Plus de paramètre author - géré automatiquement
    $"Restauration de la version {selectedVersion.Version}"
);
```

### **Suppression du code d'authentification côté client** :
```csharp
// ❌ SUPPRIME - Plus nécessaire
// var userInfo = await _authService.GetCurrentUserAsync();
// var realAuthor = userInfo?.Username ?? DEFAULT_AUTHOR;
```

## 🧪 **Migration et rétrocompatibilité**

### **Phase 1** : Support hybride
```python
@router.put("/api/v1/documents/{document_id}")
async def update_document(
    document_id: str,
    update_data: DocumentUpdate,
    author: str = Query(None, deprecated=True),  # ✅ Deprecated mais supporté
    current_user: User = Depends(get_current_user)
):
    # ✅ Prioriser l'utilisateur authentifié
    effective_author = current_user.username if current_user else author
    
    if author and current_user and author != current_user.username:
        logger.warning(f"Author mismatch: client={author}, auth={current_user.username}")
    
    # Utiliser l'auteur authentifié
    return await create_commit(effective_author, ...)
```

### **Phase 2** : Suppression paramètres author
```python
# ✅ Version finale - Seulement auth serveur
@router.put("/api/v1/documents/{document_id}")  
async def update_document(
    document_id: str,
    update_data: DocumentUpdate,
    current_user: User = Depends(get_current_user)  # ✅ Obligatoire
):
    # ✅ Seulement l'utilisateur authentifié
    return await create_commit(current_user.username, ...)
```

## 🔐 **Bénéfices sécurité**

### **Authentification renforcée** :
- ✅ **Source unique de vérité** : Seul le serveur détermine l'auteur
- ✅ **Non-répudiation** : Impossible de nier une action
- ✅ **Audit trail complet** : Traçabilité garantie

### **Prévention des attaques** :
- ✅ **Pas d'usurpation d'identité** : Le client ne peut pas se faire passer pour un autre
- ✅ **Session hijacking détecté** : Token invalide = rejet automatique
- ✅ **Logs fiables** : Corrélation auth token ↔ actions

## 🚀 **Recommandations d'implémentation**

### **Priorité 1** : Authentification côté serveur
1. ✅ Implémenter middleware d'authentification
2. ✅ Créer dependency `get_current_user()`
3. ✅ Modifier endpoints pour utiliser l'auth

### **Priorité 2** : Migration API
1. ✅ Phase hybride avec paramètres deprecated
2. ✅ Logging des divergences author vs auth
3. ✅ Communication aux développeurs clients

### **Priorité 3** : Suppression legacy
1. ✅ Supprimer paramètres author des endpoints
2. ✅ Documentation mise à jour
3. ✅ Tests de sécurité

## 📈 **Métriques de succès**

- **Sécurité** : 0% d'actions non-traçables à un utilisateur authentifié
- **Cohérence** : 100% des commits avec auteur = utilisateur auth
- **Performance** : Pas de dégradation (auth déjà présent)
- **UX** : Simplification code client (moins de paramètres)

## 🏁 **Conclusion**

Cette modification améliore significativement :
- ✅ **Sécurité** : Authentification renforcée
- ✅ **Simplicité** : Code client plus simple  
- ✅ **Fiabilité** : Source unique d'auteur
- ✅ **Audit** : Traçabilité garantie

**L'auteur doit TOUJOURS être déterminé par le serveur basé sur l'authentification, jamais par le client.**

---

**Statut** : 🔴 Problème architectural critique  
**Priorité** : 🔥 Haute (sécurité)  
**Impact** : 🔧 Breaking change contrôlé avec migration