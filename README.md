# ISP Billing System 🚀 (main branch test)

A full-stack ISP (Internet Service Provider) billing system with .NET 8 backend and React TypeScript frontend.

## Features

- **Authentication**: JWT-based authentication with role-based access control
- **User Management**: Admin can create and manage client accounts
- **Fee Management**: Create, track, and manage billing fees
- **Invoice Generation**: PDF invoice download for both admin and clients
- **Reports**: Income summary and reports for administrators
- **Docker Support**: Fully containerized with Docker Compose

## Tech Stack

### Backend
- .NET 8 Web API
- Entity Framework Core 8
- MySQL 8
- JWT Authentication
- BCrypt password hashing
- PdfSharpCore for PDF generation

### Frontend
- React 19 with TypeScript
- Vite build tool
- Axios for API calls
- React Router for navigation

### Infrastructure
- Docker & Docker Compose
- Nginx reverse proxy
- GitHub Actions CI/CD

## Quick Start

### Prerequisites
- Docker & Docker Compose
- Git

### Running Locally

1. Clone the repository:
```bash
git clone <repository-url>
cd isp-local-system
```

2. Start all services:
```bash
docker compose up --build
```

3. Access the application:
   - **Frontend**: http://localhost
   - **Backend API**: http://localhost/api

### Default Credentials

| Role   | Username | Password   |
|--------|----------|------------|
| Admin  | admin    | admin123   |
| Client | client1  | client123  |

## Project Structure

```
├── IspBackend/              # .NET 8 Web API
│   ├── Controllers/         # API endpoints
│   ├── Models/              # Entity models
│   ├── DTOs/                # Data transfer objects
│   ├── Services/            # Business logic
│   ├── Data/                # Database context
│   └── Dockerfile
├── frontend/frontend/       # React TypeScript app
│   ├── src/
│   │   ├── components/      # React components
│   │   └── main.tsx         # Entry point
│   ├── nginx.conf           # Nginx configuration
│   └── Dockerfile
├── docker-compose.yml       # Docker orchestration
└── .github/workflows/       # CI/CD pipelines
    ├── backend.yml
    └── frontend.yml
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - Register new user (Admin only)

### Users
- `GET /api/users` - Get all users (Admin only)
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user (Admin only)

### Fees
- `GET /api/fees` - Get all fees (Admin only)
- `GET /api/fees/my-fees` - Get current user's fees
- `POST /api/fees` - Create fee (Admin only)
- `PUT /api/fees/{id}/pay` - Mark fee as paid
- `GET /api/fees/{id}/invoice` - Download PDF invoice

### Reports (Admin only)
- `GET /api/reports/income` - Get income summary
- `GET /api/reports/income/monthly` - Get monthly breakdown

## CI/CD

GitHub Actions workflows are set up for automated CI/CD:

### Backend Workflow (`.github/workflows/backend.yml`)
- Triggers on changes to `IspBackend/` directory
- Builds and tests .NET application
- Builds and pushes Docker image to GitHub Container Registry

### Frontend Workflow (`.github/workflows/frontend.yml`)
- Triggers on changes to `frontend/frontend/` directory
- Installs dependencies, lints, and builds
- Builds and pushes Docker image to GitHub Container Registry

## Environment Variables

### Backend
| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | MySQL connection string | See docker-compose.yml |
| `Jwt__Key` | JWT signing key | (set in appsettings) |
| `Jwt__Issuer` | JWT issuer | IspBackend |
| `Jwt__Audience` | JWT audience | IspBackend |

## Development

### Backend Development
```bash
cd IspBackend
dotnet restore
dotnet run
```

### Frontend Development
```bash
cd frontend/frontend
npm install
npm run dev
```

## License

MIT License
