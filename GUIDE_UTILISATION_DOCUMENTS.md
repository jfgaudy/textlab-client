# 📖 Guide d'Utilisation - Visualisation des Documents TextLab

## 🚀 Comment voir les documents

### Étape 1 : Démarrer l'application
```bash
.\bin\Release\net8.0-windows\TextLabClient.exe
```

### Étape 2 : Se connecter à l'API
1. L'URL par défaut devrait être : `https://textlab-api.onrender.com`
2. Cliquez sur **"Tester la connexion"**
3. Attendez le message : ✅ **"Connexion réussie"**

### Étape 3 : Voir les repositories
- Les repositories se chargent automatiquement après la connexion
- Vous devriez voir 3 repositories dans la liste de gauche :
  - **PAC_Repo** (31 documents)
  - **gaudylab** 
  - Un autre repository

### Étape 4 : Sélectionner un repository
1. **Cliquez sur "PAC_Repo"** dans la liste de gauche
2. Les documents se chargent automatiquement
3. L'arbre hiérarchique apparaît à droite :
   ```
   📁 PAC_Repo (31 documents)
   ├── 📂 test (documents de test)
   ├── 📂 guides (guides utilisateur)
   ├── 📂 api (documentation API)
   └── 📂 Sans catégorie
   ```

### Étape 5 : Explorer les documents
1. **Développez les catégories** en cliquant sur les dossiers 📂
2. **Cliquez sur un document** pour le sélectionner
3. **Double-cliquez sur un document** pour voir ses détails complets

## 📋 Détails des Documents

Quand vous double-cliquez sur un document, une fenêtre s'ouvre avec 3 onglets :

### 📋 Onglet "Informations"
- **Métadonnées complètes** : ID, titre, catégorie, dates
- **Chemin Git** : localisation dans le repository
- **Actions** :
  - 🔄 **Actualiser** : recharger les informations
  - 🌐 **Voir sur GitHub** : ouvrir dans le navigateur

### 📄 Onglet "Contenu"
- **Contenu Markdown complet** du document
- **Bouton Copier** : copie le contenu dans le presse-papiers
- **Métadonnées fichier** : taille, version, dernière modification

### 📚 Onglet "Versions"
- **Historique Git complet** avec :
  - Version / Commit SHA
  - Auteur et date
  - Message de commit
  - Nombre de modifications

## 🔧 Résolution des Problèmes

### ❌ "Aucun repository trouvé"
1. Vérifiez la connexion Internet
2. Testez à nouveau la connexion API
3. L'API met parfois du temps à répondre (Render.com)

### ❌ "Aucun document trouvé"
1. Sélectionnez d'abord un repository dans la liste de gauche
2. Attendez le chargement complet
3. Essayez avec "PAC_Repo" qui contient 31 documents

### ❌ Erreur lors de l'ouverture des détails
- Certains endpoints de détails peuvent être temporairement indisponibles
- Les informations de base restent disponibles
- Réessayez plus tard ou contactez l'administrateur

### ❌ L'application ne se lance pas
```bash
# Recompiler si nécessaire
dotnet build -c Release

# Relancer
.\bin\Release\net8.0-windows\TextLabClient.exe
```

## 📊 Informations de Diagnostic

L'API TextLab contient actuellement :
- **3 repositories** actifs
- **31 documents** dans PAC_Repo
- **Catégories** : test, guides, api, etc.
- **Formats** : Markdown (.md)

## 🎯 Navigation Rapide

1. **Repositories** → Liste de gauche
2. **Documents** → Arbre hiérarchique à droite  
3. **Détails** → Double-clic sur un document
4. **Contenu** → Onglet "Contenu" dans les détails
5. **Historique** → Onglet "Versions" dans les détails

## 💡 Conseils d'Utilisation

- **Utilisez PAC_Repo** pour tester (contient le plus de documents)
- **Double-cliquez** pour voir les détails complets
- **Status bar** en bas pour suivre les opérations
- **Fenêtres modales** pour les détails sans perdre le contexte
- **Copier-coller** du contenu facilité

---
*Guide créé le $(Get-Date -Format "dd/MM/yyyy") - Application TextLab Client Windows* 