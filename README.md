# 🚖 UberCore — Real-Time Ride Matching System

A production-minded, clean architecture Real-Time Ride Matching System built with **.NET 10 / ASP.NET Core**, **C#**, **Entity Framework Core (SQLite)**, **Redis Geospatial (GEOADD / GEORADIUS)**, **ASP.NET Core SignalR**, **BackgroundService + System.Threading.Channels**, and a modern **React + Vite + Leaflet + Tailwind CSS** frontend.

---

## 🏛️ Clean Architecture

```
Ride/
├── src/
│   ├── RideMatching.Domain/           # Entities (Driver, Rider, Ride), ValueObjects, Enums, Domain Events
│   ├── RideMatching.Application/      # DTOs, FluentValidation, Interfaces, Services, GeoCalculator
│   ├── RideMatching.Infrastructure/   # EF Core DbContext, SQLite, Redis Geo Store, Channels Worker
│   └── RideMatching.Api/              # Controllers, SignalR RideHub, Middleware, Serilog, Swagger
│
├── tests/
│   ├── RideMatching.UnitTests/        # Domain, GeoCalculator, Service & Validator tests (Moq, FluentAssertions)
│   └── RideMatching.IntegrationTests/ # WebApplicationFactory End-to-End API tests
│
└── frontend/
    └── ride-matching-web/             # React (Vite) + Leaflet Map + SignalR + Tailwind CSS (Dual & Single Views)
```

---

## ⚡ Core Features & Workflows

### 🚗 Driver Workflow
1. **Register Driver** with vehicle details (Make, Model, License Plate, Color, Type).
2. **Go Online / Offline** toggle with instant status broadcasting.
3. **Real-time GPS Location Updates** stored in Redis Geospatial index and broadcasted via SignalR.
4. **Incoming Ride Offer HUD** with 15-second countdown timer and audio/visual alerts.
5. **Accept / Reject Ride** transitions.
6. **Simulated GPS Movement** (turn-by-turn driving step-by-step towards pickup and dropoff).
7. **Start & Complete Ride** with dynamic fare and ratings.

### 👤 Rider Workflow
1. **Register / Select Rider**.
2. **Interactive Map Pickup & Destination** selection (Click on Leaflet map or select presets).
3. **Instant Fare Estimation** (Distance, Duration, Base Fare, Total).
4. **Request Ride** triggers asynchronous channel queue & background matching worker.
5. **Live Tracking Radar** while scanning for nearby available drivers.
6. **Live Driver Assignment** showing driver info, rating, car details, and license plate.
7. **Cancel Ride** at any stage before completion.
8. **Celebration & Digital Receipt** on completion with rating.

### 🔄 Real-Time SignalR Events
| Event | Direction | Payload | Description |
| :--- | :--- | :--- | :--- |
| `DriverLocationUpdated` | Server ➔ Clients | `{ driverId, latitude, longitude, address, timestamp }` | Live GPS marker movements |
| `DriverStatusChanged` | Server ➔ Clients | `{ driverId, status }` | Online / Available / Assigned / Offline |
| `RideRequested` | Server ➔ Clients | `RideResponse` | New ride created |
| `RideMatching` | Server ➔ Rider | `{ rideId, riderId, attempt }` | Search status & radius expansion |
| `RideOffer` | Server ➔ Candidate Driver | `{ rideId, driverId, ride }` | Targeted offer to nearest driver |
| `RideMatched` | Server ➔ Rider & Driver | `{ rideId, driverId, ride }` | Driver assigned |
| `RideAccepted` | Server ➔ All in Ride | `RideResponse` | Driver accepted & en route |
| `RideRejected` | Server ➔ Driver & System | `{ rideId, driverId, reason }` | Driver declined; re-matching |
| `RideStarted` | Server ➔ Rider & Driver | `RideResponse` | Passenger picked up |
| `RideCompleted` | Server ➔ Rider & Driver | `RideResponse` | Trip completed, fare finalized |
| `RideCancelled` | Server ➔ All in Ride | `{ ride, reason }` | Cancellation broadcast |

---

## 🚀 Running the Project

### 1. Start the .NET 10 Web API Backend
```bash
cd src/RideMatching.Api
dotnet run --urls=http://localhost:5000
```
- **API Swagger UI**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Health Check**: [http://localhost:5000/health](http://localhost:5000/health)
- **SignalR Hub**: `http://localhost:5000/hubs/ride`

### 2. Start the React Frontend
```bash
cd frontend/ride-matching-web
npm run dev
```
- **App URL**: [http://localhost:5173](http://localhost:5173)

---

## 🧪 Testing

```bash
# Run Unit & Integration Tests
dotnet test RideMatching.slnx
```
