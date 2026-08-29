# e-Dhaq

Online Laundry Ordering & Delivery System with two separate apps:

1. **Web App** - ASP.NET Core 9 Razor Pages with REST API
2. **Mobile App** - Flutter app for customers and drivers

## Solution Structure

### Web App (`src/eDhaq.Web`)

- Razor Pages UI for Admin, Customer, Driver, Staff, Cashier roles
- REST API under `/api` for mobile clients
- JWT Bearer authentication for the API
- SignalR hub for real-time tracking
- EF Core with MySQL

### Mobile App (`edhaq_mobile`)

- Flutter app for iOS and Android
- Connects to the web app's REST API
- JWT token authentication
- Customer features: dashboard, orders, addresses, notifications, profile

## Prerequisites

- .NET SDK 9
- MySQL Server 8+
- Flutter SDK 3.10+

## Configuration

### Web App

Update connection string in:

- `src/eDhaq.Web/appsettings.json`
- `src/eDhaq.Web/appsettings.Development.json`

Default key:

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;port=3306;database=edhaq_db;user=root;password=yourpassword"
}
```

Also configure:

- `GoogleMaps:ApiKey`
- `Smtp` settings for email notifications
- `Jwt` settings for mobile API authentication

### Mobile App

Update the API base URL in `edhaq_mobile/lib/core/constants/app_constants.dart`:

```dart
// Android emulator -> host loopback is 10.0.2.2. Backend runs on port 5058 (http).
static const String baseUrl = 'http://10.0.2.2:5058';
// Use for iOS simulator / web / physical device on local network:
// static const String baseUrl = 'http://localhost:5058';
```

## Run

### Web App

```bash
dotnet restore
dotnet build eDhaq.sln
dotnet run --project src/eDhaq.Web
```

The web app runs on:
- HTTP: `http://localhost:5058`
- HTTPS: `https://localhost:7013`

### Mobile App

```bash
cd edhaq_mobile
flutter pub get
flutter run
```

## Migrations

Initial migration already generated in `src/eDhaq.Data/Migrations`.

Apply migration:

```bash
dotnet ef database update --project src/eDhaq.Data --startup-project src/eDhaq.Web
```

## Seeded Data

Startup seeding creates:

- Roles: `Administrator`, `Manager`, `Cashier`, `LaundryStaff`, `PickupDriver`, `DeliveryDriver`, `Customer`
- Admin user:
  - Email: `admin@edhaq.com`
  - Password: `Admin@123!`
- Demo customer:
  - Email: `customer@edhaq.com`
  - Password: `Customer@123!`
- Cities, service categories, laundry services
- Default settings and sample coupons

## API Endpoints

The web app exposes the following REST API endpoints for the mobile app:

- `POST /api/auth/login` - Login
- `POST /api/auth/register` - Register
- `GET /api/auth/me` - Get current user
- `POST /api/auth/logout` - Logout
- `GET /api/auth/cities` - Get cities
- `GET /api/auth/cities/{cityId}/villages` - Get villages
- `GET /api/auth/villages/{villageId}/subvillages` - Get sub-villages
- `GET /api/locations/cities` - Get cities
- `GET /api/locations/cities/{cityId}/villages` - Get villages
- `GET /api/locations/villages/{villageId}/subvillages` - Get sub-villages
- `GET /api/services/categories` - Get service categories
- `GET /api/services` - Get services
- `GET /api/addresses` - Get addresses
- `POST /api/addresses` - Create address
- `DELETE /api/addresses/{id}` - Delete address
- `GET /api/orders` - Get orders (paged)
- `GET /api/orders/{id}` - Get order details
- `POST /api/orders` - Create order
- `POST /api/orders/{id}/confirm-delivery` - Confirm delivery
- `GET /api/notifications` - Get notifications
- `POST /api/notifications/{id}/read` - Mark notification as read
- `GET /api/dashboard/customer` - Get customer dashboard

## Notes

- A placeholder logo is included at `src/eDhaq.Web/wwwroot/images/logo.svg`.
- Replace it with your uploaded official e-Dhaq logo as needed.