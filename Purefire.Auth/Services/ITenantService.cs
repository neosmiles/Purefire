using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Purefire.Auth.Services;

public interface ITenantService
{
    string? GetCurrentTenantId();
    void SetTenantContext(string tenantId);
    void ClearTenantContext();
}
