#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TextLabClient.Models
{
    /// <summary>
    /// Modèle pour les tags hiérarchiques TextLab
    /// </summary>
    public class Tag
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;
        
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty; // client, technology, status, category, priority, custom
        
        [JsonProperty("color")]
        public string? Color { get; set; } // Code couleur hex #RRGGBB
        
        [JsonProperty("icon")]
        public string? Icon { get; set; } // Identifiant d'icône
        
        [JsonProperty("parent_id")]
        public string? ParentId { get; set; } // Pour hiérarchie parent-enfant
        
        [JsonProperty("description")]
        public string? Description { get; set; }
        
        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
        
        [JsonProperty("is_public")]
        public bool IsPublic { get; set; } = true;
        
        [JsonProperty("is_system")]
        public bool IsSystem { get; set; } = false;
        
        [JsonProperty("is_active")]
        public bool IsActive { get; set; } = true;
        
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        
        // Propriétés calculées pour l'UI
        public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : Slug;
        public string DisplayColor => !string.IsNullOrEmpty(Color) ? Color : "#6C757D"; // Gris par défaut
        public string DisplayIcon => !string.IsNullOrEmpty(Icon) ? Icon : GetDefaultIconForType();
        
        /// <summary>
        /// Retourne l'icône par défaut selon le type de tag
        /// </summary>
        private string GetDefaultIconForType()
        {
            return Type.ToLower() switch
            {
                "client" => "🏢",
                "technology" => "⚙️",
                "status" => "📊",
                "category" => "📂",
                "priority" => "⭐",
                "custom" => "🏷️",
                _ => "🏷️"
            };
        }
    }
    
    /// <summary>
    /// Association entre un document et un tag avec métadonnées
    /// </summary>
    public class DocumentTag
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("document_id")]
        public string DocumentId { get; set; } = string.Empty;
        
        [JsonProperty("tag_id")]
        public string TagId { get; set; } = string.Empty;
        
        [JsonProperty("weight")]
        public double Weight { get; set; } = 1.0; // Pertinence 0.0 à 1.0
        
        [JsonProperty("confidence")]
        public double? Confidence { get; set; } = 1.0; // Précision pour tags automatiques
        
        [JsonProperty("context")]
        public string? Context { get; set; } // Contexte descriptif
        
        [JsonProperty("source")]
        public string Source { get; set; } = "manual"; // manual, auto, imported, suggested
        
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public Tag? Tag { get; set; }
        public Document? Document { get; set; }
        
        // Propriétés pour l'UI
        public string SourceDisplay => Source switch
        {
            "manual" => "👤 Manuel",
            "auto" => "🤖 Automatique", 
            "imported" => "📥 Importé",
            "suggested" => "💡 Suggéré",
            _ => Source
        };
        
        public string WeightDisplay => $"{Weight:P0}";
        public string ConfidenceDisplay => Confidence.HasValue ? $"{Confidence.Value:P0}" : "N/A";
    }
    
    /// <summary>
    /// Requête pour recherche avancée par tags
    /// </summary>
    public class TagSearchRequest
    {
        [JsonProperty("AND")]
        public List<TagFilter>? AndFilters { get; set; }
        
        [JsonProperty("OR")]
        public List<TagFilter>? OrFilters { get; set; }
        
        [JsonProperty("NOT")]
        public List<TagFilter>? NotFilters { get; set; }
        
        [JsonProperty("min_weight")]
        public double? MinWeight { get; set; }
        
        [JsonProperty("sort_by")]
        public string SortBy { get; set; } = "updated_at"; // updated_at, created_at, title
        
        [JsonProperty("sort_order")]
        public string SortOrder { get; set; } = "desc"; // asc, desc
        
        [JsonProperty("limit")]
        public int Limit { get; set; } = 50;
        
        [JsonProperty("offset")]
        public int Offset { get; set; } = 0;
    }
    
    /// <summary>
    /// Filtre pour recherche par type et valeurs de tags
    /// </summary>
    public class TagFilter
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonProperty("values")]
        public List<string> Values { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Réponse de recherche avec documents et métadonnées
    /// </summary>
    public class TagSearchResponse
    {
        [JsonProperty("documents")]
        public List<Document> Documents { get; set; } = new List<Document>();
        
        [JsonProperty("total")]
        public int Total { get; set; }
        
        [JsonProperty("filters_applied")]
        public object? FiltersApplied { get; set; }
        
        [JsonProperty("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }
    
    /// <summary>
    /// Statistiques du système de tags
    /// </summary>
    public class TagSystemStats
    {
        [JsonProperty("total_tags")]
        public int TotalTags { get; set; }
        
        [JsonProperty("total_associations")]
        public int TotalAssociations { get; set; }
        
        [JsonProperty("tags_by_type")]
        public Dictionary<string, int> TagsByType { get; set; } = new Dictionary<string, int>();
        
        [JsonProperty("most_used_tags")]
        public List<Tag> MostUsedTags { get; set; } = new List<Tag>();
        
        [JsonProperty("recent_activity")]
        public List<object> RecentActivity { get; set; } = new List<object>();
    }
    
    /// <summary>
    /// Suggestion de tag pour un document
    /// </summary>
    public class TagSuggestion
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = string.Empty;
        
        [JsonProperty("tag_type")]
        public string TagType { get; set; } = string.Empty;
        
        [JsonProperty("confidence")]
        public double Confidence { get; set; }
        
        [JsonProperty("reason")]
        public string? Reason { get; set; }
    }
    
    /// <summary>
    /// Réponse de suggestions de tags
    /// </summary>
    public class TagSuggestionsResponse
    {
        [JsonProperty("document_id")]
        public string DocumentId { get; set; } = string.Empty;
        
        [JsonProperty("suggested_tags")]
        public List<TagSuggestion> SuggestedTags { get; set; } = new List<TagSuggestion>();
        
        [JsonProperty("generated_at")]
        public DateTime GeneratedAt { get; set; }
    }
}