using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaManagement.Models;

[Table("account")]
public class Account
{
    [Key, Column("account_id")] public int AccountId { get; set; }
    [Column("username"), Required, StringLength(50), Display(Name = "Tên đăng nhập")] public string Username { get; set; }
    [Column("password_hash"), Required, StringLength(64)] public string PasswordHash { get; set; }
    [Column("role"), Required, StringLength(20), Display(Name = "Vai trò")] public string Role { get; set; } = "Staff";
    [Column("status"), Required, StringLength(20), Display(Name = "Trạng thái")] public string Status { get; set; } = "Active";
}

[Table("customer")]
public class Customer
{
    [Key, Column("customer_id")] public int CustomerId { get; set; }
    [Column("account_id")] public int? AccountId { get; set; }
    [Column("full_name"), Required, StringLength(100)] public string FullName { get; set; }
    [Column("phone"), StringLength(20)] public string Phone { get; set; }
    [Column("email"), StringLength(100)] public string Email { get; set; }
    public Account Account { get; set; }
}

[Table("movie")]
public class Movie
{
    [Key, Column("movie_id")] public int MovieId { get; set; }
    [Column("title"), Required, StringLength(200)] public string Title { get; set; }
    [Column("duration")] public int Duration { get; set; }
    [Column("release_date", TypeName = "date")] public DateTime ReleaseDate { get; set; }
    [Column("description")] public string Description { get; set; }
    [Column("status"), Required, StringLength(20)] public string Status { get; set; } = "NowShowing";
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}

[Table("genre")]
public class Genre
{
    [Key, Column("genre_id")] public int GenreId { get; set; }
    [Column("genre_name"), Required, StringLength(50)] public string GenreName { get; set; }
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}

[Table("movie_genre")]
public class MovieGenre
{
    [Column("movie_id")] public int MovieId { get; set; }
    [Column("genre_id")] public int GenreId { get; set; }
    public Movie Movie { get; set; }
    public Genre Genre { get; set; }
}

[Table("room")]
public class Room
{
    [Key, Column("room_id")] public int RoomId { get; set; }
    [Column("room_name"), Required, StringLength(50)] public string RoomName { get; set; }
    [Column("capacity")] public int Capacity { get; set; }
    [Column("status"), Required, StringLength(20)] public string Status { get; set; } = "Active";
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}

[Table("seat")]
public class Seat
{
    [Key, Column("seat_id")] public int SeatId { get; set; }
    [Column("room_id")] public int RoomId { get; set; }
    [Column("seat_row"), Required, StringLength(2)] public string SeatRow { get; set; }
    [Column("seat_number")] public int SeatNumber { get; set; }
    [Column("seat_type"), Required, StringLength(20)] public string SeatType { get; set; } = "Normal";
    [Column("status"), Required, StringLength(20)] public string Status { get; set; } = "Active";
    public Room Room { get; set; }
    public string Label => $"{SeatRow}{SeatNumber}";
}

[Table("showtime")]
public class Showtime
{
    [Key, Column("showtime_id")] public int ShowtimeId { get; set; }
    [Column("movie_id")] public int MovieId { get; set; }
    [Column("room_id")] public int RoomId { get; set; }
    [Column("start_time")] public DateTime StartTime { get; set; }
    [Column("end_time")] public DateTime EndTime { get; set; }
    [Column("price", TypeName = "decimal(18,0)")] public decimal Price { get; set; }
    [Column("status"), Required, StringLength(20)] public string Status { get; set; } = "Active";
    public Movie Movie { get; set; }
    public Room Room { get; set; }
}

[Table("booking")]
public class Booking
{
    [Key, Column("booking_id")] public int BookingId { get; set; }
    [Column("customer_id")] public int? CustomerId { get; set; }
    [Column("booking_time")] public DateTime BookingTime { get; set; } = DateTime.Now;
    [Column("booking_status"), Required, StringLength(20)] public string BookingStatus { get; set; } = "Paid";
    [Column("total_amount", TypeName = "decimal(18,0)")] public decimal TotalAmount { get; set; }
    public Customer Customer { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<FoodOrder> FoodOrders { get; set; } = new List<FoodOrder>();
}

[Table("ticket")]
public class Ticket
{
    [Key, Column("ticket_id")] public int TicketId { get; set; }
    [Column("booking_id")] public int BookingId { get; set; }
    [Column("showtime_id")] public int ShowtimeId { get; set; }
    [Column("seat_id")] public int SeatId { get; set; }
    [Column("ticket_price", TypeName = "decimal(18,0)")] public decimal TicketPrice { get; set; }
    [Column("ticket_status"), Required, StringLength(20)] public string TicketStatus { get; set; } = "Sold";
    public Booking Booking { get; set; }
    public Showtime Showtime { get; set; }
    public Seat Seat { get; set; }
}

[Table("payment")]
public class Payment
{
    [Key, Column("payment_id")] public int PaymentId { get; set; }
    [Column("booking_id")] public int BookingId { get; set; }
    [Column("amount", TypeName = "decimal(18,0)")] public decimal Amount { get; set; }
    [Column("payment_method"), Required, StringLength(30)] public string PaymentMethod { get; set; }
    [Column("payment_status"), Required, StringLength(20)] public string PaymentStatus { get; set; } = "Paid";
    [Column("paid_at")] public DateTime? PaidAt { get; set; }
    [Column("transaction_code"), StringLength(50)] public string TransactionCode { get; set; }
    public Booking Booking { get; set; }
}

[Table("food_item")]
public class FoodItem
{
    [Key, Column("food_id")] public int FoodId { get; set; }
    [Column("food_name"), Required, StringLength(100), Display(Name = "Tên sản phẩm")] public string FoodName { get; set; }
    [Column("price", TypeName = "decimal(18,0)"), Range(0, 100000000), Display(Name = "Giá")] public decimal Price { get; set; }
    [Column("status"), Required, StringLength(20), Display(Name = "Trạng thái")] public string Status { get; set; } = "Active";
}

[Table("food_order")]
public class FoodOrder
{
    [Key, Column("food_order_id")] public int FoodOrderId { get; set; }
    [Column("booking_id")] public int BookingId { get; set; }
    [Column("order_time")] public DateTime OrderTime { get; set; } = DateTime.Now;
    [Column("order_status"), Required, StringLength(20)] public string OrderStatus { get; set; } = "Paid";
    [Column("total_amount", TypeName = "decimal(18,0)")] public decimal TotalAmount { get; set; }
    public Booking Booking { get; set; }
    public ICollection<FoodOrderDetail> Details { get; set; } = new List<FoodOrderDetail>();
}

[Table("food_order_detail")]
public class FoodOrderDetail
{
    [Column("food_order_id")] public int FoodOrderId { get; set; }
    [Column("food_id")] public int FoodId { get; set; }
    [Column("quantity")] public int Quantity { get; set; }
    [Column("unit_price", TypeName = "decimal(18,0)")] public decimal UnitPrice { get; set; }
    public FoodOrder FoodOrder { get; set; }
    public FoodItem FoodItem { get; set; }
}
