using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize(Roles = "Admin")]
public class RoomsController : Controller
{
    private readonly CinemaDbContext _db;
    public RoomsController(CinemaDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Rooms.Include(r => r.Seats).OrderBy(r => r.RoomName).ToListAsync());

    public IActionResult Create() => View(new RoomCreateVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoomCreateVm vm)
    {
        if (vm.VipRows > vm.Rows) ModelState.AddModelError(nameof(vm.VipRows), "Số hàng VIP không được lớn hơn số hàng.");
        if (await _db.Rooms.AnyAsync(r => r.RoomName == vm.RoomName)) ModelState.AddModelError(nameof(vm.RoomName), "Tên phòng đã tồn tại.");
        if (!ModelState.IsValid) return View(vm);

        var room = new Room { RoomName = vm.RoomName, Capacity = vm.Rows * vm.SeatsPerRow };
        for (int r = 0; r < vm.Rows; r++)
            for (int n = 1; n <= vm.SeatsPerRow; n++)
                room.Seats.Add(new Seat
                {
                    SeatRow = ((char)('A' + r)).ToString(),
                    SeatNumber = n,
                    SeatType = r >= vm.Rows - vm.VipRows ? "VIP" : "Normal"
                });
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo {room.RoomName} với {room.Capacity} ghế.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var r = await _db.Rooms.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = r.Status == "Active" ? "Maintenance" : "Active";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
