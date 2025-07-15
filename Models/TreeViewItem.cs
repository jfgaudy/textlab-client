using System.Collections.ObjectModel;

namespace TextLabClient.Models
{
    public class DocumentTreeItem
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "repository", "folder", "document"
        public object? Tag { get; set; } // Pour stocker l'objet original (Repository, Document)
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
    }
} 