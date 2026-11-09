using CinemaManagement.Data;
using CinemaManagement.Helpers;
using CinemaManagement.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CinemaDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("CinemaDb")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Denied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Nếu chưa chạy script SQL: tự tạo DB từ model và seed tài khoản admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    db.Database.EnsureCreated();
    if (!db.Accounts.Any())
    {
        db.Accounts.Add(new Account { Username = "admin", PasswordHash = PasswordHasher.Hash("admin123"), Role = "Admin" });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
