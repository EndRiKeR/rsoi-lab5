using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StatisticService.Database;

namespace StatisticService.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(Roles = "Admin")]
public class StatisticsController : ControllerBase
{
    private readonly StatisticContext _db;
    public StatisticsController(StatisticContext db) => _db = db;

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.Events.AsQueryable();
        if (from.HasValue) query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Timestamp <= to.Value);
        var events = await query.ToListAsync();
        return Ok(events);
    }
}