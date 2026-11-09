using System.ComponentModel.DataAnnotations;
using CinemaManagement.Models;

namespace CinemaManagement.ViewModels;

public class LoginVm
{
    [Required(ErrorMessage = "Nhập tên đăng nhập"), Display(Name = "Tên đăng nhập")] public string Username { get; set; }
    [Required(ErrorMessage = "Nhập mật khẩu"), DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; }
}

public class MovieFormVm
{
    public int MovieId { get; set; }
    [Required(ErrorMessage = "Nhập tên phim"), StringLength(200), Display(Name = "Tên phim")] public string Title { get; set; }
    [Range(1, 600, ErrorMessage = "Thời lượng 1-600 phút"), Display(Name = "Thời lượng (phút)")] public int Duration { get; set; } = 100;
    [DataType(DataType.Date), Display(Name = "Ngày phát hành")] public DateTime ReleaseDate { get; set; } = DateTime.Today;
    [Display(Name = "Mô tả")] public string Description { get; set; }
    [Display(Name = "Trạng thái")] public string Status { get; set; } = "NowShowing";
    public List<int> GenreIds { get; set; } = new();
    public List<Genre> AllGenres { get; set; } = new();
}

public class RoomCreateVm
{
    [Required(ErrorMessage = "Nhập tên phòng"), StringLength(50), Display(Name = "Tên phòng")] public string RoomName { get; set; }
    [Range(1, 26, ErrorMessage = "1-26 hàng"), Display(Name = "Số hàng ghế")] public int Rows { get; set; } = 5;
    [Range(1, 30, ErrorMessage = "1-30 ghế"), Display(Name = "Số ghế mỗi hàng")] public int SeatsPerRow { get; set; } = 10;
    [Range(0, 26), Display(Name = "Số hàng VIP (hàng cuối)")] public int VipRows { get; set; } = 1;
}

public class ShowtimeCreateVm
{
    [Required, Display(Name = "Phim")] public int MovieId { get; set; }
    [Required, Display(Name = "Phòng chiếu")] public int RoomId { get; set; }
    [Required, Display(Name = "Giờ bắt đầu")] public DateTime StartTime { get; set; } = DateTime.Today.AddDays(1).AddHours(9);
    [Range(0, 10000000), Display(Name = "Giá vé cơ bản")] public decimal Price { get; set; } = 80000;
}

public class AccountCreateVm
{
    [Required, StringLength(50), Display(Name = "Tên đăng nhập")] public string Username { get; set; }
    [Required, MinLength(6, ErrorMessage = "Tối thiểu 6 ký tự"), DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; }
    [Display(Name = "Vai trò")] public string Role { get; set; } = "Staff";
}

public class SeatMapVm
{
    public Showtime Showtime { get; set; }
    public List<Seat> Seats { get; set; }
    public HashSet<int> TakenSeatIds { get; set; }
    public List<FoodItem> Foods { get; set; }
    public decimal VipPrice => Math.Round(Showtime.Price * 1.2m / 1000m) * 1000m;
}

public class ShowtimeRow
{
    public Showtime Showtime { get; set; }
    public int Sold { get; set; }
    public int Capacity { get; set; }
}

public class ReportVm
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<string> Labels { get; set; } = new();
    public List<decimal> TicketSeries { get; set; } = new();
    public List<decimal> FoodSeries { get; set; } = new();
    public decimal TicketRevenue { get; set; }
    public decimal FoodRevenue { get; set; }
    public int TicketCount { get; set; }
    public decimal Total => TicketRevenue + FoodRevenue;
    public List<TopMovieRow> TopMovies { get; set; } = new();
}

public class TopMovieRow
{
    public string Title { get; set; }
    public int Tickets { get; set; }
    public decimal Revenue { get; set; }
}
