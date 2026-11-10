using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CinemaManagement.Data;
using CinemaManagement.Helpers;
using CinemaManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize(Roles = "Admin")]
public class AccountsController : Controller
{
    private readonly CinemaDbContext _db;

    public AccountsController(CinemaDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var accounts = await _db.Accounts.AsNoTracking()
            .OrderBy(account => account.Username)
            .ToListAsync();
        return View(accounts);
    }

    [HttpGet]
    public IActionResult Create() => View(new AccountCreateFormVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountCreateFormVm vm)
    {
        vm.Username = vm.Username?.Trim();
        if (string.IsNullOrWhiteSpace(vm.Username))
            ModelState.AddModelError(nameof(vm.Username), "Nhập tên đăng nhập.");

        if (!ModelState.IsValid)
            return View(vm);

        if (await _db.Accounts.AnyAsync(account => account.Username == vm.Username))
        {
            ModelState.AddModelError(nameof(vm.Username), "Tên đăng nhập đã tồn tại.");
            return View(vm);
        }

        _db.Accounts.Add(new Account
        {
            Username = vm.Username,
            PasswordHash = PasswordHasher.Hash(vm.Password),
            Role = "Staff",
            Status = "Active"
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is SqlException sqlException
                && sqlException.Number is 2601 or 2627)
        {
            ModelState.AddModelError(nameof(vm.Username), "Tên đăng nhập đã tồn tại.");
            return View(vm);
        }

        TempData["Success"] = "Đã tạo tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var account = await _db.Accounts.AsNoTracking()
            .Where(item => item.AccountId == id)
            .Select(item => new { item.AccountId, item.Username })
            .FirstOrDefaultAsync();
        if (account == null) return NotFound();

        ViewBag.Username = account.Username;
        return View(new AccountPasswordResetVm { AccountId = account.AccountId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AccountPasswordResetVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Username = await _db.Accounts
                .Where(account => account.AccountId == vm.AccountId)
                .Select(account => account.Username)
                .FirstOrDefaultAsync();
            if (ViewBag.Username == null) return NotFound();
            return View(vm);
        }

        var account = await _db.Accounts.FindAsync(vm.AccountId);
        if (account == null) return NotFound();

        account.PasswordHash = PasswordHasher.Hash(vm.Password);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã đặt lại mật khẩu cho tài khoản {account.Username}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account == null) return NotFound();

        var currentAccountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (account.Status == "Active"
            && int.TryParse(currentAccountId, out var signedInAccountId)
            && account.AccountId == signedInAccountId)
        {
            TempData["Error"] = "Bạn không thể tự khóa tài khoản của mình.";
            return RedirectToAction(nameof(Index));
        }

        account.Status = account.Status == "Active" ? "Locked" : "Active";
        await _db.SaveChangesAsync();

        TempData["Success"] = account.Status == "Active"
            ? $"Đã mở lại tài khoản {account.Username}."
            : $"Đã khóa tài khoản {account.Username}.";
        return RedirectToAction(nameof(Index));
    }
}

public class AccountCreateFormVm
{
    [Required(ErrorMessage = "Nhập tên đăng nhập.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    [StringLength(64, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 64 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Nhập lại mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; }
}

public class AccountPasswordResetVm
{
    public int AccountId { get; set; }

    [Required(ErrorMessage = "Nhập mật khẩu mới.")]
    [StringLength(64, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 64 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Nhập lại mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; }
}
