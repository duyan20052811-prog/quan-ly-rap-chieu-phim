using System.Security.Claims;
using CinemaManagement.Data;
using CinemaManagement.Helpers;
using CinemaManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

public class AccountController : Controller
{
    private readonly CinemaDbContext _db;
    public AccountController(CinemaDbContext db) => _db = db;

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(new LoginVm()); }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);
        var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.Username == vm.Username);
        if (acc == null || !PasswordHasher.Verify(vm.Password, acc.PasswordHash))
        {
            ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
            return View(vm);
        }
        if (acc.Status != "Active")
        {
            ModelState.AddModelError("", "Tài khoản đã bị khóa.");
            return View(vm);
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, acc.AccountId.ToString()),
            new(ClaimTypes.Name, acc.Username),
            new(ClaimTypes.Role, acc.Role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(principal);
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] public IActionResult Denied() => View();
}
