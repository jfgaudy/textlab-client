using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TextLabClient.Models
{
    public class Document
    {
        // Champs existants
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        
        // Champs avec mapping JSON pour correspondre à l'API
        [JsonProperty("git_path")]
        public string GitPath { get; set; } = string.Empty;
        
        [JsonProperty("commit_sha")]
        public string CommitSha { get; set; } = string.Empty;
        
        public string Version { get; set; } = string.Empty;
        
        [JsonProperty("repository_id")]
        public string RepositoryId { get; set; } = string.Empty;
        
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        
        // NOUVEAUX CHAMPS selon la documentation API
        [JsonProperty("repository_name")]
        public string RepositoryName { get; set; } = string.Empty;
        

        
        [JsonProperty("content_preview")]
        public string ContentPreview { get; set; } = string.Empty;
        
        [JsonProperty("current_commit_sha")]
        public string CurrentCommitSha { get; set; } = string.Empty;
        
        [JsonProperty("file_size_bytes")]
        public int FileSizeBytes { get; set; }
        
        [JsonProperty("visibility")]
        public string Visibility { get; set; } = string.Empty;
        
        [JsonProperty("visibility_display")]
        public string VisibilityDisplay { get; set; } = string.Empty;
        
        [JsonProperty("created_by")]
        public string CreatedBy { get; set; } = string.Empty;
        
        [JsonProperty("is_active")]
        public bool IsActive { get; set; } = true;
        
        [JsonProperty("unique_identifier")]
        public string UniqueIdentifier { get; set; } = string.Empty;
        
        // NOUVEAUX CHAMPS pour les tags
        /// <summary>
        /// Tags associés à ce document avec leurs métadonnées
        /// </summary>
        [JsonProperty("tags")]
        public List<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
        
        /// <summary>
        /// Tags sous forme de liste simple (pour affichage rapide)
        /// </summary>
        [JsonIgnore]
        public List<Tag> TagList => Tags.Select(dt => dt.Tag).Where(t => t != null).ToList()!;
        
        /// <summary>
        /// Chaîne d'affichage des tags pour l'UI
        /// </summary>
        [JsonIgnore]
        public string TagsDisplay => string.Join(", ", TagList.Select(t => t.DisplayName));
        
        /// <summary>
        /// Tags groupés par type pour affichage organisé
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, List<Tag>> TagsByType => TagList.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.ToList());
        
        /// <summary>
        /// Retourne les tags d'un type spécifique
        /// </summary>
        public List<Tag> GetTagsByType(string tagType)
        {
            return TagList.Where(t => t.Type.Equals(tagType, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        /// <summary>
        /// Vérifie si le document a un tag spécifique
        /// </summary>
        public bool HasTag(string tagId)
        {
            return Tags.Any(dt => dt.TagId == tagId);
        }
        
        /// <summary>
        /// Vérifie si le document a un tag d'un type donné
        /// </summary>
        public bool HasTagOfType(string tagType)
        {
            return TagList.Any(t => t.Type.Equals(tagType, StringComparison.OrdinalIgnoreCase));
        }
    }
} 