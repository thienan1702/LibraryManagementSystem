using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[Authorize(Roles = "Admin,User")]
public class ReservationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _email;

    public ReservationsController(
        ApplicationDbContext context,
        IEmailService email)
    {
        _context = context;
        _email = email;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _context.Reservations
            .Include(x => x.Book)
            .OrderByDescending(x => x.ReservationDate)
            .ToListAsync();

        return View(list);
    }

    public IActionResult Create(int? bookId)
    {
        ViewBag.Books = _context.Books
            .OrderBy(x => x.Title)
            .ToList();

        ViewBag.SelectedBook = bookId;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Reservation reservation)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Books = _context.Books
                .OrderBy(x => x.Title)
                .ToList();

            return View(reservation);
        }

        reservation.Status = ReservationStatus.Waiting;

        reservation.ReservationDate = DateTime.Now;

        _context.Reservations.Add(reservation);

        await _context.SaveChangesAsync();

        await _email.SendAsync(
            reservation.CustomerEmail,
            "Reservation Created",
            $"""
            <h2>Reservation Successful</h2>

            <p>Hello <b>{reservation.CustomerName}</b>,</p>

            <p>Your reservation has been received.</p>

            <p>Status:
            <b>Waiting</b></p>

            <p>We will notify you when the book becomes available.</p>
            """);

        TempData["Success"] =
            "Reservation created successfully.";

        return RedirectToAction(nameof(Index));
    }
}