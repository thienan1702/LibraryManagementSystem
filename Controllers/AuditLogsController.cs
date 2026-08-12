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

        public async Task<IActionResult> Index(
      string? search,
      string? actionFilter,
      DateTime? fromDate,
      DateTime? toDate,
      int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;

            var query = _context.AuditLogs.AsQueryable();

            // =========================
            // SEARCH
            // =========================
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    (x.UserName != null &&
                     x.UserName.Contains(search))
                    ||
                    (x.Entity != null &&
                     x.Entity.Contains(search))
                    ||
                    (x.Description != null &&
                     x.Description.Contains(search)));
            }

            // =========================
            // ACTION FILTER
            // =========================
            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(x =>
                    x.Action == actionFilter);
            }

            // =========================
            // FROM DATE
            // =========================
            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                query = query.Where(x =>
                    x.Time >= startDate);
            }

            // =========================
            // TO DATE
            // =========================
            if (toDate.HasValue)
            {
                var endDate =
                    toDate.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.Time < endDate);
            }

            // =========================
            // GET DATA
            // =========================
            var logs = await query
                .OrderByDescending(x => x.Time)
                .ToListAsync();

            // =========================
            // PAGINATION
            // =========================
            var pagedLogs = logs.ToPagedList(
                pageNumber,
                pageSize);

            // =========================
            // VIEW BAG
            // =========================
            ViewBag.Search = search;

            ViewBag.Action = actionFilter;

            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");

            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");

            ViewBag.DebugTotal = logs.Count;

            return View(pagedLogs);
        }
    }
}