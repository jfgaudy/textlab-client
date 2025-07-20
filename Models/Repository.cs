#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TextLabClient.Models
{
    public class Repository
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "local" ou "github"
        public string? LocalPath { get; set; }
        public string? RemoteUrl { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Propriétés pour la synchronisation Git
        public string? CurrentCommitSha { get; set; }
        public bool HasCredentials { get; set; }
        public DateTime? LastPullDate { get; set; }
        
        // NOUVEAUTÉ : Racine configurable des documents
        [JsonProperty("root_documents")]
        public string? RootDocuments { get; set; }
        
        // Propriété pour obtenir la racine avec fallback
        public string DocumentsRoot => RootDocuments ?? "documents/";
        
        // Propriétés pour l'interface utilisateur
        public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : LocalPath ?? RemoteUrl ?? "Repository";
        public string TypeDisplay => Type switch
        {
            "local" => "Local",
            "github" => "GitHub",
            "gitlab" => "GitLab",
            _ => Type
        };
        
        public string StatusDisplay => IsActive ? (IsDefault ? "Actif (Par défaut)" : "Actif") : "Inactif";
    }

    // Modèles pour les opérations Git
    public class PullStatus
    {
        public string RepositoryId { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public bool CanPull { get; set; }
        public bool HasRemote { get; set; }
        public int BehindCommits { get; set; }
        public int AheadCommits { get; set; }
        public bool LocalChanges { get; set; }
        public DateTime? LastPull { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class PullRequest
    {
        public string? RepositoryId { get; set; }
        public bool AutoResolveConflicts { get; set; } = false;
        public bool ForcePull { get; set; } = false;
    }

    public class PullResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PullChanges Changes { get; set; } = new();
        public string? LocalCommitBefore { get; set; }
        public string? LocalCommitAfter { get; set; }
        public string? Error { get; set; }
        public string? ErrorType { get; set; }
        public bool? ResolutionNeeded { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PullChanges
    {
        public int FilesUpdated { get; set; }
        public int CommitsPulled { get; set; }
        public List<string> Conflicts { get; set; } = new();
    }

    // Modèles pour la configuration
    public class LocalRepoConfig
    {
        public string RepoPath { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool ValidateStructure { get; set; } = true;
    }

    public class GitHubRepoConfig
    {
        public string RepoUrl { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string BranchName { get; set; } = "main";
        public bool CloneIfMissing { get; set; } = true;
    }
} 