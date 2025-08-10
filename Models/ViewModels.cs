#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TextLabClient.Models
{
    /// <summary>
    /// Vue structurée pour organiser les documents
    /// </summary>
    public class DocumentView
    {
        public string Type { get; set; } = string.Empty; // "all", "client", "technology", "status"
        public string Name { get; set; } = string.Empty;
        public List<ViewGroup> Groups { get; set; } = new List<ViewGroup>();
        public int TotalDocuments { get; set; }
    }

    /// <summary>
    /// Groupe de documents dans une vue
    /// </summary>
    public class ViewGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "📄";
        public string Color { get; set; } = "#0066CC";
        public List<Document> Documents { get; set; } = new List<Document>();
        public Dictionary<string, object>? Metadata { get; set; }
        
        public int DocumentCount => Documents.Count;
    }

    /// <summary>
    /// Réponse des endpoints de vue prédéfinie
    /// </summary>
    public class ViewResponse
    {
        [JsonProperty("view_name")]
        public string ViewName { get; set; } = string.Empty;
        
        [JsonProperty("total_documents")]
        public int TotalDocuments { get; set; }
        
        [JsonProperty("organization")]
        public Dictionary<string, object>? Organization { get; set; }
    }



    /// <summary>
    /// Groupe de documents dans la réponse API
    /// </summary>
    public class ViewGroupResponse
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = string.Empty;
        
        [JsonProperty("tag_id")]
        public string TagId { get; set; } = string.Empty;
        
        [JsonProperty("tag_color")]
        public string? TagColor { get; set; }
        
        [JsonProperty("tag_icon")]
        public string? TagIcon { get; set; }
        
        [JsonProperty("documents")]
        public List<Document> Documents { get; set; } = new List<Document>();
        
        [JsonProperty("document_count")]
        public int DocumentCount { get; set; }
        
        [JsonProperty("breakdown")]
        public Dictionary<string, int>? Breakdown { get; set; }
    }

    /// <summary>
    /// 🌳 HIÉRARCHIE RÉVOLUTIONNAIRE : Vraie structure parent-enfant récursive !
    /// </summary>
    public class RepositoryTagHierarchy
    {
        public RepositoryInfo Repository { get; set; } = new();
        
        // 🌳 RÉVOLUTION : Hiérarchie récursive avec children !
        public List<TagTreeNode> Hierarchy { get; set; } = new();
        
        [JsonProperty("total_documents")]
        public int TotalDocuments { get; set; }
        [JsonProperty("total_tags")]
        public int TotalTags { get; set; }
        [JsonProperty("max_depth")]
        public int MaxDepth { get; set; }
        [JsonProperty("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Métadonnées de pagination
    /// </summary>
    public class PaginationInfo
    {
        [JsonProperty("tag_limit")]
        public int TagLimit { get; set; }
        [JsonProperty("tag_offset")]
        public int TagOffset { get; set; }
        [JsonProperty("total_types")]
        public int TotalTypes { get; set; }
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// Information sur le repository
    /// </summary>
    public class RepositoryInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// Hiérarchie d'un type de tag avec métadonnées pagination
    /// </summary>
    public class TagTypeHierarchy
    {
        public string Type { get; set; } = "";
        [JsonProperty("type_display_name")]
        public string TypeDisplayName { get; set; } = "";
        [JsonProperty("type_icon")]
        public string TypeIcon { get; set; } = "";
        public List<TagWithDocuments> Tags { get; set; } = new();
        
        // 🚀 MÉTADONNÉES PAGINATION PAR TYPE
        [JsonProperty("total_tags")]
        public int TotalTags { get; set; }
        [JsonProperty("displayed_tags")]
        public int DisplayedTags { get; set; }
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// 🌳 RÉVOLUTION : Nœud d'arbre hiérarchique récursif !
    /// </summary>
    public class TagTreeNode
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Type { get; set; } = "";
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string? Description { get; set; }
        
        // 🌳 HIÉRARCHIE RÉVOLUTIONNAIRE
        [JsonProperty("parent_id")]
        public string? ParentId { get; set; }
        public int Level { get; set; } = 0;
        public string Path { get; set; } = "";
        
        // 📊 COMPTEURS INTELLIGENTS
        [JsonProperty("document_count")]
        public int DocumentCount { get; set; }
        [JsonProperty("total_descendants_count")]
        public int TotalDescendantsCount { get; set; }
        
        // 🌳 STRUCTURE RÉCURSIVE
        public List<TagTreeNode> Children { get; set; } = new();
        
        // 📄 DOCUMENTS (chargés à la demande)
        public List<Document>? Documents { get; set; }
    }

    /// <summary>
    /// Tag avec ses documents associés (Legacy - gardé pour compatibilité)
    /// </summary>
    public class TagWithDocuments
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Color { get; set; }
        public string? Icon { get; set; }
        [JsonProperty("document_count")]
        public int DocumentCount { get; set; }
        public List<Document> Documents { get; set; } = new();
        
        // 🌳 HIÉRARCHIE : Nouveaux champs pour parent-enfant
        [JsonProperty("parent_id")]
        public string? ParentId { get; set; }
        [JsonProperty("parent_name")]
        public string? ParentName { get; set; }
        [JsonProperty("level")]
        public int Level { get; set; } = 0;
        [JsonProperty("path")]
        public string? Path { get; set; } // Ex: "text lab > AITM"
        [JsonProperty("children")]
        public List<TagWithDocuments>? Children { get; set; }
    }

    /// <summary>
    /// 🚀 RÉVOLUTION : Réponse API pour documents d'un tag spécifique
    /// </summary>
    public class TagDocumentsResponse
    {
        [JsonProperty("tag_id")]
        public string TagId { get; set; } = "";
        
        public TagInfo? Tag { get; set; }
        public List<Document> Documents { get; set; } = new();
        public PaginationResponse? Pagination { get; set; }
        
        [JsonProperty("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Info basique d'un tag avec son chemin hiérarchique
    /// </summary>
    public class TagInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Type { get; set; } = "";
        public string? Color { get; set; }
        public string Path { get; set; } = "";
    }

    /// <summary>
    /// Réponse pagination standard
    /// </summary>
    public class PaginationResponse
    {
        public int Limit { get; set; }
        public int Offset { get; set; }
        public int Total { get; set; }
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }
        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }
    }
}