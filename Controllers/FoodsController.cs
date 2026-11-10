using System.ComponentModel.DataAnnotations;
using CinemaManagement.Data;
using CinemaManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize]
public class FoodsController : Controller
{
    private readonly CinemaDbContext _db;

    public FoodsController(CinemaDbContext db) => _db = db;

    public async Task<IActionResult> Index(string q)
    {
        var foods = _db.FoodItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
            foods = foods.Where(food => food.FoodName.Contains(q.Trim()));

        ViewBag.Search = q;
        return View(await foods.OrderBy(food => food.FoodName).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new FoodFormVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FoodFormVm vm)
    {
        NormalizeFoodName(vm);
        if (!ModelState.IsValid) return View(vm);

        _db.FoodItems.Add(new FoodItem
        {
            FoodName = vm.FoodName,
            Price = vm.Price,
            Status = "Active"
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var food = await _db.FoodItems.FindAsync(id);
        if (food == null) return NotFound();

        return View(new FoodFormVm
        {
            FoodId = food.FoodId,
            FoodName = food.FoodName,
            Price = food.Price
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FoodFormVm vm)
    {
        NormalizeFoodName(vm);
        if (!ModelState.IsValid) return View(vm);

        var food = await _db.FoodItems.FindAsync(vm.FoodId);
        if (food == null) return NotFound();

        food.FoodName = vm.FoodName;
        food.Price = vm.Price;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var food = await _db.FoodItems.FindAsync(id);
        if (food == null) return NotFound();

        food.Status = food.Status switch
        {
            "Active" => "Inactive",
            "Inactive" => "Active",
            _ => food.Status
        };
        if (food.Status is not ("Active" or "Inactive"))
        {
            TempData["Error"] = "Không thể đổi trạng thái sản phẩm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = food.Status == "Active" ? "Đã bán lại sản phẩm." : "Đã ngừng bán sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    private void NormalizeFoodName(FoodFormVm vm)
    {
        vm.FoodName = vm.FoodName?.Trim();
        if (string.IsNullOrEmpty(vm.FoodName))
            ModelState.AddModelError(nameof(vm.FoodName), "Nhập tên sản phẩm.");
    }
}

public class FoodFormVm
{
    public int FoodId { get; set; }

    [Required(ErrorMessage = "Nhập tên sản phẩm.")]
    [StringLength(100, ErrorMessage = "Tên sản phẩm tối đa 100 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string FoodName { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá phải từ 0 đến 100.000.000.")]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }
}
