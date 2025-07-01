using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Purefire.Auth.Models;

public interface ITenantEntity
{
    public string? OrganizationId { get; set; }
}
