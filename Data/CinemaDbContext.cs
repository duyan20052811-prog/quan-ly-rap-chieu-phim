using CinemaManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Data;

public class CinemaDbContext : DbContext
{
    public CinemaDbContext(DbContextOptions<CinemaDbContext> options) : base(options) { }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<MovieGenre> MovieGenres { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<FoodItem> FoodItems { get; set; }
    public DbSet<FoodOrder> FoodOrders { get; set; }
    public DbSet<FoodOrderDetail> FoodOrderDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<MovieGenre>().HasKey(x => new { x.MovieId, x.GenreId });
        b.Entity<FoodOrderDetail>().HasKey(x => new { x.FoodOrderId, x.FoodId });

        b.Entity<Account>().HasIndex(a => a.Username).IsUnique();
        b.Entity<Genre>().HasIndex(g => g.GenreName).IsUnique();
        b.Entity<Seat>().HasIndex(s => new { s.RoomId, s.SeatRow, s.SeatNumber }).IsUnique();
        // Không cho bán trùng ghế trong cùng suất chiếu (vé đã hủy được bán lại)
        b.Entity<Ticket>().HasIndex(t => new { t.ShowtimeId, t.SeatId })
            .IsUnique().HasFilter("[ticket_status] <> 'Cancelled'");

        b.Entity<MovieGenre>().HasOne(x => x.Movie).WithMany(m => m.MovieGenres).HasForeignKey(x => x.MovieId);
        b.Entity<MovieGenre>().HasOne(x => x.Genre).WithMany(g => g.MovieGenres).HasForeignKey(x => x.GenreId);

        // Tránh multiple cascade paths trên SQL Server
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
