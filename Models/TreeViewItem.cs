using System.Collections.ObjectModel;

namespace TextLabClient.Models
{
    public class TreeViewItemModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string ItemCount { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty; // "repository", "document", "category"
        public object? Tag { get; set; } // L'objet original (Repository, Document, etc.)
        public ObservableCollection<TreeViewItemModel> Children { get; set; } = new ObservableCollection<TreeViewItemModel>();
        
        // Propriétés pour faciliter l'affichage
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
    }

    // Extension du modèle Document pour l'affichage
    public class DocumentDisplayModel : Document
    {
        public string TypeIcon => GetTypeIcon();
        
        private string GetTypeIcon()
        {
            return Category?.ToLower() switch
            {
                "technology" => "⚙️",
                "guides" => "📖",
                "api" => "🔧",
                "tutorials" => "🎓",
                "notes" => "📝",
                "drafts" => "📄",
                _ => "📄"
            };
        }
    }
} 