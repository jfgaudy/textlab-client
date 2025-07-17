# SPÉCIFICATIONS TECHNIQUES API - TextLab v2.0

## 📋 **Vue d'Ensemble**

Ce document fournit les spécifications techniques détaillées pour l'implémentation des endpoints manquants de l'API TextLab, suite à l'analyse des meilleures pratiques REST.

**Conclusion de l'analyse** : L'API actuelle n'est **pas redondante** mais **incomplète**. Nous devons ajouter les opérations CRUD manquantes (PUT/DELETE) tout en conservant la structure existante.

---

## 🎯 **Architecture Cible**

### **API Complète POST-Implémentation**
```
GET    /api/v1/documents/                    # ✅ Existant - Liste
GET    /api/v1/documents/{id}                # ✅ Existant - Détails
GET    /api/v1/documents/{id}/content        # ✅ Existant - Contenu (justifié)
GET    /api/v1/documents/{id}/versions       # ✅ Existant - Historique
POST   /api/v1/documents/                    # ✅ Existant - Création
PUT    /api/v1/documents/{id}                # ❌ MANQUANT - Modification
DELETE /api/v1/documents/{id}                # ❌ MANQUANT - Suppression
GET    /api/v1/repositories/                 # ✅ Existant - Métadonnées
```

---

## 🔴 **PHASE 1 - ENDPOINTS CRITIQUES**

### **1. PUT /api/v1/documents/{id} - Modification Complète**

#### **Spécification HTTP**
```http
PUT /api/v1/documents/{document_id}
Content-Type: application/json
Authorization: Bearer <jwt_token>
Accept: application/json
```

#### **Paramètres URL**
| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| `document_id` | UUID | ✅ | Identifiant unique du document |

#### **Body Request**
```json
{
  "title": "string",                    // Requis
  "content": "string",                  // Requis
  "git_path": "string",                 // Optionnel
  "commit_message": "string"            // Optionnel, défaut: "Updated via API"
}
```

#### **Validation des Données**
```csharp
public class UpdateDocumentRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }

    [RegularExpression(@"^documents/[a-zA-Z0-9_-]+/[a-zA-Z0-9_.-]+\.md$")]
    public string? GitPath { get; set; }

    [StringLength(500)]
    public string? CommitMessage { get; set; }
}
```

#### **Réponses HTTP**

**✅ Success (200 OK)**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Document Modifié",
  "git_path": "documents/updated/modified-doc.md",
  "repository_name": "main-repo",
  "created_at": "2025-01-15T10:30:00Z",
  "updated_at": "2025-01-17T15:45:33Z",
  "file_size_bytes": 2048,
  "current_version": "v2.1",
  "last_commit_sha": "abc123def456",
  "last_commit_message": "Updated via API"
}
```

**❌ Error (400 Bad Request)**
```json
{
  "error": "Validation failed",
  "message": "The provided data is invalid",
  "code": "VALIDATION_ERROR",
  "details": [
    {
      "field": "title",
      "message": "Title is required and cannot be empty"
    }
  ]
}
```

**❌ Error (404 Not Found)**
```json
{
  "error": "Document not found",
  "message": "Document with ID 550e8400-e29b-41d4-a716-446655440000 does not exist",
  "code": "DOCUMENT_NOT_FOUND"
}
```

**❌ Error (409 Conflict)**
```json
{
  "error": "Document conflict",
  "message": "Document has been modified by another user",
  "code": "DOCUMENT_CONFLICT",
  "current_version": "v2.2",
  "your_version": "v2.1"
}
```

#### **Logique Métier**
```csharp
public async Task<DocumentResponse> UpdateDocumentAsync(Guid id, UpdateDocumentRequest request)
{
    // 1. Vérifier l'existence du document
    var existingDoc = await _repository.GetByIdAsync(id);
    if (existingDoc == null)
        throw new DocumentNotFoundException(id);

    // 2. Valider les permissions utilisateur
    await _authService.ValidateWritePermissionAsync(existingDoc, CurrentUser);

    // 3. Préparer les changements Git
    var gitChanges = new GitOperation
    {
        Type = GitOperationType.Update,
        FilePath = request.GitPath ?? existingDoc.GitPath,
        Content = request.Content,
        CommitMessage = request.CommitMessage ?? "Updated via API",
        Author = CurrentUser.Email
    };

    // 4. Exécuter la mise à jour avec transaction
    using var transaction = await _repository.BeginTransactionAsync();
    try
    {
        var updatedDoc = await _repository.UpdateAsync(id, request);
        var commitSha = await _gitService.CommitChangesAsync(gitChanges);
        
        updatedDoc.LastCommitSha = commitSha;
        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return _mapper.Map<DocumentResponse>(updatedDoc);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

### **2. DELETE /api/v1/documents/{id} - Suppression**

#### **Spécification HTTP**
```http
DELETE /api/v1/documents/{document_id}
Authorization: Bearer <jwt_token>
```

#### **Paramètres URL**
| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| `document_id` | UUID | ✅ | Identifiant unique du document |

#### **Paramètres Query (Optionnels)**
| Paramètre | Type | Défaut | Description |
|-----------|------|--------|-------------|
| `soft_delete` | boolean | `false` | Suppression logique vs physique |
| `commit_message` | string | `"Deleted via API"` | Message du commit Git |

#### **Réponses HTTP**

**✅ Success (204 No Content)**
```http
HTTP/1.1 204 No Content
X-Commit-SHA: def789abc123
X-Operation-Type: hard_delete
```

**✅ Success avec Soft Delete (200 OK)**
```json
{
  "success": true,
  "message": "Document archived successfully",
  "commit_sha": "def789abc123",
  "archived_at": "2025-01-17T16:20:00Z",
  "restore_possible": true
}
```

**❌ Error (404 Not Found)**
```json
{
  "error": "Document not found",
  "message": "Document with ID 550e8400-e29b-41d4-a716-446655440000 does not exist",
  "code": "DOCUMENT_NOT_FOUND"
}
```

**❌ Error (403 Forbidden)**
```json
{
  "error": "Permission denied",
  "message": "You do not have permission to delete this document",
  "code": "INSUFFICIENT_PERMISSIONS"
}
```

#### **Logique Métier**
```csharp
public async Task<DeleteResult> DeleteDocumentAsync(Guid id, DeleteOptions options)
{
    // 1. Vérifier l'existence et les permissions
    var document = await _repository.GetByIdAsync(id);
    if (document == null)
        throw new DocumentNotFoundException(id);

    await _authService.ValidateDeletePermissionAsync(document, CurrentUser);

    // 2. Préparer l'opération Git
    var gitOperation = new GitOperation
    {
        Type = options.SoftDelete ? GitOperationType.Archive : GitOperationType.Delete,
        FilePath = document.GitPath,
        CommitMessage = options.CommitMessage ?? "Deleted via API",
        Author = CurrentUser.Email
    };

    // 3. Exécuter la suppression
    using var transaction = await _repository.BeginTransactionAsync();
    try
    {
        string commitSha;
        
        if (options.SoftDelete)
        {
            await _repository.SoftDeleteAsync(id);
            commitSha = await _gitService.ArchiveFileAsync(gitOperation);
        }
        else
        {
            await _repository.DeleteAsync(id);
            commitSha = await _gitService.DeleteFileAsync(gitOperation);
        }

        await transaction.CommitAsync();
        
        return new DeleteResult
        {
            Success = true,
            CommitSha = commitSha,
            SoftDeleted = options.SoftDelete,
            DeletedAt = DateTime.UtcNow
        };
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 🟡 **PHASE 2 - ENDPOINTS AMÉLIORÉS**

### **3. PATCH /api/v1/documents/{id} - Modification Partielle**

#### **Spécification HTTP**
```http
PATCH /api/v1/documents/{document_id}
Content-Type: application/json-patch+json
Authorization: Bearer <jwt_token>
```

#### **Body Request (JSON Patch RFC 6902)**
```json
[
  {
    "op": "replace",
    "path": "/title",
    "value": "Nouveau titre"
  },
  {
    "op": "replace", 
    "path": "/content",
    "value": "Nouveau contenu partiel"
  }
]
```

#### **Alternative - Merge Patch**
```json
{
  "title": "Nouveau titre"  // Seuls les champs à modifier
}
```

---

### **4. PUT /api/v1/documents/{id}/content - Modification Contenu Seul**

#### **Spécification HTTP**
```http
PUT /api/v1/documents/{document_id}/content
Content-Type: text/plain
Authorization: Bearer <jwt_token>
```

#### **Body Request**
```
# Nouveau contenu Markdown
Ceci est le nouveau contenu du document...
```

#### **Réponse**
```json
{
  "content_updated": true,
  "new_file_size": 1024,
  "commit_sha": "xyz789def456",
  "updated_at": "2025-01-17T16:30:00Z"
}
```

---

## 🔒 **SÉCURITÉ ET AUTHENTIFICATION**

### **Middleware d'Authentification**
```csharp
[Authorize]
[ApiController]
[Route("api/v1/documents")]
public class DocumentsController : ControllerBase
{
    [HttpPut("{id:guid}")]
    [RequirePermission("documents:write")]
    public async Task<IActionResult> UpdateDocument(Guid id, UpdateDocumentRequest request)
    {
        // Implementation...
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("documents:delete")]
    public async Task<IActionResult> DeleteDocument(Guid id, [FromQuery] DeleteOptions options)
    {
        // Implementation...
    }
}
```

### **Validation des Permissions**
```csharp
public class DocumentPermissionService
{
    public async Task<bool> CanModifyDocumentAsync(Document document, User user)
    {
        // 1. Vérifier ownership
        if (document.CreatedBy == user.Id)
            return true;

        // 2. Vérifier permissions collaboratives
        if (await _collaborationService.HasWriteAccessAsync(document.Id, user.Id))
            return true;

        // 3. Vérifier rôles administrateur
        if (user.HasRole("Admin") || user.HasRole("DocumentManager"))
            return true;

        return false;
    }
}
```

---

## 📊 **MONITORING ET MÉTRIQUES**

### **Logging des Opérations**
```csharp
public class ApiOperationLogger
{
    public async Task LogOperationAsync(string operation, Guid documentId, string userId, bool success, TimeSpan duration)
    {
        var logEntry = new ApiOperationLog
        {
            Operation = operation,
            DocumentId = documentId,
            UserId = userId,
            Success = success,
            Duration = duration,
            Timestamp = DateTime.UtcNow,
            IpAddress = _httpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _logRepository.AddAsync(logEntry);
    }
}
```

### **Métriques à Collecter**
- **PUT /documents/{id}** : Temps de réponse, taux d'erreur, taille des documents modifiés
- **DELETE /documents/{id}** : Fréquence de suppression, ratio soft/hard delete
- **Erreurs 409 (Conflicts)** : Fréquence des conflits de versions
- **Utilisation par utilisateur** : Patterns d'usage des nouvelles fonctionnalités

---

## 🧪 **TESTS D'INTÉGRATION**

### **Tests PUT Document**
```csharp
[Test]
public async Task PUT_Document_Should_Update_Successfully()
{
    // Arrange
    var document = await CreateTestDocumentAsync();
    var updateRequest = new UpdateDocumentRequest
    {
        Title = "Titre Modifié",
        Content = "Contenu modifié"
    };

    // Act
    var response = await _client.PutAsJsonAsync($"/api/v1/documents/{document.Id}", updateRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var updatedDoc = await response.Content.ReadFromJsonAsync<DocumentResponse>();
    updatedDoc.Title.Should().Be("Titre Modifié");
    updatedDoc.UpdatedAt.Should().BeAfter(document.UpdatedAt);
}

[Test]
public async Task DELETE_Document_Should_Remove_Successfully()
{
    // Arrange
    var document = await CreateTestDocumentAsync();

    // Act
    var response = await _client.DeleteAsync($"/api/v1/documents/{document.Id}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    
    // Verify deletion
    var getResponse = await _client.GetAsync($"/api/v1/documents/{document.Id}");
    getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

---

## 📈 **PERFORMANCE ET OPTIMISATION**

### **Caching Strategy**
```csharp
[ResponseCache(Duration = 300, VaryByHeader = "Authorization")]
public async Task<IActionResult> GetDocument(Guid id)
{
    // Cache GET mais invalider cache sur PUT/DELETE
}

[HttpPut("{id}")]
public async Task<IActionResult> UpdateDocument(Guid id, UpdateDocumentRequest request)
{
    var result = await _documentService.UpdateAsync(id, request);
    
    // Invalider le cache
    await _cache.RemoveAsync($"document:{id}");
    await _cache.RemoveAsync("documents:list");
    
    return Ok(result);
}
```

### **Rate Limiting**
```csharp
[EnableRateLimiting("DocumentModification")]
public class DocumentsController : ControllerBase
{
    // Limite: 10 modifications par minute par utilisateur
}
```

---

## 🚀 **DÉPLOIEMENT ET ROLLBACK**

### **Feature Flags**
```csharp
public class DocumentsController : ControllerBase
{
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, UpdateDocumentRequest request)
    {
        if (!await _featureFlags.IsEnabledAsync("document-modifications"))
        {
            return StatusCode(503, "Feature temporarily disabled");
        }
        
        // Implementation...
    }
}
```

### **Versioning API**
- Maintenir compatibilité avec v1.0 existant
- Introduire v1.1 avec nouveaux endpoints
- Période de transition de 3 mois
- Documentation des changements breaking

---

## 📝 **CONCLUSION**

Cette spécification technique fournit une base solide pour l'implémentation des endpoints manquants de l'API TextLab. L'approche respecte les meilleures pratiques REST tout en maintenant la compatibilité avec l'architecture Git existante.

**Prochaines étapes** :
1. Implémentation Phase 1 (PUT/DELETE) - Priorité P0
2. Tests d'intégration complets
3. Déploiement en staging avec le client Windows
4. Monitoring et ajustements basés sur l'usage réel

**Contact** : Équipe Backend TextLab  
**Version** : 2.0  
**Date** : 17 janvier 2025 