#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace TextLabClient.Models
{
    /// <summary>
    /// Nœud d'arbre hiérarchique pour les tags avec navigation parent-enfant
    /// </summary>
    public class TagHierarchyNode : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;
        private string _displayName = string.Empty;

        /// <summary>
        /// Tag associé à ce nœud
        /// </summary>
        public Tag Tag { get; set; } = new Tag();
        
        /// <summary>
        /// Nœud parent (null pour les racines)
        /// </summary>
        public TagHierarchyNode? Parent { get; set; }
        
        /// <summary>
        /// Collection des nœuds enfants
        /// </summary>
        public ObservableCollection<TagHierarchyNode> Children { get; set; } = new ObservableCollection<TagHierarchyNode>();
        
        /// <summary>
        /// Niveau de profondeur dans l'arbre (0 = racine)
        /// </summary>
        public int Level => Parent?.Level + 1 ?? 0;
        
        /// <summary>
        /// Chemin complet depuis la racine (ex: "Client > ACME Corp > Projets")
        /// </summary>
        public string FullPath => Parent != null ? $"{Parent.FullPath} > {Tag.DisplayName}" : Tag.DisplayName;
        
        /// <summary>
        /// Nombre total de documents associés (incluant les enfants)
        /// </summary>
        public int DocumentCount { get; set; }
        
        /// <summary>
        /// Nombre de documents directs (sans les enfants)
        /// </summary>
        public int DirectDocumentCount { get; set; }
        
        /// <summary>
        /// État d'expansion dans l'UI
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        
        /// <summary>
        /// État de sélection dans l'UI
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        
        /// <summary>
        /// Nom d'affichage avec compteurs
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                {
                    var directText = DirectDocumentCount > 0 ? $" ({DirectDocumentCount})" : "";
                    var totalText = DocumentCount > DirectDocumentCount ? $" [{DocumentCount} total]" : "";
                    return $"{Tag.DisplayName}{directText}{totalText}";
                }
                return _displayName;
            }
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }
        
        /// <summary>
        /// Icône d'affichage basée sur le tag et l'état
        /// </summary>
        public string DisplayIcon => HasChildren ? (IsExpanded ? "📂" : "📁") : Tag.DisplayIcon;
        
        /// <summary>
        /// Couleur d'affichage du tag
        /// </summary>
        public string DisplayColor => Tag.DisplayColor;
        
        /// <summary>
        /// Indique si le nœud a des enfants
        /// </summary>
        public bool HasChildren => Children.Count > 0;
        
        /// <summary>
        /// Indique si c'est un nœud racine
        /// </summary>
        public bool IsRoot => Parent == null;
        
        /// <summary>
        /// Indique si c'est une feuille (pas d'enfants)
        /// </summary>
        public bool IsLeaf => !HasChildren;

        public TagHierarchyNode()
        {
            Children.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasChildren));
        }

        public TagHierarchyNode(Tag tag) : this()
        {
            Tag = tag;
        }

        /// <summary>
        /// Ajoute un enfant et définit sa relation parent
        /// </summary>
        public void AddChild(TagHierarchyNode child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Retire un enfant
        /// </summary>
        public void RemoveChild(TagHierarchyNode child)
        {
            child.Parent = null;
            Children.Remove(child);
        }

        /// <summary>
        /// Trouve un nœud par ID de tag récursivement
        /// </summary>
        public TagHierarchyNode? FindNodeById(string tagId)
        {
            if (Tag.Id == tagId) return this;
            
            foreach (var child in Children)
            {
                var found = child.FindNodeById(tagId);
                if (found != null) return found;
            }
            
            return null;
        }

        /// <summary>
        /// Retourne tous les nœuds descendants (enfants, petits-enfants, etc.)
        /// </summary>
        public IEnumerable<TagHierarchyNode> GetAllDescendants()
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var grandChild in child.GetAllDescendants())
                {
                    yield return grandChild;
                }
            }
        }

        /// <summary>
        /// Retourne le chemin complet d'IDs depuis la racine
        /// </summary>
        public List<string> GetIdPath()
        {
            var path = new List<string>();
            var current = this;
            
            while (current != null)
            {
                path.Insert(0, current.Tag.Id);
                current = current.Parent;
            }
            
            return path;
        }

        /// <summary>
        /// Filtre les enfants selon un prédicat
        /// </summary>
        public IEnumerable<TagHierarchyNode> FilterChildren(System.Func<TagHierarchyNode, bool> predicate)
        {
            return Children.Where(predicate);
        }

        /// <summary>
        /// Tri les enfants selon une clé
        /// </summary>
        public void SortChildren<TKey>(System.Func<TagHierarchyNode, TKey> keySelector)
        {
            var sorted = Children.OrderBy(keySelector).ToList();
            Children.Clear();
            foreach (var child in sorted)
            {
                Children.Add(child);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Gestionnaire de hiérarchie complète des tags
    /// </summary>
    public class TagHierarchyManager
    {
        private readonly Dictionary<string, TagHierarchyNode> _nodesByTagId = new();
        private readonly ObservableCollection<TagHierarchyNode> _rootNodes = new();

        /// <summary>
        /// Nœuds racines de la hiérarchie
        /// </summary>
        public ObservableCollection<TagHierarchyNode> RootNodes => _rootNodes;

        /// <summary>
        /// Tous les nœuds indexés par ID de tag
        /// </summary>
        public IReadOnlyDictionary<string, TagHierarchyNode> NodesByTagId => _nodesByTagId;

        /// <summary>
        /// Construit la hiérarchie à partir d'une liste de tags
        /// </summary>
        public void BuildHierarchy(IEnumerable<Tag> tags)
        {
            Clear();
            
            // Créer tous les nœuds d'abord
            foreach (var tag in tags)
            {
                var node = new TagHierarchyNode(tag);
                _nodesByTagId[tag.Id] = node;
            }

            // Ensuite, établir les relations parent-enfant
            foreach (var kvp in _nodesByTagId)
            {
                var node = kvp.Value;
                var tag = node.Tag;
                
                if (!string.IsNullOrEmpty(tag.ParentId) && _nodesByTagId.TryGetValue(tag.ParentId, out var parent))
                {
                    parent.AddChild(node);
                }
                else
                {
                    // Pas de parent = nœud racine
                    _rootNodes.Add(node);
                }
            }

            // Trier les nœuds racines par nom
            SortRootNodes();
        }

        /// <summary>
        /// Met à jour les compteurs de documents pour tous les nœuds
        /// </summary>
        public void UpdateDocumentCounts(Dictionary<string, int> directCounts)
        {
            // Réinitialiser tous les compteurs
            foreach (var node in _nodesByTagId.Values)
            {
                node.DirectDocumentCount = directCounts.GetValueOrDefault(node.Tag.Id, 0);
                node.DocumentCount = 0;
            }

            // Calculer les totaux récursivement depuis les feuilles
            foreach (var root in _rootNodes)
            {
                CalculateDocumentCountRecursive(root);
            }
        }

        private int CalculateDocumentCountRecursive(TagHierarchyNode node)
        {
            node.DocumentCount = node.DirectDocumentCount;
            
            foreach (var child in node.Children)
            {
                node.DocumentCount += CalculateDocumentCountRecursive(child);
            }
            
            return node.DocumentCount;
        }

        /// <summary>
        /// Trouve un nœud par ID de tag
        /// </summary>
        public TagHierarchyNode? FindNode(string tagId)
        {
            return _nodesByTagId.GetValueOrDefault(tagId);
        }

        /// <summary>
        /// Filtre la hiérarchie selon un prédicat
        /// </summary>
        public IEnumerable<TagHierarchyNode> FilterNodes(System.Func<TagHierarchyNode, bool> predicate)
        {
            return _nodesByTagId.Values.Where(predicate);
        }

        /// <summary>
        /// Trouve tous les nœuds d'un type donné
        /// </summary>
        public IEnumerable<TagHierarchyNode> GetNodesByType(string tagType)
        {
            return _nodesByTagId.Values.Where(n => n.Tag.Type == tagType);
        }

        /// <summary>
        /// Vide la hiérarchie
        /// </summary>
        public void Clear()
        {
            _nodesByTagId.Clear();
            _rootNodes.Clear();
        }

        /// <summary>
        /// Tri les nœuds racines par nom
        /// </summary>
        public void SortRootNodes()
        {
            var sorted = _rootNodes.OrderBy(n => n.Tag.DisplayName).ToList();
            _rootNodes.Clear();
            foreach (var node in sorted)
            {
                _rootNodes.Add(node);
            }
        }

        /// <summary>
        /// Développe tous les nœuds jusqu'à un niveau donné
        /// </summary>
        public void ExpandToLevel(int maxLevel)
        {
            foreach (var root in _rootNodes)
            {
                ExpandNodeToLevel(root, maxLevel);
            }
        }

        private void ExpandNodeToLevel(TagHierarchyNode node, int maxLevel)
        {
            if (node.Level < maxLevel)
            {
                node.IsExpanded = true;
                foreach (var child in node.Children)
                {
                    ExpandNodeToLevel(child, maxLevel);
                }
            }
        }
    }
}