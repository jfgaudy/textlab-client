#nullable enable
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace TextLabClient.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    public class DocumentsResponse
    {
        [JsonProperty("documents")]
        public List<Document> Documents { get; set; } = new List<Document>();
        
        [JsonProperty("total")]
        public int Total { get; set; }
        
        [JsonProperty("page")]
        public int Page { get; set; }
        
        [JsonProperty("size")]
        public int Size { get; set; }
        
        [JsonProperty("pages")]
        public int Pages { get; set; }
    }

    public class DocumentContent
    {
        public string Content { get; set; } = string.Empty;
        public string GitPath { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string RepositoryName { get; set; } = string.Empty;
        public int FileSizeBytes { get; set; }
    }

    public class DocumentVersions
    {
        [JsonProperty("document_id")]
        public string DocumentId { get; set; } = string.Empty;
        
        [JsonProperty("document_title")]
        public string DocumentTitle { get; set; } = string.Empty;
        
        [JsonProperty("git_path")]
        public string GitPath { get; set; } = string.Empty;
        
        [JsonProperty("total_versions")]
        public int TotalVersions { get; set; }
        
        [JsonProperty("versions")]
        public List<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    }

    public class DocumentVersion
    {
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;
        
        [JsonProperty("commit_sha")]
        public string CommitSha { get; set; } = string.Empty;
        
        [JsonProperty("commit_sha_short")]
        public string CommitShaShort { get; set; } = string.Empty;
        
        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;
        
        [JsonProperty("author_email")]
        public string AuthorEmail { get; set; } = string.Empty;
        
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonProperty("changes_count")]
        public int ChangesCount { get; set; }
        
        [JsonProperty("additions")]
        public int Additions { get; set; }
        
        [JsonProperty("deletions")]
        public int Deletions { get; set; }
        
        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }
    }

    public class HealthInfo
    {
        public string Status { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? Environment { get; set; }
        public Dictionary<string, object>? Details { get; set; }
    }

    public class AppSettings
    {
        public string ApiUrl { get; set; } = "https://textlab-api.onrender.com";
        public string? GitHubToken { get; set; }
        public int FontSize { get; set; } = 12;
        public string Theme { get; set; } = "Default";
        public bool AutoSave { get; set; } = true;
        public int AutoSaveIntervalSeconds { get; set; } = 30;
    }
} 