using System.ComponentModel.DataAnnotations;

namespace RadRoofer.Core.DTOs.Auth;

public record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,
    [Required]
    string Password);
