using LibraryManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }


public async Task<IActionResult> Index(int? page)
    {
        int pageNumber = page ?? 1;
        int pageSize = 10;

        var logs = await _context.AuditLogs
            .OrderByDescending(x => x.Time)
            .ToListAsync();

        var pagedLogs = logs.ToPagedList(pageNumber, pageSize);

        return View(pagedLogs);
    }
}
}