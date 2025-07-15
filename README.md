# 🪟 TextLab Client Windows

Client Windows moderne en WPF pour l'API TextLab - Visualisation complète des repositories et documents.

## 🎯 Version Actuelle

**✅ Version fonctionnelle** avec interface complète pour navigation et consultation des documents.

### 🚀 Fonctionnalités Implémentées

- **🔗 Connexion API** : Test et validation automatique
- **📁 Gestion Repositories** : Liste et sélection de tous les repositories
- **📄 Navigation Documents** : Affichage hiérarchique avec catégories
- **🪟 Détails Complets** : Fenêtre dédiée avec 3 onglets (Informations, Contenu, Versions)
- **🎨 Interface Moderne** : WPF responsive avec icônes et navigation intuitive

### 📊 Repositories Supportés

- **gaudylab** : 40 documents disponibles
- **PAC_Repo** : 31 documents disponibles  
- **Détection automatique** de tous les repositories

## 🛠️ Installation et Utilisation

### Prérequis
- .NET 8.0 Windows Desktop Runtime
- Windows 10/11

### Installation Rapide
```bash
git clone https://github.com/jfgaudy/textlab-client.git
cd textlab-client
dotnet build -c Release
.\bin\Release\net8.0-windows\TextLabClient.exe
```

### Utilisation Simple
1. **Connexion** : Cliquez "Tester la connexion" (API par défaut configurée)
2. **Repositories** : Sélectionnez un repository dans la liste (recommandé : gaudylab)
3. **Documents** : Naviguez dans l'arbre hiérarchique
4. **Détails** : Double-cliquez sur un document pour voir ses informations complètes

### Script de Lancement
```powershell
.\lancer_app.ps1  # Lance l'application avec diagnostic rapide
```

## 📋 État Fonctionnel Détaillé

### ✅ Ce qui Fonctionne Parfaitement
| Fonctionnalité | Statut | Description |
|----------------|--------|-------------|
| Connexion API | ✅ | Test automatique, statut temps réel |
| Liste Repositories | ✅ | 3 repositories détectés automatiquement |
| Navigation Documents | ✅ | Hiérarchie Repository → Catégories → Documents |
| Métadonnées | ✅ | ID, titre, dates, Git path, versions |
| Interface Utilisateur | ✅ | Responsive, intuitive, moderne |

### ⚠️ Limitations API Actuelles
| Endpoint | Statut | Impact |
|----------|--------|--------|
| `/api/v1/documents/{id}/content` | ❌ 404 | Contenu Markdown indisponible |
| `/api/v1/documents/{id}/versions` | ❌ 404 | Historique Git indisponible |

**Note** : L'application affiche des messages informatifs clairs expliquant ces limitations.

## 🔧 Configuration

### API par Défaut
```
URL: https://textlab-api.onrender.com
Timeout: 30 secondes
Format: JSON REST API
```

### Fichiers de Configuration
- **Automatique** : Sauvegarde des paramètres utilisateur
- **Modifiable** : URL API configurable dans l'interface

## 📚 Documentation

- **[Guide d'Utilisation](GUIDE_UTILISATION_DOCUMENTS.md)** : Instructions détaillées
- **[État de l'Application](ETAT_ACTUEL_APPLICATION.md)** : Rapport complet des fonctionnalités
- **[Guide de Visualisation](GUIDE_VISUALISATION.md)** : Utilisation avancée

## 🎯 Prochaines Évolutions

### Côté API (Requis)
- [ ] Implémentation endpoint `/content` 
- [ ] Implémentation endpoint `/versions`
- [ ] Tests avec les documents existants

### Côté Client (Prêt)
- [x] Interface pour contenu Markdown
- [x] Tableau historique Git  
- [x] Gestion d'erreurs robuste
- [x] Messages informatifs

## 👨‍💻 Développement

### Architecture
- **Framework** : .NET 8.0 WPF
- **API Client** : HttpClient avec Newtonsoft.Json
- **Interface** : XAML avec modèle MVVM
- **Gestion d'Erreurs** : Try-catch avec fallbacks

### Structure du Projet
```
├── MainWindow.xaml(.cs)          # Fenêtre principale
├── DocumentDetailsWindow.xaml(.cs) # Détails des documents  
├── Models/                       # Classes de données
├── Services/                     # Services API
└── Resources/                    # Configuration
```

## 🐛 Support

### Issues Connues
- ✅ **Aucun bug majeur** dans la version actuelle
- ⚠️ **Limitations API** documentées et gérées

### Contact
- **GitHub** : [Issues](https://github.com/jfgaudy/textlab-client/issues)
- **Email** : jfgaudy@outlook.com

---

**🎉 Application prête pour utilisation en production** avec toutes les fonctionnalités client implémentées ! 