# 🐛 Bug Report - API Création Documents

## Problème identifié

L'endpoint `POST /api/v1/documents/` présente un comportement incohérent pour le paramètre `category` :

### Comportement observé
- `category = null` → Document créé dans `internal/` 
- `category = "test"` → Document créé dans `test/`
- `category = "docs"` → Document créé dans `docs/`

### Problème
Quand `category = null`, l'API utilise automatiquement `internal/` comme répertoire par défaut au lieu d'un comportement prévisible.

## Test de reproduction

```bash
# Test effectué avec l'API en production
POST /api/v1/documents/
{
  "title": "Test Document",
  "content": "# Test",
  "category": null,
  "repository_id": null
}

# Résultat : git_path = "internal/test_document.md"
# Attendu : git_path = "documents/test_document.md" ou comportement documenté
```

## Questions pour l'équipe backend

1. **Est-il normal que `category = null` crée dans `internal/` ?**
2. **Est-il normal que les documents soient créés dans des sous-répertoires plutôt qu'à la racine ?**
3. **Quel devrait être le comportement par défaut pour `category = null` ?**

## Impact

- Désynchronisation entre l'affichage client et la structure réelle dans Git
- Confusion utilisateur sur l'emplacement des documents créés

---
*Rapport généré automatiquement par le client Windows - 19/07/2025* 