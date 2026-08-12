using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

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

    public async Task<IActionResult> Index(int? page)
    {
        int pageNumber = page ?? 1;
        int pageSize = 10;

        var reservations = await _context.Reservations
            .Include(x => x.Book)
            .OrderByDescending(x => x.ReservationDate)
            .ToListAsync();

        var waiting = reservations.Count(x => x.Status == ReservationStatus.Waiting);
        var approved = reservations.Count(x => x.Status == ReservationStatus.Approved);
        var rejected = reservations.Count(x => x.Status == ReservationStatus.Rejected);

        ViewBag.Waiting = waiting;
        ViewBag.Approved = approved;
        ViewBag.Rejected = rejected;

        var pagedReservations =
            reservations.ToPagedList(pageNumber, pageSize);

        return View(pagedReservations);
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



    public async Task<IActionResult> BorrowReserved(int id)
    {
        var reservation = await _context.Reservations
            .Include(x => x.Book)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (reservation == null)
            return NotFound();

        if (reservation.Status != ReservationStatus.Approved)
            return RedirectToAction(nameof(Index));

        if (reservation.Book.AvailableQuantity < reservation.Quantity)
        {
            TempData["Error"] = "Book is no longer available.";
            return RedirectToAction(nameof(Index));
        }

        var borrow = new Borrow
        {
            BorrowerName = reservation.CustomerName,
            BorrowerEmail = reservation.CustomerEmail,
            BorrowDate = DateTime.Now,
            DueDate = DateTime.Now.AddDays(14),
            IsReturned = false,
            ReturnDate = null
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

        reservation.Book.AvailableQuantity -= reservation.Quantity;

        reservation.Status = ReservationStatus.Completed;

        await _context.SaveChangesAsync();

        await _email.SendAsync(
            reservation.CustomerEmail,
            "Borrow Confirmation",
    $@"
<h2>Library Management</h2>

<p>Hello <b>{reservation.CustomerName}</b>,</p>

<p>Your reserved book has been borrowed successfully.</p>

<table border='1' cellpadding='8' cellspacing='0'>
<tr>
<td>Book</td>
<td>{reservation.Book.Title}</td>
</tr>

<tr>
<td>Quantity</td>
<td>{reservation.Quantity}</td>
</tr>

<tr>
<td>Borrow Date</td>
<td>{borrow.BorrowDate:dd/MM/yyyy}</td>
</tr>

<tr>
<td>Due Date</td>
<td>{borrow.DueDate:dd/MM/yyyy}</td>
</tr>
</table>

<br/>

<p>Thank you.</p>");

        TempData["Success"] = "Borrow created successfully.";

        return RedirectToAction(nameof(Index));
    }


}