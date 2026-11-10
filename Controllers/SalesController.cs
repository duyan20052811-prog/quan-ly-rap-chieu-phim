using System.ComponentModel.DataAnnotations;
using System.Data;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize]
public class SalesController : Controller
{
    private static readonly string[] PaymentMethods = ["Cash", "Card", "Transfer"];
    private readonly CinemaDbContext _db;

    public SalesController(CinemaDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var tomorrow = now.Date.AddDays(1);
        var showtimes = await _db.Showtimes.AsNoTracking()
            .Include(showtime => showtime.Movie)
            .Include(showtime => showtime.Room)
            .Where(showtime => showtime.Status == "Active"
                && showtime.Room.Status == "Active"
                && showtime.StartTime >= now
                && showtime.StartTime < tomorrow)
            .OrderBy(showtime => showtime.StartTime)
            .ToListAsync();

        return View(showtimes);
    }

    [HttpGet]
    public async Task<IActionResult> Seats(int id)
    {
        if (!await IsAvailableShowtimeAsync(id, DateTime.Now))
            return NotFound();

        return await RenderSeatsAsync(id, new SaleCheckoutVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([Bind(Prefix = "Checkout")] SaleCheckoutVm vm)
    {
        vm.SeatIds ??= new List<int>();
        vm.FoodQuantities ??= new Dictionary<int, int>();
        var now = DateTime.Now;
        var showtime = await GetAvailableShowtimeAsync(vm.ShowtimeId, now);
        if (showtime == null)
            return NotFound();

        if (vm.SeatIds.Count == 0)
            ModelState.AddModelError(nameof(vm.SeatIds), "Chọn ít nhất một ghế.");
        if (vm.SeatIds.Count != vm.SeatIds.Distinct().Count())
            ModelState.AddModelError(nameof(vm.SeatIds), "Danh sách ghế không hợp lệ.");
        if (!PaymentMethods.Contains(vm.PaymentMethod))
            ModelState.AddModelError(nameof(vm.PaymentMethod), "Chọn phương thức thanh toán hợp lệ.");

        var foodQuantities = new Dictionary<int, int>();
        foreach (var (foodId, quantity) in vm.FoodQuantities)
        {
            if (quantity is < 0 or > 99)
            {
                ModelState.AddModelError(nameof(vm.FoodQuantities), "Số lượng mỗi món phải từ 0 đến 99.");
                continue;
            }

            if (quantity > 0)
                foodQuantities[foodId] = quantity;
        }

        var seats = await _db.Seats.AsNoTracking()
            .Where(seat => seat.RoomId == showtime.RoomId
                && seat.Status == "Active"
                && vm.SeatIds.Contains(seat.SeatId))
            .OrderBy(seat => seat.SeatRow)
            .ThenBy(seat => seat.SeatNumber)
            .ToListAsync();
        if (seats.Count != vm.SeatIds.Distinct().Count())
            ModelState.AddModelError(nameof(vm.SeatIds), "Có ghế không hợp lệ hoặc không còn sử dụng.");

        var alreadySold = await _db.Tickets.AsNoTracking()
            .Where(ticket => ticket.ShowtimeId == showtime.ShowtimeId
                && ticket.TicketStatus != "Cancelled"
                && vm.SeatIds.Contains(ticket.SeatId))
            .Select(ticket => ticket.SeatId)
            .ToListAsync();
        if (alreadySold.Count > 0)
            ModelState.AddModelError(nameof(vm.SeatIds), "Một hoặc nhiều ghế vừa được bán. Vui lòng chọn lại.");

        var foods = foodQuantities.Count == 0
            ? new List<FoodItem>()
            : await _db.FoodItems.AsNoTracking()
                .Where(food => food.Status == "Active" && foodQuantities.Keys.Contains(food.FoodId))
                .ToListAsync();
        if (foods.Count != foodQuantities.Count)
            ModelState.AddModelError(nameof(vm.FoodQuantities), "Một hoặc nhiều món đã ngừng bán. Vui lòng tải lại trang.");

        if (!ModelState.IsValid)
            return await RenderSeatsAsync(vm.ShowtimeId, vm);

        var vipPrice = Math.Round(showtime.Price * 1.2m / 1000m) * 1000m;
        var total = seats.Sum(seat => seat.SeatType == "VIP" ? vipPrice : showtime.Price)
            + foods.Sum(food => food.Price * foodQuantities[food.FoodId]);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var booking = new Booking
            {
                BookingTime = now,
                BookingStatus = "Paid",
                TotalAmount = total
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            var tickets = seats.Select(seat => new Ticket
            {
                BookingId = booking.BookingId,
                ShowtimeId = showtime.ShowtimeId,
                SeatId = seat.SeatId,
                TicketPrice = seat.SeatType == "VIP" ? vipPrice : showtime.Price,
                TicketStatus = "Sold"
            }).ToList();
            _db.Tickets.AddRange(tickets);

            _db.Payments.Add(new Payment
            {
                BookingId = booking.BookingId,
                Amount = total,
                PaymentMethod = vm.PaymentMethod,
                PaymentStatus = "Paid",
                PaidAt = now,
                TransactionCode = $"BK{booking.BookingId:D6}"
            });

            var foodOrder = new FoodOrder
            {
                BookingId = booking.BookingId,
                OrderTime = now,
                OrderStatus = "Paid",
                TotalAmount = foods.Sum(food => food.Price * foodQuantities[food.FoodId])
            };
            _db.FoodOrders.Add(foodOrder);
            foreach (var food in foods)
            {
                _db.FoodOrderDetails.Add(new FoodOrderDetail
                {
                    FoodOrder = foodOrder,
                    FoodId = food.FoodId,
                    Quantity = foodQuantities[food.FoodId],
                    UnitPrice = food.Price
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToAction(nameof(Receipt), new { id = booking.BookingId });
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is SqlException sqlException
                && sqlException.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            ModelState.AddModelError(nameof(vm.SeatIds), "Một hoặc nhiều ghế vừa được bán. Vui lòng chọn lại.");
            return await RenderSeatsAsync(vm.ShowtimeId, vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var booking = await LoadBookingAsync(id);
        return booking == null ? NotFound() : View(booking);
    }

    [HttpGet]
    public async Task<IActionResult> Check(string code)
    {
        var vm = new BookingCheckVm { Code = code?.Trim() };
        if (!string.IsNullOrWhiteSpace(vm.Code))
        {
            var bookingIdText = vm.Code.StartsWith("BK", StringComparison.OrdinalIgnoreCase)
                ? vm.Code[2..]
                : vm.Code;
            if (int.TryParse(bookingIdText, out var bookingId) && bookingId > 0)
                vm.Booking = await LoadBookingAsync(bookingId);

            if (vm.Booking == null)
                ModelState.AddModelError(nameof(vm.Code), "Không tìm thấy vé theo mã booking này.");
        }

        return View(vm);
    }

    private async Task<bool> IsAvailableShowtimeAsync(int showtimeId, DateTime now) =>
        await GetAvailableShowtimeAsync(showtimeId, now) != null;

    private Task<Showtime> GetAvailableShowtimeAsync(int showtimeId, DateTime now)
    {
        var tomorrow = now.Date.AddDays(1);
        return _db.Showtimes.AsNoTracking()
            .Include(showtime => showtime.Movie)
            .Include(showtime => showtime.Room)
            .FirstOrDefaultAsync(showtime => showtime.ShowtimeId == showtimeId
                && showtime.Status == "Active"
                && showtime.Room.Status == "Active"
                && showtime.StartTime >= now
                && showtime.StartTime < tomorrow);
    }

    private async Task<IActionResult> RenderSeatsAsync(int showtimeId, SaleCheckoutVm checkout)
    {
        var showtime = await GetAvailableShowtimeAsync(showtimeId, DateTime.Now);
        if (showtime == null)
            return NotFound();

        var seats = await _db.Seats.AsNoTracking()
            .Where(seat => seat.RoomId == showtime.RoomId)
            .OrderBy(seat => seat.SeatRow)
            .ThenBy(seat => seat.SeatNumber)
            .ToListAsync();
        var soldSeatIds = await _db.Tickets.AsNoTracking()
            .Where(ticket => ticket.ShowtimeId == showtimeId && ticket.TicketStatus != "Cancelled")
            .Select(ticket => ticket.SeatId)
            .ToListAsync();
        var foods = await _db.FoodItems.AsNoTracking()
            .Where(food => food.Status == "Active")
            .OrderBy(food => food.FoodName)
            .ToListAsync();

        return View("Seats", new SaleSeatsVm
        {
            SeatMap = new SeatMapVm
            {
                Showtime = showtime,
                Seats = seats,
                TakenSeatIds = soldSeatIds.ToHashSet(),
                Foods = foods
            },
            Checkout = checkout
        });
    }

    private Task<Booking> LoadBookingAsync(int bookingId) =>
        _db.Bookings.AsNoTracking()
            .Include(booking => booking.Tickets)
                .ThenInclude(ticket => ticket.Seat)
            .Include(booking => booking.Tickets)
                .ThenInclude(ticket => ticket.Showtime)
                    .ThenInclude(showtime => showtime.Movie)
            .Include(booking => booking.Tickets)
                .ThenInclude(ticket => ticket.Showtime)
                    .ThenInclude(showtime => showtime.Room)
            .Include(booking => booking.Payments)
            .Include(booking => booking.FoodOrders)
                .ThenInclude(order => order.Details)
                    .ThenInclude(detail => detail.FoodItem)
            .FirstOrDefaultAsync(booking => booking.BookingId == bookingId);
}

public class SaleSeatsVm
{
    public SeatMapVm SeatMap { get; set; }
    public SaleCheckoutVm Checkout { get; set; }
}

public class SaleCheckoutVm
{
    public int ShowtimeId { get; set; }
    public List<int> SeatIds { get; set; } = new();
    public Dictionary<int, int> FoodQuantities { get; set; } = new();

    [Required(ErrorMessage = "Chọn phương thức thanh toán.")]
    public string PaymentMethod { get; set; } = "Cash";
}

public class BookingCheckVm
{
    public string Code { get; set; }
    public Booking Booking { get; set; }
}
