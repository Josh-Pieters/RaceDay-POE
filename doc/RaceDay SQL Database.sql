CREATE DATABASE RaceDayDB;

USE RaceDayDB;

CREATE TABLE Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    Role            VARCHAR(20) NOT NULL, 
    PhoneNumber     VARCHAR(20),
    CreatedAt       DATETIME DEFAULT GETDATE()
);

CREATE TABLE UserProfiles (
    ProfileID               INT IDENTITY(1,1) PRIMARY KEY,
    UserID                  INT NOT NULL UNIQUE,
    DateOfBirth              DATE,
    Gender                    VARCHAR(10),
    EmergencyContactName      VARCHAR(100),
    EmergencyContactPhone     VARCHAR(20),
    MedicalNotes              VARCHAR(500),
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

CREATE TABLE Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT NOT NULL,
    EventName       VARCHAR(150) NOT NULL,
    Description     VARCHAR(1000),
    EventDate       DATE NOT NULL,
    StartTime       TIME,
    Location        VARCHAR(200) NOT NULL,
    Province        VARCHAR(50),
    CreatedAt       DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (OrganiserID) REFERENCES Users(UserID)
);

CREATE TABLE Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT NOT NULL,
    CategoryName    VARCHAR(50) NOT NULL,
    DistanceKM      DECIMAL(5,2) NOT NULL,
    Price           DECIMAL(8,2) DEFAULT 0,
    MaxParticipants INT DEFAULT 500,
    FOREIGN KEY (EventID) REFERENCES Events(EventID)
);

CREATE TABLE Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT NOT NULL,
    CategoryID      INT NOT NULL,
    EnrolmentDate   DATETIME DEFAULT GETDATE(),
    Status          VARCHAR(20) DEFAULT 'Confirmed',
    FOREIGN KEY (ParticipantID) REFERENCES Users(UserID),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);

CREATE TABLE Results (
    ResultID                INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID             INT NOT NULL UNIQUE,
    CapturedByOrganiserID   INT NOT NULL,
    FinishTime              TIME,
    OverallPosition         INT,
    CategoryPosition        INT,
    CapturedAt              DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (EnrolmentID) REFERENCES Enrolments(EnrolmentID),
    FOREIGN KEY (CapturedByOrganiserID) REFERENCES Users(UserID)
);

INSERT INTO Users (FullName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Thandiwe Nkosi', 'thandiwe.nkosi@raceday.co.za', 'HASH1', 'Organiser', '0821234567'),
('Johan van der Merwe', 'johan.vdm@raceday.co.za', 'HASH2', 'Organiser', '0837654321'),
('Lindiwe Dube', 'lindiwe.dube@example.com', 'HASH3', 'Participant', '0729876543'),
('Ryan Petersen', 'ryan.petersen@example.com', 'HASH4', 'Participant', '0715551234');

INSERT INTO UserProfiles (UserID, DateOfBirth, Gender, EmergencyContactName, EmergencyContactPhone, MedicalNotes) VALUES
(3, '1994-03-12', 'Female', 'Nomsa Dube', '0839998888', 'None'),
(4, '1989-11-05', 'Male', 'Carla Petersen', '0821112222', 'Mild asthma');

INSERT INTO Events (OrganiserID, EventName, Description, EventDate, StartTime, Location, Province) VALUES
(1, 'Cape Town Cycle Tour', '109km road cycling race around the Cape Peninsula.', '2026-03-08', '06:00:00', 'Green Point, Cape Town', 'Western Cape'),
(1, 'Two Oceans Marathon', 'Ultra and half marathon along the Cape Peninsula coastline.', '2026-04-04', '05:30:00', 'UCT Rugby Fields, Cape Town', 'Western Cape'),
(2, 'Soweto Marathon', 'Marathon, half marathon and 10km fun run through Soweto.', '2026-11-01', '06:00:00', 'FNB Stadium, Soweto', 'Gauteng');

INSERT INTO Categories (EventID, CategoryName, DistanceKM, Price, MaxParticipants) VALUES
(1, '109km Individual', 109.00, 650.00, 15000),
(1, 'Mini Peloton 42km', 42.00, 450.00, 5000),
(2, 'Ultra Marathon 56km', 56.00, 550.00, 8000),
(2, 'Half Marathon 21.1km', 21.10, 350.00, 6000),
(3, 'Full Marathon 42.2km', 42.20, 300.00, 10000),
(3, 'Half Marathon 21.1km', 21.10, 200.00, 8000),
(3, 'Fun Run 10km', 10.00, 120.00, 5000);

INSERT INTO Enrolments (ParticipantID, CategoryID, Status) VALUES
(3, 2, 'Confirmed'),
(3, 4, 'Confirmed'),
(4, 5, 'Confirmed'),
(4, 7, 'Confirmed');

INSERT INTO Results (EnrolmentID, CapturedByOrganiserID, FinishTime, OverallPosition, CategoryPosition) VALUES
(1, 1, '01:45:32', 320, 45),
(3, 2, '03:58:10', 210, 30);
GO

SELECT * FROM Users;
SELECT * FROM UserProfiles;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;
