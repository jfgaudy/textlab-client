#nullable enable
using System;
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
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 