using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Http.Resilience;

namespace GitHub.Api;

public sealed class CustomPipelineSettings {
  
   public const string ConfigurationSection = "CustomPipelineResilience";

   [Required]
   public HttpTimeoutStrategyOptions Timeout { get; init; } = new();
}