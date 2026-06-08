# InCleanHome.Backend

Backend en **.NET 9 / ASP.NET Core** para la plataforma **InCleanHome**, construido siguiendo
**Domain-Driven Design + Clean Architecture** y persistido en **PostgreSQL** (vía EF Core +
Npgsql). Diseñado para conectar 1:1 con el frontend Vue 3 / Vite incluido en el proyecto
hermano `InCleanHome.ViteVueJS.Frontend`.

---

## 1. Requisitos

| Herramienta | Versión |
| ----------- | ------- |
| .NET SDK    | 9.0+    |
| PostgreSQL  | 14+     |

## 2. Configuración de la base de datos

Edita `InCleanHome.API/appsettings.json` y ajusta la `DefaultConnection` si tu
Postgres local usa credenciales distintas:



Crea la base de datos vacía:

```sql
CREATE DATABASE incleanhome;
```

Las tablas se crean **automáticamente** la primera vez que arranca la API
(`context.Database.EnsureCreated()` en `Program.cs`). No necesitas correr migraciones
manuales para esta primera versión.

## 3. Levantar el backend

```bash
cd InCleanHome.API
dotnet restore
dotnet run
```

La API queda escuchando en `http://localhost:5000`, que es exactamente la `baseURL`
que el frontend tiene configurada en `src/Shared/api.js` (`http://localhost:5000/api`).

Swagger disponible en `http://localhost:5000/swagger`.

## 4. Levantar el frontend contra este backend

```bash
cd InCleanHome.ViteVueJS.Frontend
npm install
npm run dev
```

El frontend levanta en `http://localhost:5173` y, gracias a la política CORS abierta del
backend (`AllowAllPolicy`), puede consumir la API sin más configuración.

---

## 5. Arquitectura

Cada **Bounded Context** sigue la misma estructura de capas Clean Architecture / DDD:

```
<BoundedContext>/
├── Domain/
│   ├── Model/
│   │   ├── Aggregates/      ← agregados raíz (User, WorkerProfile, BookingRequest, …)
│   │   ├── Commands/        ← records de input para command handlers
│   │   ├── Queries/         ← records de input para query handlers
│   │   └── ValueObjects/    ← VO inmutables (UserRole, BookingStatus, …)
│   ├── Repositories/        ← contratos persistencia (IXxxRepository)
│   └── Services/            ← contratos de aplicación (IXxxCommandService, IXxxQueryService)
├── Application/
│   ├── Internal/
│   │   ├── CommandServices/ ← orquestadores de escritura
│   │   ├── QueryServices/   ← orquestadores de lectura
│   │   └── OutboundServices/← puertos a infra externa (hashing, JWT)
│   └── ACL/                 ← anti-corruption layer hacia otros BCs
├── Infrastructure/
│   ├── Persistence/EFC/Repositories/ ← implementaciones EF Core
│   ├── Hashing/ Tokens/ Pipeline/    ← solo en IAM
└── Interfaces/
    ├── REST/
    │   ├── Controllers/
    │   ├── Resources/       ← DTOs entrada/salida
    │   └── Transform/       ← assemblers entity ↔ resource
    └── ACL/                 ← contratos publicados a otros BCs
```

### 5.1 Bounded Contexts

| BC | Responsabilidad |
| -- | --------------- |
| **IAM** | Cuentas, login (JWT), roles (`client`/`worker`/`admin`), upload de documentos del worker. |
| **Profiles** | Perfil de cliente (nombre, teléfono) y de worker (nombre, edad, género, servicios, zonas, tarifa, experiencia, bio, rating acumulado). |
| **SearchAndCatalog** | Búsqueda con filtros y disponibilidad semanal del worker. |
| **Booking** | Ciclo completo de reserva (`pending → accepted → completed/rejected/cancelled`), cómputo automático de comisión 10%. |
| **Payments** | Métodos de pago off-platform (cash, card, yape, plin, bank_transfer) con manejo de "default". |
| **ReviewsAndEvaluation** | Reseñas 1-5 ★ + comentario por booking. Dispara recálculo de stats del worker vía ACL de Profiles. |
| **Messaging** | Chat directo entre usuarios (mensajes + listado de conversaciones + lectura automática). |

### 5.2 Cross-context interactions (vía ACL)

* `Booking` consulta `IProfilesContextFacade` para resolver nombres y `HourlyRate` del worker.
* `ReviewsAndEvaluation` llama a `IProfilesContextFacade.RegisterWorkerCompletedService` para
  refrescar `AverageRating` / `TotalServices` después de crear una reseña.
* `Messaging` usa `IProfilesContextFacade.FetchUserNameByUserId` para construir la vista de
  conversaciones.

Ningún BC referencia agregados de otro BC directamente.

---

## 6. Endpoints (mapa completo)

> Todas las rutas anteponen `/api`. Excepto `auth/*`, todas requieren JWT en
> `Authorization: Bearer <token>`.

### IAM
| Método | Ruta | Descripción |
| ------ | ---- | ----------- |
| POST | `/api/auth/login` | Login. Devuelve `{ user, token }`. |
| POST | `/api/auth/register/client` | Registro cliente + perfil. |
| POST | `/api/auth/register/worker` | Registro worker + perfil (queda pendiente de subir documentos). |
| POST | `/api/auth/worker/upload-document` | Sube PDF base64 (`background_check` o `experience`). Cuando ambos están subidos, `documentsVerified=true` y el worker puede loguearse. |

### SearchAndCatalog (Workers)
| Método | Ruta | Descripción |
| ------ | ---- | ----------- |
| GET  | `/api/workers` | Búsqueda con filtros: `serviceType`, `zone`, `gender`, `minAge`, `maxAge`, `maxHourlyRate`, `minRating`. |
| GET  | `/api/workers/{id}` | Worker por user-id. |
| GET  | `/api/workers/{id}/availability` | Disponibilidad semanal. |
| PUT  | `/api/workers/{id}/availability` | Reemplaza disponibilidad (replace-all). |
| GET  | `/api/workers/me/profile` | Mi perfil de worker. |
| PUT  | `/api/workers/me/profile` | Actualiza mi perfil de worker. |
| GET  | `/api/workers/me/stats` | Stats del dashboard (net earnings, fee, completed, rating, ganancias por mes). |

### Booking
| Método | Ruta | Descripción |
| ------ | ---- | ----------- |
| POST  | `/api/bookings` | Cliente crea reserva. |
| GET   | `/api/bookings` | Lista mis bookings (vista cliente o worker según rol). |
| PATCH | `/api/bookings/{id}/status` | Cambia estado: `accepted`/`rejected` (worker) · `cancelled` (cliente) · `completed` (worker). |

### Payments
| Método | Ruta | Descripción |
| ------ | ---- | ----------- |
| GET    | `/api/payments/methods` | Mis métodos de pago. |
| POST   | `/api/payments/methods` | Registra nuevo método. |
| PATCH  | `/api/payments/methods/{id}/default` | Marca como predeterminado. |
| DELETE | `/api/payments/methods/{id}` | Elimina (promueve otro como default si era el default). |

### ReviewsAndEvaluation
| Método | Ruta | Descripción |
| ------ | ---- | ----------- |
| POST | `/api/reviews` | Cliente crea reseña sobre un booking. Refresca rating del worker. |
| GET  | `/api/reviews/worker/{id}` | Reseñas de un worker. |

### Messaging
| Método | Ruta | Descripción |
| ------ | ---- | ----------- |
| GET  | `/api/messages/conversations` | Mis conversaciones (un row por peer con último mensaje + unread). |
| GET  | `/api/messages/{userId}` | Hilo entre yo y `userId`. Marca como leídos al abrir. |
| POST | `/api/messages/{userId}` | Envía mensaje. |

---

## 7. Decisiones de diseño dignas de mención

* **`int` IDs** auto-incrementales para que los enlaces del frontend (`/worker/3`,
  `/client/messages/7`) sigan funcionando como vienen escritos.
* **`WorkerProfile.UserId` se expone como `id` en la API** (no la PK del perfil), porque
  el frontend siempre identifica al worker por su `User.Id`.
* **`text[]` de PostgreSQL** para `ServiceTypes` y `Zones` (Npgsql mapea
  `List<string>` nativamente). Esto permite consultas con `Contains(filter)` traducidas
  a operadores de array.
* **Comisión 10%** se calcula dentro del agregado `BookingRequest` en el momento de
  crear el booking — los totales nunca se recalculan a posteriori.
* **`MarkAsRead` automático** al abrir el hilo (`GET /api/messages/{userId}`), siguiendo
  la UX del componente `ChatView.vue`.
* **Validación de transiciones de estado** encapsulada en el agregado: `Accept`/`Reject`
  solo desde `pending`, `Complete` solo desde `accepted`, etc.
* **Auto-default en payment methods**: el primer método registrado es default; al borrar
  el default se promueve otro automáticamente.

## 8. Estructura del repo

```
InCleanHome.Backend/
├── InCleanHome.Backend.sln
└── InCleanHome.API/
    ├── InCleanHome.API.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Properties/launchSettings.json
    ├── Shared/
    ├── IAM/
    ├── Profiles/
    ├── SearchAndCatalog/
    ├── Booking/
    ├── Payments/
    ├── ReviewsAndEvaluation/
    └── Messaging/
```

---

## 9. Probar end-to-end

1. Levanta el backend: `dotnet run` en `InCleanHome.API`.
2. Levanta el frontend: `npm run dev` en `InCleanHome.ViteVueJS.Frontend`.
3. Abre `http://localhost:5173`.
4. Regístrate como **cliente** → entras directo a buscar workers.
5. Regístrate como **worker** → te redirige a `/upload-documents` (sube los 2 PDFs base64),
   logueate de nuevo y entras al dashboard.
