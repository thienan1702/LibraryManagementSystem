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

            "Reservation Successful",

    $"""
<h2>Library Reservation</h2>

<p>Hello <b>{reservation.CustomerName}</b>,</p>

<p>Your reservation has been created successfully.</p>

<p>Status :
<b>Waiting</b></p>

<p>We will notify you once the book is available.</p>

""");

        TempData["Success"] =
            "Reservation created successfully.";

        return RedirectToAction(nameof(Index));
    }


    public async Task<IActionResult> Approve(int id)
    {
        var reservation = await _context.Reservations
            .Include(x => x.Book)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (reservation == null)
            return NotFound();

        if (reservation.Book.AvailableQuantity < reservation.Quantity)
        {
            TempData["Error"] =
                "Book is still unavailable.";

            return RedirectToAction(nameof(Index));
        }

        reservation.Status = ReservationStatus.Approved;

        reservation.Book.AvailableQuantity -= reservation.Quantity;

        Borrow borrow = new Borrow
        {
            BorrowerName = reservation.CustomerName,

            BorrowerEmail = reservation.CustomerEmail,

            BorrowDate = DateTime.Today,

            DueDate = DateTime.Today.AddDays(7),

            ReturnDate = null,

            IsReturned = false
        };

        _context.Borrows.Add(borrow);

        await _context.SaveChangesAsync();

        BorrowDetail detail = new BorrowDetail
        {
            BorrowId = borrow.Id,

            BookId = reservation.BookId,

            Quantity = reservation.Quantity
        };

        _context.BorrowDetails.Add(detail);

        await _context.SaveChangesAsync();

        await _email.SendAsync(

            reservation.CustomerEmail,

            "Reservation Approved",

    $"""
<h2>Reservation Approved</h2>

<p>Hello <b>{reservation.CustomerName}</b>,</p>

<p>Your reserved book is now available.</p>

<p>Please come to the library.</p>

""");

        TempData["Success"] =
            "Reservation approved.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Reject(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);

        if (reservation == null)
            return NotFound();

        reservation.Status = ReservationStatus.Rejected;

        await _context.SaveChangesAsync();

        await _email.SendAsync(

            reservation.CustomerEmail,

            "Reservation Rejected",

    $"""
<h2>Reservation Rejected</h2>

<p>Hello <b>{reservation.CustomerName}</b>,</p>

<p>Sorry, your reservation has been rejected.</p>

""");

        TempData["Success"] =
            "Reservation rejected.";

        return RedirectToAction(nameof(Index));
    }


}