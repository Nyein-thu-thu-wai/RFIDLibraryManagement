# RFID Library Management System

A starter full-stack project based on the supplied RFID Library Management System diagrams.

## Stack
- Backend: ASP.NET Core Web API (.NET 8)
- ORM: Entity Framework Core + Pomelo MySQL provider
- Frontend: Vue 3 + Vite + Axios
- Database: MySQL

## Included features
- Book catalogue CRUD and search by title, author, call number
- Patron registration, update, delete and membership validation
- RFID tag registration and scan lookup
- Borrow and return workflow with transaction records
- Dashboard summary and overdue report
- Basic MARC text import/export starter endpoints

## 1. Create the MySQL database
Open MySQL Workbench or the MySQL terminal and run:

```sql
CREATE DATABASE rfid_library CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

Edit `backend/RfidLibraryApi/appsettings.json` and replace `YOUR_MYSQL_PASSWORD` with your local MySQL password.

The API uses `EnsureCreated()` on startup to create the tables for this starter project. For a long-lived project, switch to EF Core migrations before changing the schema.

## 2. Run the backend
Install the .NET 8 SDK, then:

```bash
cd backend/RfidLibraryApi
dotnet restore
dotnet run --urls http://localhost:5000
```

Open Swagger:
http://localhost:5000/swagger

## 3. Run the frontend
Install Node.js 20+ and npm, then open a second terminal:

```bash
cd frontend/rfid-library
npm install
npm run dev
```

Open:
http://localhost:5173

## 4. Suggested first test
1. Add a book from Manage Books.
2. Register a member from Patrons.
3. Register an RFID tag and select the book.
4. Open Borrow / Return.
5. Enter the generated member ID and RFID number, then Borrow.
6. Return the same RFID number.
7. Check Transactions and Dashboard.

## API routes
- `GET/POST /api/books`
- `GET/PUT/DELETE /api/books/{id}`
- `GET/POST /api/patrons`
- `PUT/DELETE /api/patrons/{id}`
- `GET /api/patrons/{id}/validate`
- `GET/POST /api/rfidtags`
- `GET /api/rfidtags/{rfidNo}/scan`
- `DELETE /api/rfidtags/{rfidNo}`
- `POST /api/circulation/borrow`
- `POST /api/circulation/return`
- `GET /api/circulation/transactions`
- `GET /api/reports/summary`
- `GET /api/reports/overdue`
- `GET /api/marc/export`
- `POST /api/marc/import`

## Important limitations
- This is a functional learning/MVP starter, not a production-secured library system.
- Authentication, role-based authorization, audit logs, fine calculation, backups and deployment hardening are not included yet.
- RFID scan endpoint currently accepts a tag number through HTTP. Connect the actual RFID reader using its device SDK, serial/TCP protocol or a local reader service.
- MARC import/export here is a simple text-record starter, not a complete MARC21 binary/ISO2709 implementation.
- The PDF's ERD includes Librarian and RFID Reader entities. Their model classes are included, but full management screens/endpoints for them are not yet implemented.
- If your installed SDK is .NET 9 or .NET 10 only, install .NET 8 SDK or update the target framework and compatible EF/Pomelo package versions together.
