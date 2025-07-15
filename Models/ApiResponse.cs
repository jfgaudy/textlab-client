using System.Collections.Generic;
using System;

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
        public List<Document> Documents { get; set; } = new List<Document>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
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
        public string DocumentId { get; set; } = string.Empty;
        public int TotalVersions { get; set; }
        public List<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    }

    public class DocumentVersion
    {
        public string Version { get; set; } = string.Empty;
        public string CommitSha { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ChangesCount { get; set; }
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