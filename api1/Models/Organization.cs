using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api1.Models;

public class Organization
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? KeycloakOrganizationId { get; set; }

}


public class OrganizationDto
{

    public string? Name { get; set; }
    public string? ShortName { get; set; }

}
