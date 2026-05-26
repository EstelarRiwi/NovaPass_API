# NovaPass API

## Descripción

Sistema de gestión y venta de boletas para teatro. API REST centralizada construida con ASP.NET Core 10, diseñada para soportar múltiples PWAs con roles diferenciados.

## Stack tecnológico

| Componente | Tecnología |
|---|---|
| Runtime | .NET 10 / ASP.NET Core 10 |
| Lenguaje | C# |
| Base de datos | PostgreSQL 16 (Database-First, sin migraciones EF Core) |
| ORM | Entity Framework Core 9 + Npgsql |
| Autenticación | JWT (HmacSha256) + Google OAuth2 |
| Hash de contraseñas | BCrypt.Net |
| Notificaciones | n8n self-hosted (webhooks) |
| Contenedores | Docker + Docker Compose |
| CI/CD | GitHub Actions → ghcr.io → VPS Ubuntu via SSH |

## Arquitectura del sistema

El sistema expone una única API que sirve a cuatro PWAs con roles distintos:

| PWA | Rol | Descripción |
|---|---|---|
| Pública | `customer` | Compra de boletas, consulta de eventos, favoritos |
| Admin | `admin` | Gestión de eventos, empleados, PQRS y reportes |
| Taquilla | `seller` | Venta presencial de boletas |
| Acceso | `scanner` | Validación de QR en puerta |

## Estructura del proyecto

```
NovaPass_API/
├── Controllers/
│   └── AuthController.cs
├── Data/
│   └── TicketEventsDbContext.cs
├── DTOs/
│   └── Auth/
│       └── AuthDTOs.cs
├── Helpers/
│   └── JwtHelper.cs
├── Models/
│   ├── Enums.cs
│   ├── Event.cs
│   ├── Favorite.cs
│   ├── PasswordResetToken.cs
│   ├── Payment.cs
│   ├── Pqr.cs
│   ├── PqrsResponse.cs
│   ├── Seat.cs
│   ├── Ticket.cs
│   ├── TicketCategory.cs
│   ├── TokenBlacklist.cs
│   └── User.cs
├── Services/
│   ├── AuthService.cs
│   └── Interfaces/
│       └── IAuthService.cs
├── Program.cs
├── appsettings.json
└── docker-compose.yml
```

## Base de datos

El esquema es **Database-First**: el scaffold de EF Core se corrió contra la base existente. No se usan migraciones.

### Tablas

| Tabla | Descripción |
|---|---|
| `users` | Usuarios del sistema (customers, admin, seller, scanner) |
| `events` | Eventos de teatro con fechas, venue e imagen |
| `ticket_categories` | Categorías de precio por evento (VIP, General, etc.) |
| `seats` | Asientos disponibles por categoría |
| `tickets` | Boletas vendidas, con estado y QR token |
| `payments` | Pagos vía Mercado Pago, con referencia y estado |
| `favorites` | Relación N:M entre usuarios y eventos (PK compuesta) |
| `password_reset_tokens` | Tokens de recuperación de contraseña (persistidos en DB) |
| `token_blacklist` | JTIs revocados al hacer logout (persistidos en DB) |
| `pqrs` | Peticiones, quejas, reclamos y sugerencias |
| `pqrs_responses` | Respuestas de admins a los PQRS |

### Enums de PostgreSQL

| Enum C# | Tipo PostgreSQL | Valores |
|---|---|---|
| `UserRole` | `user_role` | `customer`, `seller`, `scanner`, `admin` |
| `EventStatus` | `event_status` | `active`, `cancelled`, `sold_out` |
| `TicketStatus` | `ticket_status` | `pending`, `active`, `used`, `cancelled` |
| `PaymentStatus` | `payment_status` | `pending`, `approved`, `rejected` |
| `PqrsType` | `pqrs_type` | `question`, `complaint`, `claim`, `suggestion` |
| `PqrsStatus` | `pqrs_status` | `pending`, `in_progress`, `resolved`, `closed` |

## Módulos implementados

### Auth Module (completo)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/v1/auth/register` | No | Registro de nuevo usuario (rol: customer) |
| POST | `/api/v1/auth/login` | No | Login con email y contraseña |
| POST | `/api/v1/auth/google` | No | Login / registro con Google ID Token |
| GET | `/api/v1/auth/me` | JWT | Perfil del usuario autenticado |
| POST | `/api/v1/auth/logout` | JWT | Revoca el token (persiste JTI en DB) |
| POST | `/api/v1/auth/forgot-password` | No | Genera token de recuperación y dispara n8n |
| POST | `/api/v1/auth/reset-password` | No | Restablece contraseña con token válido |
| PUT | `/api/v1/auth/profile` | JWT | Actualiza foto, nombre y teléfono |
| POST | `/api/v1/auth/avatar` | JWT | Sube imagen de perfil a `/wwwroot/avatars/` |
| POST | `/api/v1/auth/employees` | JWT (admin) | Crea empleado con rol y permisos |
| DELETE | `/api/v1/auth/employees/{id}` | JWT (admin) | Desactiva empleado (soft delete) |

### Payments Module (pendiente)

Integración con Mercado Pago pendiente de implementación. La tabla `payments` ya existe en el schema.

## JWT

**Algoritmo:** HmacSha256

**Payload:**

```json
{
  "sub": "userId (UUID CHAR-36)",
  "email": "user@email.com",
  "role": "customer|seller|scanner|admin",
  "permissions": ["taquilla", "acceso"],
  "jti": "unique-guid",
  "exp": 1234567890
}
```

Los tokens revocados se almacenan en la tabla `token_blacklist` y se validan en cada request via el evento `OnTokenValidated` del middleware JWT.

## Rate Limiting

| Endpoint | Límite | Ventana |
|---|---|---|
| `/register` | 5 requests | Por hora, por IP |
| `/login` | 10 requests | Por minuto, por IP |

Respuesta al superar el límite: `HTTP 429 Too Many Requests`.

## Variables de entorno

| Variable | Descripción |
|---|---|
| `JWT_SECRET` | Clave secreta para firmar los tokens JWT |
| `JWT_ISSUER` | Issuer del token JWT |
| `JWT_AUDIENCE` | Audience del token JWT |
| `DB_CONNECTION_STRING` | Cadena de conexión completa a PostgreSQL |
| `POSTGRES_PASSWORD` | Contraseña de PostgreSQL (usada en Docker Compose) |
| `GOOGLE_CLIENT_ID` | Client ID de Google OAuth2 |
| `GOOGLE_CLIENT_SECRET` | Client Secret de Google OAuth2 |
| `N8N_WEBHOOK_URL` | URL del webhook de n8n para notificaciones |
| `N8N_USER` | Usuario de acceso a n8n |
| `N8N_PASSWORD` | Contraseña de acceso a n8n |
| `MONGODB_CONNECTION_STRING` | Conexión a MongoDB (uso futuro) |
| `SERVER_HOST` | IP o hostname del VPS para deploy |
| `SERVER_SSH_KEY` | Clave SSH privada para GitHub Actions |
| `TOKEN` | Token de acceso a ghcr.io (GitHub Container Registry) |

## Configuración local

Crea `appsettings.Development.json` con la siguiente estructura (nunca valores reales en el repo):

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=novapass;Username=postgres;Password=..."
  },
  "Jwt": {
    "Secret": "...",
    "Issuer": "...",
    "Audience": "...",
    "ExpiryHours": "8"
  },
  "Google": {
    "ClientId": "..."
  },
  "N8N": {
    "WebhookUrl": "..."
  }
}
```

## Cómo correr el proyecto localmente

```bash
git clone https://github.com/EstelarRiwi/NovaPass_API.git
cd NovaPass_API

# Configura appsettings.Development.json con tus variables locales
dotnet restore
dotnet run

# Scalar UI disponible en: https://localhost:{port}/scalar/v1
```

## Docker

```bash
docker compose up -d
```

Los contenedores expuestos:

| Contenedor | Puerto | Descripción |
|---|---|---|
| `novapass-api` | 8080 | ASP.NET Core API |
| `novapass-postgres` | 5432 | PostgreSQL 16 |
| `novapass-mongo` | 27017 | MongoDB |
| `novapass-n8n` | 5678 | n8n workflow automation |

## Infraestructura VPS

- **IP:** 5.189.174.154
- **OS:** Ubuntu 24
- **Contenedores:** Docker Compose con los 4 servicios listados arriba

## n8n Workflows

| Estado | Flujo |
|--------|---|
| Ready  | Bienvenida al registrarse (en modo test) |
| -      | Recuperación de contraseña |
| -      | Ticket comprado |
| -      | Venta en taquilla |
| -      | Evento actualizado |
| -      | PQRS respondida |

## CI/CD

El pipeline de GitHub Actions se activa en cada push a `main`:

1. Build y test de la imagen Docker
2. Push de la imagen a `ghcr.io` (GitHub Container Registry)
3. Deploy en el VPS via SSH: pull de la nueva imagen + `docker compose up -d`


