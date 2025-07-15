using System;

namespace TextLabClient.Models
{
    public class Document
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string GitPath { get; set; } = string.Empty;
        public string CommitSha { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 