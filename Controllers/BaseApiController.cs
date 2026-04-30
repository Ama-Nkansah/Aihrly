using Aihrly.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected async Task<(Guid? memberId, IActionResult? error)> ResolveTeamMemberAsync(AppDbContext db)
    {
        if (!Request.Headers.TryGetValue("X-Team-Member-Id", out var headerValue) ||
            !Guid.TryParse(headerValue, out var memberId))
        {
            return (null, BadRequest(new ProblemDetails
            {
                Title = "Missing or invalid X-Team-Member-Id header",
                Status = 400
            }));
        }

        var exists = await db.TeamMembers.AnyAsync(m => m.Id == memberId);
        if (!exists)
        {
            return (null, BadRequest(new ProblemDetails
            {
                Title = $"Team member {memberId} not found",
                Status = 400
            }));
        }

        return (memberId, null);
    }
}
