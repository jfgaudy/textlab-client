# Guide de Visualisation des Documents - TextLab Client

## 🎯 Nouvelle Fonctionnalité : Visualisation Complète des Documents

Grâce à l'intégration avec la documentation TextLab officielle, l'application propose maintenant une visualisation complète des documents avec toutes les informations disponibles via l'API.

## 🚀 Comment Utiliser

### 1. **Accéder aux Détails d'un Document**

1. **Connectez-vous** à l'API TextLab (bouton "Test Connexion")
2. **Sélectionnez un repository** dans le panneau gauche
3. **Naviguez** dans le TreeView : Repository → Catégorie → Document
4. **Double-cliquez** sur un document pour ouvrir ses détails complets

### 2. **Interface de Détails - 3 Onglets**

#### 📋 **Onglet "Informations"**
- **Métadonnées complètes** :
  - ID unique du document
  - Titre et catégorie
  - Repository d'origine
  - Chemin Git complet
  - Taille du fichier
  - Version actuelle
  - Dates de création/modification

- **Actions disponibles** :
  - 🔄 **Actualiser** : Recharge toutes les informations
  - 🌐 **Voir sur GitHub** : Ouvre le document sur GitHub

#### 📄 **Onglet "Contenu"**
- **Visualisation complète** du contenu Markdown
- **Police Consolas** pour une meilleure lisibilité
- **📋 Bouton Copier** : Copie tout le contenu dans le presse-papiers
- **Informations de taille** : Nombre d'octets affiché
- **Scroll horizontal/vertical** pour les longs documents

#### 📚 **Onglet "Versions"**
- **Historique complet** des versions Git
- **Tableau détaillé** avec :
  - Version (v1.0, v2.0, etc.)
  - Commit SHA (identifiant Git)
  - Auteur des modifications
  - Date et heure précises
  - Message de commit
  - Nombre de changements

## 🔧 Endpoints API Utilisés

D'après la documentation TextLab officielle, l'application utilise :

### **GET /api/v1/documents/{id}**
- Récupère les métadonnées complètes du document
- Informations de base, dates, repository

### **GET /api/v1/documents/{id}/content**
- Récupère le contenu complet du document
- Réponse JSON avec structure :
```json
{
  "content": "# Contenu Markdown...",
  "git_path": "path/to/file.md", 
  "version": "abc123def456",
  "last_modified": "2025-01-14T10:30:00Z",
  "repository_name": "gaudylab",
  "file_size_bytes": 1024
}
```

### **GET /api/v1/documents/{id}/versions**
- Récupère l'historique complet des versions
- Réponse JSON avec structure :
```json
{
  "document_id": "uuid-document",
  "total_versions": 5,
  "versions": [
    {
      "version": "v5.0",
      "commit_sha": "abc123def456", 
      "author": "TextLab User",
      "date": "2025-01-14T10:30:00Z",
      "message": "Mise à jour majeure",
      "changes_count": 15
    }
  ]
}
```

## 🎯 Cas d'Usage

### **1. Révision de Documents**
- Consultez l'historique pour comprendre l'évolution
- Identifiez qui a fait quelles modifications
- Vérifiez les dates de dernière mise à jour

### **2. Copie de Contenu**
- Copiez facilement le contenu Markdown
- Collez dans d'autres outils (Notion, Word, etc.)
- Récupérez le contenu pour traitement externe

### **3. Navigation GitHub**
- Accès direct au fichier sur GitHub
- Visualisation dans le contexte du repository
- Édition en ligne si nécessaire

### **4. Audit et Suivi**
- Vérification des métadonnées complètes
- Contrôle de la cohérence des informations
- Suivi des versions pour compliance

## 🔍 Informations Techniques

### **Gestion d'Erreurs**
- **Timeouts** : 30 secondes par requête
- **Fallbacks** : Affichage des erreurs explicites
- **Retry** : Bouton Actualiser pour nouvelle tentative

### **Performance**
- **Chargement asynchrone** : Interface réactive
- **Mise en cache** : Évite les requêtes répétées
- **HttpClient statique** : Pas de conflit de réutilisation

### **Sécurité**
- **Lecture seule** : Aucune modification possible
- **URLs validées** : Construction sécurisée des liens GitHub
- **Gestion exceptions** : Pas de crash sur erreurs API

## 🚀 Évolutions Futures Possibles

### **Phase Suivante Potentielle**
- **Édition de documents** : Interface d'édition intégrée
- **Comparaison de versions** : Diff visuel entre versions
- **Recherche dans le contenu** : Recherche full-text
- **Export** : Sauvegarde locale des documents
- **Notifications** : Alertes sur nouvelles versions

### **Intégrations Possibles**
- **Victor (Document Chat)** : Chat IA sur le contenu affiché
- **Markdown Renderer** : Prévisualisation formatée
- **Syntax Highlighting** : Coloration syntaxique du code
- **Print/PDF** : Export en formats divers

## 📊 Résultats

### ✅ **Fonctionnalités Opérationnelles**
- Double-clic sur document → Fenêtre de détails
- 3 onglets avec informations complètes
- Intégration parfaite avec l'API TextLab
- Interface professionnelle et intuitive

### ✅ **API TextLab Exploitée**
- Utilisation des endpoints officiels documentés
- Gestion de toutes les structures de données
- Respect des bonnes pratiques d'intégration

### ✅ **Expérience Utilisateur**
- Navigation fluide et logique
- Informations riches et détaillées
- Actions pratiques (copier, GitHub)
- Gestion d'erreurs gracieuse

**La visualisation des documents est maintenant complète et professionnelle ! 🎉** 