using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Purefire.Auth.Services;

public class TenantService : ITenantService
{
    private readonly AsyncLocal<string?> _currentTenantId = new();

    public string? GetCurrentTenantId() => _currentTenantId.Value;

    public void SetTenantContext(string tenantId)
    {
        _currentTenantId.Value = tenantId;
    }

    public void ClearTenantContext()
    {
        _currentTenantId.Value = null;
    }
}
