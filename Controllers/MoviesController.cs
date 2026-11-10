using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize]
public class MoviesController : Controller
{
    private readonly CinemaDbContext _db;
    public MoviesController(CinemaDbContext db) => _db = db;

    public async Task<IActionResult> Index(string q)
    {
        var query = _db.Movies.Include(m => m.MovieGenres).ThenInclude(g => g.Genre).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(m => m.Title.Contains(q));
        ViewBag.Q = q;
        return View(await query.OrderByDescending(m => m.ReleaseDate).ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create() => View(new MovieFormVm { AllGenres = await _db.Genres.OrderBy(g => g.GenreName).ToListAsync() });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(MovieFormVm vm)
    {
        if (!ModelState.IsValid) { vm.AllGenres = await _db.Genres.ToListAsync(); return View(vm); }
        var m = new Movie { Title = vm.Title, Duration = vm.Duration, ReleaseDate = vm.ReleaseDate, Description = vm.Description, Status = vm.Status };
        foreach (var gid in vm.GenreIds.Distinct()) m.MovieGenres.Add(new MovieGenre { GenreId = gid });
        _db.Movies.Add(m);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm phim.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var m = await _db.Movies.Include(x => x.MovieGenres).FirstOrDefaultAsync(x => x.MovieId == id);
        if (m == null) return NotFound();
        return View(new MovieFormVm
        {
            MovieId = m.MovieId, Title = m.Title, Duration = m.Duration, ReleaseDate = m.ReleaseDate,
            Description = m.Description, Status = m.Status,
            GenreIds = m.MovieGenres.Select(g => g.GenreId).ToList(),
            AllGenres = await _db.Genres.OrderBy(g => g.GenreName).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, MovieFormVm vm)
    {
        var m = await _db.Movies.Include(x => x.MovieGenres).FirstOrDefaultAsync(x => x.MovieId == id);
        if (m == null) return NotFound();
        if (!ModelState.IsValid) { vm.AllGenres = await _db.Genres.ToListAsync(); return View(vm); }
        m.Title = vm.Title; m.Duration = vm.Duration; m.ReleaseDate = vm.ReleaseDate; m.Description = vm.Description; m.Status = vm.Status;
        var wanted = vm.GenreIds.Distinct().ToHashSet();
        foreach (var old in m.MovieGenres.Where(g => !wanted.Contains(g.GenreId)).ToList()) m.MovieGenres.Remove(old);
        foreach (var gid in wanted.Where(g => m.MovieGenres.All(x => x.GenreId != g)))
            m.MovieGenres.Add(new MovieGenre { MovieId = m.MovieId, GenreId = gid });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật phim.";
        return RedirectToAction(nameof(Index));
    }
}
