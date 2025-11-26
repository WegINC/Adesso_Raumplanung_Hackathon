using System.ComponentModel.DataAnnotations;

namespace Roomy.Api.Endpoints.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public string Message { get; set; } = string.Empty;
    public UserInfo User { get; set; } = null!;
}
