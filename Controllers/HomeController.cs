using CinemaManagement.Data;
using CinemaManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly CinemaDbContext _db;
    public HomeController(CinemaDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today; var tomorrow = today.AddDays(1);
        ViewBag.Movies = await _db.Movies.CountAsync(m => m.Status == "NowShowing");
        ViewBag.ShowtimesToday = await _db.Showtimes.CountAsync(s => s.Status == "Active" && s.StartTime >= today && s.StartTime < tomorrow);
        ViewBag.TicketsToday = await _db.Tickets.CountAsync(t => t.TicketStatus != "Cancelled" && t.Booking.BookingTime >= today && t.Booking.BookingTime < tomorrow);
        ViewBag.RevenueToday = await _db.Bookings.Where(b => b.BookingStatus == "Paid" && b.BookingTime >= today && b.BookingTime < tomorrow)
                                        .SumAsync(b => (decimal?)b.TotalAmount) ?? 0m;
        var shows = await _db.Showtimes.Include(x => x.Movie).Include(x => x.Room)
            .Where(x => x.Status == "Active" && x.StartTime >= today && x.StartTime < tomorrow)
            .OrderBy(x => x.StartTime).ToListAsync();
        var ids = shows.Select(x => x.ShowtimeId).ToList();
        var sold = await _db.Tickets.Where(t => ids.Contains(t.ShowtimeId) && t.TicketStatus != "Cancelled")
            .GroupBy(t => t.ShowtimeId).Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C);
        ViewBag.Today = shows.Select(x => new ShowtimeRow { Showtime = x, Sold = sold.GetValueOrDefault(x.ShowtimeId), Capacity = x.Room.Capacity }).ToList();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        var ex = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var text = "Đã xảy ra lỗi.";
        if (ex != null)
        {
            text += "\n\n" + ex.GetType().Name + ": " + ex.Message;
            for (var i = ex.InnerException; i != null; i = i.InnerException) text += "\n-> " + i.Message;
        }
        return Content(text, "text/plain; charset=utf-8");
    }
}
