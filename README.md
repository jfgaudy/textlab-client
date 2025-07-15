# TextLab Client - Phase 1

## Description
Client Windows pour TextLab API - Interface simple et efficace en WPF.

## ✅ Phase 1 - Configuration et Setup (Terminée)

### Fonctionnalités Implémentées
- ✅ **Interface WPF** : Fenêtre principale avec menu et toolbar
- ✅ **Test de connexion** : Vérification de l'état de l'API TextLab
- ✅ **Configuration** : Sauvegarde automatique des paramètres utilisateur
- ✅ **Gestion repositories** : Affichage des repositories depuis l'API
- ✅ **Architecture propre** : Services, modèles, et UI séparés

## ✅ Phase 2 - Interface Liste Documents (Terminée)

### Nouvelles Fonctionnalités
- ✅ **Navigation à onglets** : Interface avec onglets Accueil et Documents
- ✅ **TreeView repositories** : Navigation structurée des repositories
- ✅ **DataGrid documents** : Affichage professionnel des documents avec colonnes
- ✅ **Icônes par catégorie** : Distinction visuelle des types de documents
- ✅ **Sélection interactive** : Clic sur repository → affichage des documents
- ✅ **Actions de base** : Boutons Actualiser et Nouveau document (préparation Phase 4)
- ✅ **Gestion d'états** : Messages de statut et compteurs de documents

### Prérequis
- **.NET 8 SDK** ou plus récent
- **Visual Studio 2022** ou **VS Code** avec extension C#
- **API TextLab** en fonctionnement (local ou Render)

### Installation et Test

#### 1. Installation .NET 8
```bash
# Télécharger depuis https://dotnet.microsoft.com/download/dotnet/8.0
# Ou via winget
winget install Microsoft.DotNet.SDK.8
```

#### 2. Build et Exécution
```bash
# Dans le dossier TextLabClient
dotnet restore
dotnet build
dotnet run
```

#### 3. Test de l'Application
1. **Lancer l'application** - Une fenêtre WPF s'ouvre
2. **Configurer l'URL API** - Dans la barre d'outils (par défaut: http://localhost:8000)
3. **Tester la connexion** - Cliquer sur "🔗 Test API"
4. **Vérifier les repositories** - S'affichent automatiquement si connexion réussie
5. **Naviguer dans les documents** - Cliquer sur un repository pour voir ses documents

### Structure du Projet
```
TextLabClient/
├── TextLabClient.csproj         # Configuration projet .NET
├── App.xaml / App.xaml.cs       # Application principale
├── MainWindow.xaml/.cs          # Fenêtre principale
├── Models/                      # Modèles de données
│   ├── Document.cs             # Modèle document
│   ├── Repository.cs           # Modèle repository
│   ├── TreeViewItem.cs         # Modèles UI (TreeView, Documents)
│   └── ApiResponse.cs          # Réponses API
├── Services/                   # Couche logique
│   ├── TextLabApiService.cs    # Communication API REST
│   └── ConfigurationService.cs # Gestion paramètres
├── Views/                      # Fenêtres supplémentaires (futures phases)
└── Resources/                  # Configuration et ressources
    └── app.config             # Paramètres application
```

### Fonctionnalités UI

#### Menu Principal
- **Fichier** → Quitter
- **Outils** → Paramètres (à implémenter), Tester Connexion
- **Aide** → À propos

#### Toolbar
- **🔄 Actualiser** - Recharge les repositories
- **🔗 Test API** - Teste la connexion à l'API
- **URL API** - Configuration de l'endpoint

#### Zone Principale (Phase 2)
- **Panneau gauche** : TreeView des repositories avec navigation
- **Zone centrale** : Onglets Accueil et Documents
  - **Onglet Accueil** : Informations de connexion et instructions
  - **Onglet Documents** : DataGrid avec liste des documents du repository sélectionné
- **Status bar** : Messages d'état et timestamp

### Configuration Automatique

#### Sauvegarde des Paramètres
Les paramètres sont sauvegardés automatiquement dans :
```
%AppData%\TextLabClient\settings.json
```

#### Paramètres Disponibles
- **ApiUrl** : URL de l'API TextLab
- **FontSize** : Taille de police (futur)
- **Theme** : Thème d'interface (futur)
- **AutoSave** : Sauvegarde automatique (futur)

### Test avec API TextLab

#### API Locale
```bash
# Démarrer l'API TextLab en local
cd backend
python -m uvicorn main:app --reload --port 8000

# Dans TextLab Client, utiliser :
# URL: http://localhost:8000
```

#### API Render (Production)
```bash
# Dans TextLab Client, utiliser :
# URL: https://textlab-api.onrender.com
```

### Gestion d'Erreurs

#### Connexion API
- **Timeout** : 30 secondes maximum
- **Erreurs réseau** : Messages explicites
- **API indisponible** : Statut d'erreur clair

#### Configuration
- **Dossier inaccessible** : Fallback vers paramètres par défaut
- **Fichier corrompu** : Recréation automatique

### Validation Phase 1 ✅

#### Tests à Effectuer
1. **Lancement application** ✅
2. **Interface responsive** ✅
3. **Test connexion locale** (si API locale disponible) ✅
4. **Test connexion Render** ✅
5. **Affichage repositories** ✅
6. **Sauvegarde paramètres** ✅
7. **Gestion erreurs** ✅

#### Critères de Succès
- [x] Application se lance sans erreur
- [x] Interface claire et intuitive
- [x] Connexion API fonctionnelle
- [x] Repositories affichés correctement
- [x] Paramètres persistants
- [x] Messages d'erreur explicites

### Validation Phase 2 ✅

#### Tests à Effectuer
1. **Navigation repositories** ✅ - Clic sur repository charge les documents
2. **Affichage documents** ✅ - DataGrid avec toutes les colonnes
3. **Compteur documents** ✅ - Affichage du nombre de documents
4. **Icônes catégories** ✅ - Différentes icônes par type de document
5. **Double-clic document** ✅ - Message d'information (préparation Phase 3)
6. **Boutons d'action** ✅ - Actualiser et Nouveau document disponibles

#### Critères de Succès
- [x] Sélection repository affiche ses documents
- [x] Interface onglets fonctionnelle
- [x] DataGrid responsive et lisible
- [x] Navigation fluide et intuitive
- [x] Gestion d'états appropriée
- [x] Prêt pour Phase 3 (lecture documents)

## Prochaines Phases

### Phase 2 - Interface Liste Documents
- TreeView avec documents par repository
- Navigation et sélection
- Métadonnées des documents

### Phase 3 - Lecture et Affichage
- Affichage contenu des documents
- Support Markdown
- Navigation versions

### Phases Suivantes
Voir `plan_implementation.md` pour le détail complet des 11 phases.

## Support et Debug

### Logs de Debug
Les logs sont disponibles dans la console de debug de Visual Studio.

### Problèmes Courants

#### "dotnet n'est pas reconnu"
```bash
# Installer .NET 8 SDK
winget install Microsoft.DotNet.SDK.8
# Redémarrer le terminal
```

#### "Connexion échouée"
- Vérifier que l'API TextLab est démarrée
- Tester l'URL dans un navigateur : `http://localhost:8000/health`
- Vérifier les pare-feu Windows

#### "Pas de repositories"
- L'API peut être connectée mais vide
- Créer un repository via l'API REST
- Vérifier les logs de l'API TextLab

---

**Phase 1 & 2 Terminées ! ✅**

L'application peut maintenant :
- Se connecter à l'API TextLab (Phase 1)
- Afficher les repositories disponibles (Phase 1)
- Naviguer dans les documents par repository (Phase 2)
- Afficher les détails des documents dans un DataGrid (Phase 2)
- Sauvegarder la configuration utilisateur
- Gérer les erreurs proprement

**Prêt pour la Phase 3 - Lecture et Affichage des Documents !** 🚀 