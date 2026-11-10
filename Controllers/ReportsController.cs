using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly CinemaDbContext _db;

    public ReportsController(CinemaDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string groupBy = "day")
    {
        var today = DateTime.Today;
        var startDate = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var endDate = (to ?? today).Date;
        var model = new ReportsPageVm
        {
            Summary = new ReportVm { From = startDate, To = endDate },
            GroupBy = groupBy
        };

        if (groupBy is not ("day" or "month"))
            ModelState.AddModelError(nameof(groupBy), "Chọn cách nhóm theo ngày hoặc tháng.");
        if (endDate < startDate)
            ModelState.AddModelError(nameof(to), "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
        else if (groupBy == "day" && (endDate - startDate).TotalDays > 365)
            ModelState.AddModelError(nameof(to), "Khoảng ngày không được vượt quá 366 ngày khi nhóm theo ngày.");
        else if (groupBy == "month"
            && ((endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month) > 59)
            ModelState.AddModelError(nameof(to), "Khoảng báo cáo không được vượt quá 60 tháng.");

        if (!ModelState.IsValid)
            return View(model);

        var endExclusive = endDate.AddDays(1);
        var paidTickets = _db.Tickets.AsNoTracking()
            .Where(ticket => ticket.Booking.BookingStatus == "Paid"
                && ticket.TicketStatus != "Cancelled"
                && ticket.Booking.BookingTime >= startDate
                && ticket.Booking.BookingTime < endExclusive);
        var paidFoodOrders = _db.FoodOrders.AsNoTracking()
            .Where(order => order.Booking.BookingStatus == "Paid"
                && order.OrderStatus == "Paid"
                && order.Booking.BookingTime >= startDate
                && order.Booking.BookingTime < endExclusive);

        Dictionary<DateTime, (int Count, decimal Revenue)> ticketTotals;
        Dictionary<DateTime, decimal> foodTotals;
        if (groupBy == "month")
        {
            ticketTotals = await paidTickets
                .GroupBy(ticket => new
                {
                    ticket.Booking.BookingTime.Year,
                    ticket.Booking.BookingTime.Month
                })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Count = group.Count(),
                    Revenue = group.Sum(ticket => ticket.TicketPrice)
                })
                .ToDictionaryAsync(
                    row => new DateTime(row.Year, row.Month, 1),
                    row => (row.Count, row.Revenue));

            foodTotals = await paidFoodOrders
                .GroupBy(order => new
                {
                    order.Booking.BookingTime.Year,
                    order.Booking.BookingTime.Month
                })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Revenue = group.Sum(order => order.TotalAmount)
                })
                .ToDictionaryAsync(
                    row => new DateTime(row.Year, row.Month, 1),
                    row => row.Revenue);
        }
        else
        {
            ticketTotals = await paidTickets
                .GroupBy(ticket => ticket.Booking.BookingTime.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    Count = group.Count(),
                    Revenue = group.Sum(ticket => ticket.TicketPrice)
                })
                .ToDictionaryAsync(row => row.Date, row => (row.Count, row.Revenue));

            foodTotals = await paidFoodOrders
                .GroupBy(order => order.Booking.BookingTime.Date)
                .Select(group => new
                {
                    Date = group.Key,
                    Revenue = group.Sum(order => order.TotalAmount)
                })
                .ToDictionaryAsync(row => row.Date, row => row.Revenue);
        }

        var periods = new List<DateTime>();
        if (groupBy == "month")
        {
            var period = new DateTime(startDate.Year, startDate.Month, 1);
            var lastPeriod = new DateTime(endDate.Year, endDate.Month, 1);
            while (period <= lastPeriod)
            {
                periods.Add(period);
                period = period.AddMonths(1);
            }
        }
        else
        {
            for (var period = startDate; period <= endDate; period = period.AddDays(1))
                periods.Add(period);
        }

        var summary = model.Summary;
        summary.Labels = periods.Select(period => groupBy == "month"
            ? period.ToString("yyyy-MM")
            : period.ToString("dd/MM/yyyy")).ToList();
        model.TicketCountSeries = periods.Select(period => ticketTotals.GetValueOrDefault(period).Count).ToList();
        summary.TicketSeries = periods.Select(period => ticketTotals.GetValueOrDefault(period).Revenue).ToList();
        summary.FoodSeries = periods.Select(period => foodTotals.GetValueOrDefault(period)).ToList();
        summary.TicketCount = ticketTotals.Values.Sum(value => value.Count);
        summary.TicketRevenue = ticketTotals.Values.Sum(value => value.Revenue);
        summary.FoodRevenue = foodTotals.Values.Sum();

        summary.TopMovies = await paidTickets
            .GroupBy(ticket => new { ticket.Showtime.Movie.MovieId, ticket.Showtime.Movie.Title })
            .Select(group => new TopMovieRow
            {
                Title = group.Key.Title,
                Tickets = group.Count(),
                Revenue = group.Sum(ticket => ticket.TicketPrice)
            })
            .OrderByDescending(movie => movie.Revenue)
            .ThenBy(movie => movie.Title)
            .ToListAsync();

        return View(model);
    }
}

public class ReportsPageVm
{
    public ReportVm Summary { get; set; }
    public string GroupBy { get; set; }
    public List<int> TicketCountSeries { get; set; } = new();
}
