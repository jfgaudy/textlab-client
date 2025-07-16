using System;
using Newtonsoft.Json;

namespace TextLabClient.Models
{
    public class Document
    {
        // Champs existants
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
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
        public DateTime UpdatedAt { get; set; }
        
        // NOUVEAUX CHAMPS selon la documentation API
        [JsonProperty("repository_name")]
        public string RepositoryName { get; set; } = string.Empty;
        
        [JsonProperty("category_display")]
        public string CategoryDisplay { get; set; } = string.Empty;
        
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
    }
} 