using System.ComponentModel.DataAnnotations;
using CinemaManagement.Data;
using CinemaManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize]
public class GenresController : Controller
{
    private readonly CinemaDbContext _db;

    public GenresController(CinemaDbContext db) => _db = db;

    public async Task<IActionResult> Index(string searchString)
    {
        var genres = _db.Genres.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchString))
            genres = genres.Where(g => g.GenreName.Contains(searchString.Trim()));

        var rows = await genres
            .OrderBy(g => g.GenreName)
            .Select(g => new GenreRowVm
            {
                GenreId = g.GenreId,
                GenreName = g.GenreName,
                MovieCount = g.MovieGenres.Count
            })
            .ToListAsync();

        ViewBag.SearchString = searchString;
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create() => View(new GenreFormVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GenreFormVm vm)
    {
        vm.GenreName = vm.GenreName?.Trim();
        ValidateGenreName(vm);

        if (ModelState.IsValid)
        {
            if (await IsDuplicateNameAsync(vm.GenreName))
                ModelState.AddModelError(nameof(vm.GenreName), "Tên thể loại này đã tồn tại.");
            else
            {
                _db.Genres.Add(new Genre { GenreName = vm.GenreName });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã thêm thể loại thành công.";
                return RedirectToAction(nameof(Index));
            }
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var genre = await _db.Genres.AsNoTracking().FirstOrDefaultAsync(g => g.GenreId == id);
        if (genre == null) return NotFound();

        return View(new GenreFormVm { GenreId = genre.GenreId, GenreName = genre.GenreName });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GenreFormVm vm)
    {
        if (id != vm.GenreId) return BadRequest();

        vm.GenreName = vm.GenreName?.Trim();
        ValidateGenreName(vm);

        if (ModelState.IsValid)
        {
            var genre = await _db.Genres.FindAsync(id);
            if (genre == null) return NotFound();

            if (await IsDuplicateNameAsync(vm.GenreName, id))
                ModelState.AddModelError(nameof(vm.GenreName), "Tên thể loại này đã tồn tại.");
            else
            {
                genre.GenreName = vm.GenreName;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật thể loại thành công.";
                return RedirectToAction(nameof(Index));
            }
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await _db.Genres.FindAsync(id);
        if (genre == null) return NotFound();

        if (await _db.MovieGenres.AnyAsync(mg => mg.GenreId == id))
        {
            TempData["Error"] = "Không thể xóa thể loại đang được gắn với phim.";
            return RedirectToAction(nameof(Index));
        }

        _db.Genres.Remove(genre);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa thể loại thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsDuplicateNameAsync(string name, int? exceptGenreId = null) =>
        await _db.Genres.AnyAsync(g =>
            g.GenreName == name &&
            (!exceptGenreId.HasValue || g.GenreId != exceptGenreId.Value));

    private void ValidateGenreName(GenreFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.GenreName))
            ModelState.AddModelError(nameof(vm.GenreName), "Vui lòng nhập tên thể loại.");
        else if (vm.GenreName.Length > 50)
            ModelState.AddModelError(nameof(vm.GenreName), "Tên thể loại không được vượt quá 50 ký tự.");
    }
}

public class GenreFormVm
{
    public int GenreId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thể loại.")]
    [StringLength(50, ErrorMessage = "Tên thể loại không được vượt quá 50 ký tự.")]
    [Display(Name = "Tên thể loại")]
    public string GenreName { get; set; }
}

public class GenreRowVm
{
    public int GenreId { get; set; }
    public string GenreName { get; set; }
    public int MovieCount { get; set; }
}
