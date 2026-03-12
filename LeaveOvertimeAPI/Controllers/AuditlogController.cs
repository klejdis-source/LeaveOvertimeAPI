using LeaveOvertimeAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Data;

namespace LeaveOvertimeAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditlogController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuditlogController(AppDbContext db)
        {
            _db = db;
        }


        // Get: api/auditlogs
        // filtrim sipas UserId dhe Date Range


        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)

        {
            var query = _db.Auditlogs.AsQueryable();
            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            if (from.HasValue)
                query = query.Where(a => a.Date >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.Date <= to.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(new { TotalCount = total, Page = page, PageSize = pageSize, Items = items });
            

        }
    }   

}
