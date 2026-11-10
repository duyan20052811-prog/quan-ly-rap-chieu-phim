using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize]
public class ShowtimesController : Controller
{
    private const int CleaningMinutes = 15;
    private readonly CinemaDbContext _db;
    public ShowtimesController(CinemaDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? date)
    {
        var d = (date ?? DateTime.Today).Date;
        ViewBag.Date = d;
        var list = await _db.Showtimes.Include(s => s.Movie).Include(s => s.Room)
            .Where(s => s.StartTime >= d && s.StartTime < d.AddDays(1))
            .OrderBy(s => s.StartTime).ToListAsync();
        return View(list);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create() { await LoadLists(); return View(new ShowtimeCreateVm()); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ShowtimeCreateVm vm)
    {
        var movie = await _db.Movies.FindAsync(vm.MovieId);
        var room = await _db.Rooms.FindAsync(vm.RoomId);
        if (movie == null) ModelState.AddModelError(nameof(vm.MovieId), "Phim không tồn tại.");
        if (room == null) ModelState.AddModelError(nameof(vm.RoomId), "Phòng không tồn tại.");
        else if (room.Status != "Active") ModelState.AddModelError(nameof(vm.RoomId), "Phòng đang bảo trì.");
        if (movie != null && movie.Status == "Stopped") ModelState.AddModelError(nameof(vm.MovieId), "Phim đã ngừng chiếu.");

        if (ModelState.IsValid)
        {
            var start = vm.StartTime;
            var end = start.AddMinutes(movie.Duration + CleaningMinutes);
            // FR-09: kiểm tra xung đột lịch trong cùng phòng chiếu
            bool conflict = await _db.Showtimes.AnyAsync(s => s.RoomId == vm.RoomId && s.Status == "Active"
                                                              && s.StartTime < end && s.EndTime > start);
            if (conflict) ModelState.AddModelError("", "Xung đột lịch: phòng đã có suất chiếu trong khoảng thời gian này.");
            else
            {
                _db.Showtimes.Add(new Showtime { MovieId = vm.MovieId, RoomId = vm.RoomId, StartTime = start, EndTime = end, Price = vm.Price });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã tạo suất chiếu.";
                return RedirectToAction(nameof(Index), new { date = start.Date });
            }
        }
        await LoadLists();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        var s = await _db.Showtimes.FindAsync(id);
        if (s == null) return NotFound();
        if (await _db.Tickets.AnyAsync(t => t.ShowtimeId == id && t.TicketStatus != "Cancelled"))
            TempData["Error"] = "Suất chiếu đã có vé bán, không thể hủy.";
        else { s.Status = "Cancelled"; await _db.SaveChangesAsync(); TempData["Success"] = "Đã hủy suất chiếu."; }
        return RedirectToAction(nameof(Index), new { date = s.StartTime.Date });
    }

    private async Task LoadLists()
    {
        ViewBag.Movies = new SelectList(await _db.Movies.Where(m => m.Status != "Stopped").OrderBy(m => m.Title).ToListAsync(), "MovieId", "Title");
        ViewBag.Rooms = new SelectList(await _db.Rooms.Where(r => r.Status == "Active").OrderBy(r => r.RoomName).ToListAsync(), "RoomId", "RoomName");
    }
}
