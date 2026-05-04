using System.ComponentModel.DataAnnotations;

namespace Giwu.HRMS.Hybrid.Models.Auth;

/// <summary>
/// Form model bound to the Login page. Once the API exists, the same shape can
/// double as <c>LoginRequest</c> sent to <c>POST /api/auth/login</c> — keep the
/// validation attributes here so the rules match on both sides.
/// </summary>
public class LoginModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
