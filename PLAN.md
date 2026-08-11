# План реалізації: Hotel Availability API (.NET 10)

Тестове завдання: REST API для пошуку доступності номерів готелю + заглушка endpoint'у створення бронювання.

## 1. Структура проєкту

Мінімалістичне рішення без зайвих шарів (для тестового завдання окремі Domain/Infrastructure проєкти — оверкіл, тримаємо все в одному Web API проєкті з чіткими папками).

```
HotelAvailability.sln
src/
  HotelAvailability.Api/
    Program.cs
    HotelAvailability.Api.csproj        # <Nullable>enable</Nullable>, <ImplicitUsings>enable</ImplicitUsings>
    Endpoints/
      AvailabilityEndpoints.cs          # POST /api/availability/search
      BookingEndpoints.cs               # POST /api/bookings
      HealthEndpoints.cs                # GET /health (опційно, дешевий сигнал production-mindset)
    Models/
      Common/
        Money.cs
        CancellationPolicy.cs           # ієрархія record'ів, [JsonPolymorphic]/[JsonDerivedType] — спільна для domain і API
        MealPlan.cs
      Requests/
        SearchAvailabilityRequest.cs
        CreateBookingRequest.cs
        GuestInfo.cs
      Responses/
        SearchAvailabilityResponse.cs
        AvailableRoomDto.cs
        RatePlanDto.cs
      Domain/
        Hotel.cs
        Room.cs
        RatePlan.cs
    Validation/
      SearchAvailabilityRequestValidator.cs
      ValidationResultExtensions.cs
    Services/
      IHotelCatalog.cs
      InMemoryHotelCatalog.cs           # mock-каталог готелів/номерів
      IAvailabilityService.cs
      MockAvailabilityService.cs        # генерація тарифів під запит
    Infrastructure/
      ApiExceptionHandler.cs            # -> ProblemDetails
  HotelAvailability.Api.Tests/
    Validation/
      SearchAvailabilityRequestValidatorTests.cs
    Services/
      MockAvailabilityServiceTests.cs
    Endpoints/
      AvailabilityEndpointsTests.cs     # WebApplicationFactory
README.md
```

Використовуємо **Minimal API** (endpoint mapping в `Endpoints/*.cs` як extension-методи над `IEndpointRouteBuilder`) — для такого обсягу API це простіше й сучасніше за контролери, і добре демонструє DI (через `[FromServices]`/constructor injection в делегатах).

## 2. Моделі даних

Ключова ідея: розділити **Domain**-моделі (внутрішнє представлення) і **DTO** для API (Request/Response) — демонструє розуміння шарів і дає свободу еволюції API незалежно від домену. Але суворо дублювати **кожен** тип немає сенсу: `Money`, `CancellationPolicy`, `MealPlan` мають однакову форму і семантику в domain, і в API-відповіді, тож живуть в одному місці (`Models/Common`) і перевикористовуються напряму в `RatePlan` (domain) і `RatePlanDto` (response) — окремих `*Dto`-дублікатів для них немає. Domain/DTO розділяємо тільки там, де форма справді відрізняється (`Hotel`/`Room` vs агреговані `SearchAvailabilityResponse`).

### 2.1 Спільні типи (`Models/Common`)

```csharp
public sealed record Money(decimal Amount, string CurrencyCode); // ISO 4217, напр. "UAH"
```

#### Умови скасування — дискримінована унія через record-ієрархію

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NonRefundable), "nonRefundable")]
[JsonDerivedType(typeof(FreeCancellationUntil), "freeCancellationUntil")]
public abstract record CancellationPolicy
{
    public sealed record NonRefundable : CancellationPolicy;

    public sealed record FreeCancellationUntil(DateTimeOffset Deadline) : CancellationPolicy;
}
```

Без `[JsonPolymorphic]`/`[JsonDerivedType]` `System.Text.Json` **не** серіалізує ієрархію в очікуваний `{ "type": "...", ... }` — без них похідні поля (`Deadline` тощо) губляться при серіалізації через базовий тип. Це найризикованіше місце плану технічно, тому рішення фіксується тут явно, а не залишається "розібратись по ходу" на етапі реалізації.

Це дає exhaustive pattern-matching (`switch` без `default`, компілятор попередить, якщо забути кейс) і читабельніше за enum + nullable-поле.

#### Харчування

```csharp
public enum MealPlanType
{
    RoomOnly,
    Breakfast,
    HalfBoard,
    FullBoard,
    AllInclusive
}

public sealed record MealPlan(MealPlanType Type, string? Description);
```

`MealPlan?` на рівні тарифного плану — `null`, якщо харчування не входить (пряма демонстрація роботи з nullable reference types).

### 2.2 Domain-моделі (`Models/Domain`)

```csharp
public sealed record Hotel(Guid Id, string Name);

public sealed record Room(
    Guid Id,
    Guid HotelId,
    string Name,
    int MaxAdults,
    int MaxChildren);
```

### 2.3 Тарифний план і номер

```csharp
public sealed record RatePlan(
    Guid Id,
    string Name,
    Money TotalPrice,
    CancellationPolicy CancellationPolicy,
    MealPlan? MealPlan);

public sealed record AvailableRoom(
    Room Room,
    IReadOnlyList<RatePlan> RatePlans);
```

### 2.4 Запит на пошук

```csharp
public sealed record SearchAvailabilityRequest(
    Guid HotelId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Rooms,
    int Adults,
    IReadOnlyList<int>? ChildrenAges); // null/empty = без дітей
```

`DateOnly` — правильний тип для дат заїзду/виїзду без часової складової.

### 2.5 Запит на створення бронювання (тільки типи, без реалізації)

```csharp
public sealed record CreateBookingRequest(
    Guid HotelId,
    Guid RoomId,
    Guid RatePlanId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Rooms,
    int Adults,
    IReadOnlyList<int>? ChildrenAges,
    GuestInfo Guest);

public sealed record GuestInfo(
    string FirstName,
    string LastName,
    string Email,
    string? Phone);
```

## 3. Endpoints

### 3.1 `POST /api/availability/search`

Request body: `SearchAvailabilityRequest` (JSON).

Response `200 OK`:

```json
{
  "hotelId": "…",
  "hotelName": "Grand Plaza",
  "checkIn": "2026-09-01",
  "checkOut": "2026-09-05",
  "rooms": [
    {
      "roomId": "…",
      "name": "Deluxe Double Room",
      "ratePlans": [
        {
          "id": "…",
          "name": "Standard Rate",
          "totalPrice": { "amount": 480.00, "currencyCode": "UAH" },
          "cancellationPolicy": { "type": "freeCancellationUntil", "deadline": "2026-08-30T23:59:00Z" },
          "mealPlan": { "type": "breakfast", "description": "Сніданок включено" }
        },
        {
          "id": "…",
          "name": "Non-refundable Rate",
          "totalPrice": { "amount": 400.00, "currencyCode": "UAH" },
          "cancellationPolicy": { "type": "nonRefundable" },
          "mealPlan": null
        }
      ]
    }
  ]
}
```

Response `400 Bad Request` (ProblemDetails/ValidationProblemDetails) при некоректному запиті.
Response `404 Not Found`, якщо готель з таким `HotelId` відсутній.
Якщо готель існує, але жоден номер не вміщує запитану кількість гостей (див. п.5 — фільтр за `MaxAdults`/`MaxChildren`) — це не помилка, а `200 OK` з `"rooms": []`.

### 3.2 `POST /api/bookings`

Приймає `CreateBookingRequest`. Валідація — лише автоматичний model binding ASP.NET (обов'язкові поля через non-nullable типи, перевірка типів JSON); окремого семантичного валідатора (email-формат тощо) свідомо немає — дублювати validation-логіку для endpoint'у, який все одно завжди повертає стаб, не варто. Якщо тіло не парситься або відсутнє обов'язкове поле — ASP.NET поверне `400` ще до виклику делегата (покрито тестом, п.9).

Якщо body валідне з точки зору binding — ендпоінт **завжди** повертає `501`, незалежно від того, чи існують `HotelId`/`RoomId`/`RatePlanId` — бізнес-перевірки за визначенням стаба не виконуються.

Response: `501 Not Implemented` з `ProblemDetails`:

```json
{
  "type": "https://httpstatuses.com/501",
  "title": "Not Implemented",
  "status": 501,
  "detail": "Booking creation is not implemented yet."
}
```

### 3.3 `GET /health` (опційно)

Простий healthcheck (`Results.Ok("Healthy")` або `app.MapHealthChecks("/health")`) — 2 рядки коду, дешевий сигнал production-mindset, не критично для завдання.

## 4. Правила валідації

Для `SearchAvailabilityRequest`:

| Правило | Помилка |
|---|---|
| `CheckIn >= today` | заїзд не може бути в минулому |
| `CheckIn <= today.AddYears(1)` | бронювання максимум за рік наперед |
| `CheckOut > CheckIn` | виїзд має бути пізніше заїзду |
| `CheckOut <= CheckIn.AddMonths(1)` | максимальна тривалість — 1 місяць |
| `Rooms >= 1` | має бути хоча б один номер |
| `Adults >= 1` | має бути хоча б один дорослий |
| кожен елемент `ChildrenAges` в діапазоні `0..17` | некоректний вік дитини |
| `HotelId` не порожній | обов'язкове поле |

Реалізація: легкий власний валідатор (`SearchAvailabilityRequestValidator`, повертає `IReadOnlyList<ValidationError>` або `FluentValidation`, якщо хочемо показати знайомство з бібліотекою — обидва варіанти прийнятні, за замовчуванням піду без зовнішньої залежності, щоб рішення лишалось "невеликим"). Результат мапиться в `ValidationProblemDetails` через `Results.ValidationProblem(...)`.

Для `CreateBookingRequest` окремого валідатора немає — див. п.3.2 (лише automatic model binding, свідомо, щоб не дублювати логіку для endpoint'у-заглушки).

Час "поточної дати" беремо з ін'єкованого `TimeProvider` (`TimeProvider.System` за замовчуванням, DI-friendly, легко підмінити в тестах) замість статичного `DateTime.Now` — демонструє правильний DI і тестованість.

## 5. Сервіси та DI

```csharp
public interface IHotelCatalog
{
    Task<Hotel?> FindAsync(Guid hotelId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Room>> GetRoomsAsync(Guid hotelId, CancellationToken cancellationToken);
}

public interface IAvailabilityService
{
    Task<IReadOnlyList<AvailableRoom>> SearchAsync(
        SearchAvailabilityRequest request,
        CancellationToken cancellationToken);
}
```

- `InMemoryHotelCatalog` — статичний/seed-набір готелів і номерів (2-3 готелі, по 2-4 номери) для реалістичних відповідей.
- `MockAvailabilityService` — детерміновано генерує 1-2 тарифні плани на номер (наприклад, "Standard Rate" з можливістю безкоштовного скасування за 2 дні до заїзду і "Non-refundable Rate" дешевший на ~15%); ціна вважається як `basePricePerNight * nights * rooms` з невеликим коефіцієнтом за дорослих/дітей. Номери, чиї `MaxAdults`/`MaxChildren` не вміщують запитану кількість гостей (`Adults`/`ChildrenAges.Count` на один номер із `Rooms`), відфільтровуються — інакше поля місткості на `Room` існують лише для показу і пошук ігнорує вхідні параметри запиту.
- Реєстрація в `Program.cs`:
  ```csharp
  builder.Services.AddSingleton(TimeProvider.System);
  builder.Services.AddSingleton<IHotelCatalog, InMemoryHotelCatalog>();
  builder.Services.AddSingleton<IAvailabilityService, MockAvailabilityService>(); // стейтлес, Scoped нічого не додає
  ```

Усі методи сервісів — `async`, приймають `CancellationToken` і прокидають його до `HttpContext.RequestAborted` в endpoint-делегатах (навіть якщо всередині немає реального I/O — для тестового завдання головне показати правильний контракт і прокидання токена).

## 6. Обробка помилок

- Глобальний exception handler (`app.UseExceptionHandler(...)` або `IExceptionHandler` в .NET 8+/10) — мапить неочікувані винятки в `500` з `ProblemDetails`.
- `Results.ValidationProblem` для помилок валідації (400).
- `Results.NotFound` / `Results.Problem(statusCode: 404, ...)` для відсутнього готеля.
- `Results.Problem(statusCode: StatusCodes.Status501NotImplemented, detail: "...")` для endpoint'у бронювання.
- Увімкнути `builder.Services.AddProblemDetails()` для консистентного формату помилок за замовчуванням.

## 7. Nullable reference types та якість коду

- `<Nullable>enable</Nullable>` в `.csproj`.
- Явна різниця між обов'язковими (`string Name`) і опціональними (`string? Description`, `MealPlan? MealPlan`, `IReadOnlyList<int>? ChildrenAges`) полями в моделях.
- Уникати `null!`-хаків; там, де значення дійсно завжди присутнє — не робити nullable.

## 8. Swagger / OpenAPI

`Microsoft.AspNetCore.OpenApi` (вбудований у .NET 10) генерує лише OpenAPI JSON-документ — інтерактивний UI ним **не** постачається, потрібен окремий пакет. Беремо `Scalar.AspNetCore` (сучасний дефолт у шаблонах .NET 9+/10, легший за Swashbuckle):

```csharp
builder.Services.AddOpenApi();
// ...
app.MapOpenApi();
app.MapScalarApiReference(); // UI на /scalar/v1
```

Альтернатива, якщо потрібен саме класичний вигляд Swagger UI — `Swashbuckle.AspNetCore.SwaggerUI`, але для .NET 10 Scalar простіший і не вимагає окремого генератора документа.

## 9. Тести (xUnit)

- **Валідація**: усі граничні кейси з таблиці в п.4 (заїзд у минулому, >1 рік наперед, тривалість >1 місяць, `checkOut <= checkIn`, `rooms/adults < 1`, некоректний вік дитини) + happy path.
- **MockAvailabilityService**: перевірка, що для валідного запиту повертається непорожній список номерів, кожен номер має ≥1 тарифний план, безкоштовне скасування має дедлайн раніше заїзду, номери, які не вміщують запитану кількість гостей, відсутні у відповіді.
- **Endpoint-тести** через `WebApplicationFactory<Program>`: 200 на валідний запит (у т.ч. перевірка, що `CancellationPolicy` серіалізується з правильним `"type"`-дискримінатором — regression-тест саме на п.2.1), 400 на невалідний, 404 на неіснуючий готель, 200 з порожнім `rooms: []`, якщо жоден номер не вміщує гостей, 501 на `POST /api/bookings` з валідним тілом, 400 на `POST /api/bookings` з відсутнім обов'язковим полем (model binding).

## 10. Порядок виконання

1. `git init` + `.gitignore` (bin/obj/.vs) — далі комітити після кожного кроку, щоб історія показувала хід думки рецензенту.
2. Ініціалізація solution + Web API проєкту (.NET 10, minimal API, nullable enable), базовий `Program.cs`.
3. Спільні типи (`Money`, `CancellationPolicy` з `[JsonPolymorphic]`/`[JsonDerivedType]`, `MealPlan`) + domain-моделі (`Room`, `Hotel`, `RatePlan`, `AvailableRoom`).
4. Request/Response DTO + мапінг domain → DTO.
5. `IHotelCatalog` + `InMemoryHotelCatalog` з тестовими даними.
6. `IAvailabilityService` + `MockAvailabilityService` (генерація тарифів + фільтр за місткістю номера).
7. Валідатор `SearchAvailabilityRequest` + мапінг помилок у `ValidationProblemDetails`.
8. Endpoint `POST /api/availability/search` з повним ланцюжком: bind → validate → catalog lookup (404) → service (200/порожній список).
9. Моделі й endpoint `POST /api/bookings` → `501 Not Implemented`.
10. Глобальна обробка помилок (`ProblemDetails`, exception handler).
11. OpenAPI + Scalar UI.
12. Юніт- і endpoint-тести (включно з regression-тестом на JSON-серіалізацію `CancellationPolicy`).
13. README: як запустити, приклади запитів (curl/HTTP-файл), обґрунтування ключових рішень (чому record, чому DateOnly/TimeProvider, структура CancellationPolicy, чому POST а не GET для пошуку тощо).

## 11. Що варто явно згадати в README для рецензента

- Чому `record` для моделей (immutability, value equality, короткий синтаксис `with`-виразів).
- Чому `CancellationPolicy` як ієрархія record'ів, а не enum + nullable-поле.
- Як прокидається `CancellationToken` через увесь стек.
- Чому `TimeProvider` замість `DateTime.Now` (тестованість).
- Що саме імітується (mock-дані) і як легко підмінити `IAvailabilityService`/`IHotelCatalog` реальною реалізацією без зміни API-шару (демонстрація DI/чистої архітектури).
- Чому `POST`, а не `GET`, для `/api/availability/search` — масив `ChildrenAges` і кількість фільтрів погано лягають у query-string.
