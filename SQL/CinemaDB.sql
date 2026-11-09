/* =====================================================================
   HỆ THỐNG QUẢN LÝ RẠP CHIẾU PHIM - Microsoft SQL Server
   Chạy toàn bộ script trong SSMS hoặc Visual Studio (SQL Server Object Explorer)
   Tài khoản mẫu: admin / admin123   |   staff / staff123
   ===================================================================== */
IF DB_ID('CinemaDB') IS NULL CREATE DATABASE CinemaDB;
GO
USE CinemaDB;
GO

-- Xóa bảng cũ (nếu có) theo thứ tự phụ thuộc
DROP TABLE IF EXISTS food_order_detail;
DROP TABLE IF EXISTS food_order;
DROP TABLE IF EXISTS food_item;
DROP TABLE IF EXISTS payment;
DROP TABLE IF EXISTS ticket;
DROP TABLE IF EXISTS booking;
DROP TABLE IF EXISTS showtime;
DROP TABLE IF EXISTS seat;
DROP TABLE IF EXISTS room;
DROP TABLE IF EXISTS movie_genre;
DROP TABLE IF EXISTS genre;
DROP TABLE IF EXISTS movie;
DROP TABLE IF EXISTS customer;
DROP TABLE IF EXISTS account;
GO

CREATE TABLE account (
    account_id    INT IDENTITY(1,1) PRIMARY KEY,
    username      NVARCHAR(50)  NOT NULL,
    password_hash NVARCHAR(64)  NOT NULL,
    role          NVARCHAR(20)  NOT NULL CHECK (role IN ('Admin','Staff')),
    status        NVARCHAR(20)  NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Locked')),
    CONSTRAINT UQ_account_username UNIQUE (username)
);

CREATE TABLE customer (
    customer_id INT IDENTITY(1,1) PRIMARY KEY,
    account_id  INT NULL REFERENCES account(account_id),
    full_name   NVARCHAR(100) NOT NULL,
    phone       NVARCHAR(20)  NULL,
    email       NVARCHAR(100) NULL
);

CREATE TABLE movie (
    movie_id     INT IDENTITY(1,1) PRIMARY KEY,
    title        NVARCHAR(200) NOT NULL,
    duration     INT NOT NULL CHECK (duration > 0),          -- phút
    release_date DATE NOT NULL,
    description  NVARCHAR(MAX) NULL,
    status       NVARCHAR(20) NOT NULL DEFAULT 'NowShowing'
                 CHECK (status IN ('NowShowing','ComingSoon','Stopped'))
);

CREATE TABLE genre (
    genre_id   INT IDENTITY(1,1) PRIMARY KEY,
    genre_name NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE movie_genre (
    movie_id INT NOT NULL REFERENCES movie(movie_id),
    genre_id INT NOT NULL REFERENCES genre(genre_id),
    PRIMARY KEY (movie_id, genre_id)
);

CREATE TABLE room (
    room_id   INT IDENTITY(1,1) PRIMARY KEY,
    room_name NVARCHAR(50) NOT NULL UNIQUE,
    capacity  INT NOT NULL CHECK (capacity > 0),
    status    NVARCHAR(20) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Maintenance'))
);

CREATE TABLE seat (
    seat_id     INT IDENTITY(1,1) PRIMARY KEY,
    room_id     INT NOT NULL REFERENCES room(room_id),
    seat_row    NVARCHAR(2) NOT NULL,
    seat_number INT NOT NULL,
    seat_type   NVARCHAR(20) NOT NULL DEFAULT 'Normal' CHECK (seat_type IN ('Normal','VIP')),
    status      NVARCHAR(20) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Broken')),
    CONSTRAINT UQ_seat_position UNIQUE (room_id, seat_row, seat_number)
);

CREATE TABLE showtime (
    showtime_id INT IDENTITY(1,1) PRIMARY KEY,
    movie_id    INT NOT NULL REFERENCES movie(movie_id),
    room_id     INT NOT NULL REFERENCES room(room_id),
    start_time  DATETIME NOT NULL,
    end_time    DATETIME NOT NULL,
    price       DECIMAL(18,0) NOT NULL CHECK (price >= 0),
    status      NVARCHAR(20) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Cancelled')),
    CONSTRAINT CK_showtime_time CHECK (end_time > start_time)
);

CREATE TABLE booking (
    booking_id     INT IDENTITY(1,1) PRIMARY KEY,
    customer_id    INT NULL REFERENCES customer(customer_id),
    booking_time   DATETIME NOT NULL DEFAULT GETDATE(),
    booking_status NVARCHAR(20) NOT NULL DEFAULT 'Paid' CHECK (booking_status IN ('Pending','Paid','Cancelled')),
    total_amount   DECIMAL(18,0) NOT NULL CHECK (total_amount >= 0)
);

CREATE TABLE ticket (
    ticket_id     INT IDENTITY(1,1) PRIMARY KEY,
    booking_id    INT NOT NULL REFERENCES booking(booking_id),
    showtime_id   INT NOT NULL REFERENCES showtime(showtime_id),
    seat_id       INT NOT NULL REFERENCES seat(seat_id),
    ticket_price  DECIMAL(18,0) NOT NULL CHECK (ticket_price >= 0),
    ticket_status NVARCHAR(20) NOT NULL DEFAULT 'Sold' CHECK (ticket_status IN ('Sold','Used','Cancelled'))
);
-- Ngăn bán trùng ghế trong cùng suất chiếu (vé đã hủy thì được bán lại)
CREATE UNIQUE INDEX UQ_ticket_showtime_seat ON ticket(showtime_id, seat_id)
    WHERE ticket_status <> 'Cancelled';

CREATE TABLE payment (
    payment_id       INT IDENTITY(1,1) PRIMARY KEY,
    booking_id       INT NOT NULL REFERENCES booking(booking_id),
    amount           DECIMAL(18,0) NOT NULL CHECK (amount >= 0),
    payment_method   NVARCHAR(30) NOT NULL,
    payment_status   NVARCHAR(20) NOT NULL DEFAULT 'Paid',
    paid_at          DATETIME NULL,
    transaction_code NVARCHAR(50) NULL
);

CREATE TABLE food_item (
    food_id   INT IDENTITY(1,1) PRIMARY KEY,
    food_name NVARCHAR(100) NOT NULL,
    price     DECIMAL(18,0) NOT NULL CHECK (price >= 0),
    status    NVARCHAR(20) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Inactive'))
);

CREATE TABLE food_order (
    food_order_id INT IDENTITY(1,1) PRIMARY KEY,
    booking_id    INT NOT NULL REFERENCES booking(booking_id),
    order_time    DATETIME NOT NULL DEFAULT GETDATE(),
    order_status  NVARCHAR(20) NOT NULL DEFAULT 'Paid',
    total_amount  DECIMAL(18,0) NOT NULL CHECK (total_amount >= 0)
);

CREATE TABLE food_order_detail (
    food_order_id INT NOT NULL REFERENCES food_order(food_order_id),
    food_id       INT NOT NULL REFERENCES food_item(food_id),
    quantity      INT NOT NULL CHECK (quantity > 0),
    unit_price    DECIMAL(18,0) NOT NULL CHECK (unit_price >= 0),
    PRIMARY KEY (food_order_id, food_id)
);
GO

/* ===================== DỮ LIỆU MẪU ===================== */
INSERT INTO account(username, password_hash, role) VALUES
 ('admin','240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9','Admin'),
 ('staff','10176E7B7B24D317ACFCF8D2064CFD2F24E154F7B5A96603077D5EF813D6A6B6','Staff');

INSERT INTO genre(genre_name) VALUES (N'Hành động'),(N'Hài'),(N'Tình cảm'),(N'Kinh dị'),(N'Hoạt hình'),(N'Khoa học viễn tưởng');

INSERT INTO movie(title,duration,release_date,description,status) VALUES
 (N'Biệt đội săn mồi',120,DATEADD(DAY,-10,GETDATE()),N'Phim hành động nghẹt thở.','NowShowing'),
 (N'Chuyện tình mùa thu',105,DATEADD(DAY,-20,GETDATE()),N'Câu chuyện tình lãng mạn.','NowShowing'),
 (N'Ngôi nhà ma ám',95,DATEADD(DAY,-5,GETDATE()),N'Phim kinh dị tâm lý.','NowShowing'),
 (N'Thế giới phép thuật',90,DATEADD(DAY,-30,GETDATE()),N'Phim hoạt hình gia đình.','NowShowing'),
 (N'Hành tinh cuối cùng',140,DATEADD(DAY,15,GETDATE()),N'Phim viễn tưởng sắp chiếu.','ComingSoon');

INSERT INTO movie_genre VALUES (1,1),(2,3),(2,2),(3,4),(4,5),(4,2),(5,6),(5,1);

INSERT INTO room(room_name,capacity) VALUES (N'Phòng 1',50),(N'Phòng 2',40);

-- Sinh ghế: Phòng 1 = 5 hàng (A-E) x 10 ghế, Phòng 2 = 4 hàng (A-D) x 10 ghế; 1 hàng cuối là VIP
DECLARE @r INT, @n INT, @rows INT, @room INT;
SET @room = 1;
WHILE @room <= 2
BEGIN
    SET @rows = CASE WHEN @room = 1 THEN 5 ELSE 4 END;
    SET @r = 0;
    WHILE @r < @rows
    BEGIN
        SET @n = 1;
        WHILE @n <= 10
        BEGIN
            INSERT INTO seat(room_id, seat_row, seat_number, seat_type)
            VALUES (@room, CHAR(65 + @r), @n, CASE WHEN @r = @rows - 1 THEN 'VIP' ELSE 'Normal' END);
            SET @n += 1;
        END
        SET @r += 1;
    END
    SET @room += 1;
END
GO

DECLARE @today DATETIME = CAST(CAST(GETDATE() AS DATE) AS DATETIME);
INSERT INTO showtime(movie_id,room_id,start_time,end_time,price) VALUES
 (1,1,DATEADD(HOUR,9,@today),  DATEADD(MINUTE,9*60+135,@today), 80000),
 (1,1,DATEADD(HOUR,14,@today), DATEADD(MINUTE,14*60+135,@today),90000),
 (2,2,DATEADD(HOUR,10,@today), DATEADD(MINUTE,10*60+120,@today),80000),
 (3,2,DATEADD(HOUR,19,@today), DATEADD(MINUTE,19*60+110,@today),90000),
 (4,1,DATEADD(HOUR,33,@today), DATEADD(MINUTE,33*60+105,@today),70000),
 (2,2,DATEADD(HOUR,38,@today), DATEADD(MINUTE,38*60+120,@today),85000);

INSERT INTO food_item(food_name,price) VALUES
 (N'Bắp rang bơ (M)',45000),(N'Bắp rang bơ (L)',60000),(N'Nước ngọt',30000),(N'Combo 1 bắp + 2 nước',110000);

INSERT INTO customer(full_name,phone,email) VALUES (N'Nguyễn Văn A','0901234567','a@example.com');
GO
