#nullable enable
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TextLabClient.Models
{
    public class DocumentTreeItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _icon = string.Empty;
        private string _info = string.Empty;
        
        public string Name 
        { 
            get => _name; 
            set 
            { 
                if (_name != value) 
                { 
                    _name = value; 
                    OnPropertyChanged(nameof(Name)); 
                } 
            } 
        }
        
        public string Icon 
        { 
            get => _icon; 
            set 
            { 
                if (_icon != value) 
                { 
                    _icon = value; 
                    OnPropertyChanged(nameof(Icon)); 
                } 
            } 
        }
        
        public string Info 
        { 
            get => _info; 
            set 
            { 
                if (_info != value) 
                { 
                    _info = value; 
                    OnPropertyChanged(nameof(Info)); 
                } 
            } 
        }
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "repository", "folder", "document", "version"
        public object? Tag { get; set; } // Pour stocker l'objet original (Repository, Document, DocumentVersion)
        
        // Nouveau champ pour les versions
        public DocumentVersion? Version { get; set; } // Quand Type == "version"
        public string? VersionSha { get; set; } // SHA de la version spécifique
        
        public ObservableCollection<DocumentTreeItem> Children { get; set; }

        public DocumentTreeItem()
        {
            Children = new ObservableCollection<DocumentTreeItem>();
        }

        public DocumentTreeItem(string name, string icon = "", string info = "", string type = "")
        {
            Name = name;
            Icon = icon;
            Info = info;
            Type = type;
            Children = new ObservableCollection<DocumentTreeItem>();
        }
        
        // Constructeur spécialisé pour les versions
        public DocumentTreeItem(string name, string icon, string info, string type, DocumentVersion version, string versionSha) : this(name, icon, info, type)
        {
            Version = version;
            VersionSha = versionSha;
            Tag = version;
        }
        
        // Implémentation de INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 