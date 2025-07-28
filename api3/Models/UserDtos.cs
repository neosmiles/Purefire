namespace api3.Models;

public class AppUserCreateDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
}

public class AppUserUpdateDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? Enabled { get; set; }
    public bool? EmailConfirmed { get; set; }
}

public class RoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AspNetRoleRequestDto
{
    public string RoleName { get; set; } = string.Empty;
}