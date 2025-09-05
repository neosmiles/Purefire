using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api1.Data;
using api1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationController(DataContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> GetOrganizations()
    {
        return await context.Organizations.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> GetOrganization(int id)
    {
        var organization = await context.Organizations.FindAsync(id);
        if (organization == null)
        {
            return NotFound();
        }
        return organization;
    }

    [HttpPost]
    public async Task<ActionResult<Organization>> CreateOrganization(OrganizationDto organizationDto)
    {
        var organization = new Organization
        {
            Name = organizationDto.Name,
            ShortName = organizationDto.ShortName
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        // Create organization in Keycloak
        using var httpClient = new HttpClient();
        var keycloakOrgData = new
        {
            name = organizationDto.ShortName ?? organizationDto.Name,
            description = organizationDto.Name,
            domains = new[] { $"{organizationDto.ShortName ?? organizationDto.Name}.com" }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(keycloakOrgData);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync("http://localhost:5007/api/KeycloakOrganization", content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            // Log the error but don't fail the organization creation
            // You might want to add proper logging here
        }

        return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrganization(int id, Organization organization)
    {
        if (id != organization.Id)
        {
            return BadRequest();
        }

        context.Entry(organization).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrganizationExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrganization(int id)
    {
        var organization = await context.Organizations.FindAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        context.Organizations.Remove(organization);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private bool OrganizationExists(int id)
    {
        return context.Organizations.Any(e => e.Id == id);
    }
}