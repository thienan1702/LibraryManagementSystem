using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index(
      string? search,
      int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;

            var query = _context.Suppliers
                .AsQueryable();

            // ==============================
            // SEARCH
            // ==============================

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Name.Contains(search) ||
                    (x.Address != null &&
                     x.Address.Contains(search)) ||
                    (x.Phone != null &&
                     x.Phone.Contains(search)) ||
                    (x.Email != null &&
                     x.Email.Contains(search)));
            }

            // ==============================
            // ORDER
            // ==============================

            query = query
                .OrderBy(x => x.Name);

            // ==============================
            // GET DATA
            // ==============================

            var supplierList =
                await query.ToListAsync();

            // ==============================
            // PAGINATION
            // ==============================

            var suppliers =
                supplierList.ToPagedList(
                    pageNumber,
                    pageSize);

            ViewBag.Search = search;

            return View(suppliers);
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var supplier = await _context.Suppliers
                .Include(x => x.StockReceipts)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }


        // GET: Suppliers/Create
        public IActionResult Create()
        {
            return View();
        }


        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (!ModelState.IsValid)
                return View(supplier);

            supplier.IsActive = true;

            _context.Suppliers.Add(supplier);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Supplier has been created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ==============================
        // EDIT - GET
        // ==============================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }


        // ==============================
        // EDIT - POST
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Supplier model)
        {
            if (id != model.Id)
                return NotFound();


            // ==============================
            // VALIDATE NAME
            // ==============================

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Supplier name is required.");
            }


            // ==============================
            // CHECK DUPLICATE NAME
            // ==============================

            var duplicate = await _context.Suppliers
                .AnyAsync(x =>
                    x.Id != model.Id &&
                    x.Name.ToLower() ==
                    model.Name.Trim().ToLower());

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "A supplier with this name already exists.");
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // ==============================
            // UPDATE
            // ==============================

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return NotFound();


            supplier.Name =
                model.Name.Trim();

            supplier.Address =
                string.IsNullOrWhiteSpace(model.Address)
                    ? null
                    : model.Address.Trim();

            supplier.Phone =
                string.IsNullOrWhiteSpace(model.Phone)
                    ? null
                    : model.Phone.Trim();

            supplier.Email =
                string.IsNullOrWhiteSpace(model.Email)
                    ? null
                    : model.Email.Trim();

            supplier.IsActive =
                model.IsActive;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Supplier '{supplier.Name}' has been updated successfully.";


            return RedirectToAction(
                nameof(Details),
                new { id = supplier.Id });
        }


        // ==============================
        // DELETE - GET
        // ==============================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var supplier = await _context.Suppliers
                .Include(x => x.StockReceipts)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }


        // ==============================
        // DELETE - POST
        // ==============================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers
                .Include(x => x.StockReceipts)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return NotFound();


            // ==============================
            // CHECK STOCK RECEIPTS
            // ==============================

            if (supplier.StockReceipts != null &&
                supplier.StockReceipts.Any())
            {
                TempData["Warning"] =
                    $"Supplier '{supplier.Name}' cannot be deleted because " +
                    $"it has {supplier.StockReceipts.Count} stock receipt(s). " +
                    "Please set the supplier to Inactive instead.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = supplier.Id });
            }


            // ==============================
            // DELETE
            // ==============================

            string supplierName = supplier.Name;

            _context.Suppliers.Remove(supplier);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Supplier '{supplierName}' has been deleted successfully.";


            return RedirectToAction(nameof(Index));
        }



        // POST: Suppliers/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x => x.Id == id);

            if (supplier == null)
                return NotFound();

            supplier.IsActive = !supplier.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                supplier.IsActive
                    ? "Supplier has been activated."
                    : "Supplier has been deactivated.";

            return RedirectToAction(nameof(Index));
        }
    }
}