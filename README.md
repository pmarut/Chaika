# Hotel Availability API

Тестове завдання: REST API для пошуку доступності номерів готелю (`POST /api/availability/search`) та заглушка створення бронювання (`POST /api/bookings`).

Стек: .NET 10, ASP.NET Core Minimal API, xUnit.

## Запуск

```bash
dotnet run --project src/HotelAvailability.Api
```

За замовчуванням API піднімається на адресах з `src/HotelAvailability.Api/Properties/launchSettings.json` (http/https). Кореневий шлях `/` редіректить на інтерактивну документацію Scalar (`/scalar/v1`), сирий OpenAPI-документ доступний на `/openapi/v1.json`.

## Тести

```bash
dotnet test
```

20 тестів: валідація (`SearchAvailabilityRequestValidatorTests`), доменна логіка mock-сервісу (`MockAvailabilityServiceTests`), інтеграційні тести ендпоінтів через `WebApplicationFactory<Program>` (`AvailabilityEndpointsTests`).

## Приклади запитів

У `InMemoryHotelCatalog` зашито 3 готелі з фіксованими `Guid`, зокрема `Grand Plaza` — `11111111-1111-1111-1111-111111111111`.

Пошук доступності:

```bash
curl -X POST http://localhost:5299/api/availability/search \
  -H "Content-Type: application/json" \
  -d '{
    "hotelId": "11111111-1111-1111-1111-111111111111",
    "checkIn": "2026-09-01",
    "checkOut": "2026-09-05",
    "rooms": 1,
    "adults": 2
  }'
```

Створення бронювання (завжди `501 Not Implemented`):

```bash
curl -X POST http://localhost:5299/api/bookings \
  -H "Content-Type: application/json" \
  -d '{
    "hotelId": "11111111-1111-1111-1111-111111111111",
    "roomId": "11111111-0001-0000-0000-000000000000",
    "ratePlanId": "00000000-0000-0000-0000-000000000000",
    "checkIn": "2026-09-01",
    "checkOut": "2026-09-05",
    "rooms": 1,
    "adults": 2,
    "guest": { "firstName": "Jane", "lastName": "Doe", "email": "jane@example.com" }
  }'
```

## Ключові рішення

**Чому `record`.** Моделі домену та DTO — незмінні дані без поведінки: `record` дає value-equality "з коробки", короткий синтаксис і `with`-вирази для похідних копій (корисно, наприклад, при мапінгу domain → DTO).

**Чому `CancellationPolicy` — ієрархія record'ів, а не enum + nullable-поле.** `NonRefundable` не потребує додаткових даних, `FreeCancellationUntil` вимагає `Deadline` — enum + nullable `Deadline` дозволив би побудувати некоректну комбінацію (`NonRefundable` з непорожнім дедлайном) і компілятор про це не попередить. Дискримінована унія через `record`-ієрархію унеможливлює такий стан і дає exhaustive pattern-matching (`switch` без `default` — компілятор підкаже, якщо забути кейс). Плата за це — обов'язкові `[JsonPolymorphic]`/`[JsonDerivedType]` на базовому типі: без них `System.Text.Json` серіалізує похідні record'и через оголошений базовий тип і мовчки губить властивості, яких немає в базовому (`Deadline`), замість очікуваного `{ "type": "...", ... }`. Регресію на це покриває `AvailabilityEndpointsTests.Search_WithValidRequest_Returns200WithRoomsAndCancellationPolicyDiscriminator`.

**`CancellationToken`.** Прокидається від `HttpContext.RequestAborted` у делегатах ендпоінтів (`Endpoints/*.cs`) через увесь стек `IHotelCatalog`/`IAvailabilityService` — навіть попри те, що поточні реалізації синхронні (in-memory), це фіксує правильний контракт для майбутньої заміни на реальний I/O (БД, зовнішній сервіс).

**Чому `TimeProvider`, а не `DateTime.Now`/`DateTime.UtcNow`.** `SearchAvailabilityRequestValidator` отримує `TimeProvider` через DI (`TimeProvider.System` у `Program.cs`), завдяки чому правила "заїзд не в минулому"/"не більше ніж за рік" у тестах перевіряються з фіксованою датою (`FakeTimeProvider`) — без цього тести на граничні дати були б крихкими відносно реального часу виконання.

**Що саме mock, і як підмінити.** `InMemoryHotelCatalog` (`IHotelCatalog`) — фіксований in-memory каталог із 3 готелями; `MockAvailabilityService` (`IAvailabilityService`) — детерміновано генерує 2 тарифні плани на номер, що вміщує запитаних гостей (`Standard Rate` з безкоштовним скасуванням за 2 дні до заїзду, `Non-refundable Rate` дешевша на ~15%, без харчування). Обидва зареєстровані в DI за інтерфейсом (`Program.cs`), тож заміна на реальну БД/зовнішній provider — це підміна реєстрації однієї реалізації, без змін у ендпоінтах чи моделях.

**Чому `POST`, а не `GET`, для `/api/availability/search`.** Запит містить масив змінної довжини (`childrenAges`) і кілька обов'язкових полів — коректно і зручно передати як query-string складно й нестандартно; тіло запиту тут природніше за REST-семантикою "пошук за критеріями", навіть попри те що операція логічно read-only.

**JSON-валідність замість ручного валідатора для бронювання.** `POST /api/bookings` — стаб, що завжди повертає `501`, тож окремий семантичний валідатор (формат email тощо) дублював би логіку заради ендпоінту, який однаково не виконує бізнес-перевірок. Замість цього `JsonSerializerOptions.RespectRequiredConstructorParameters` + `RespectNullableAnnotations` (`Program.cs`) змушують `System.Text.Json` відхиляти запит, у якому відсутнє обов'язкове поле (`Guest`, `RatePlanId` тощо) або воно `null` — ще до виклику делегата, `400` замість `501`. Поля, які справді необов'язкові (`ChildrenAges`, `GuestInfo.Phone`), мають явний дефолт `= null` у `record`, інакше ці два прапорці вимагали б їх у кожному запиті.

**Обробка помилок.** Глобальний `IExceptionHandler` (`Infrastructure/ApiExceptionHandler.cs`) мапить будь-який необроблений виняток у `ProblemDetails`. Окремо він розпізнає `BadHttpRequestException` (не вдалось розпарсити/зв'язати тіло запиту) і повертає `400` замість `500` — без цього необхідне через нюанс ASP.NET Core: автоматична конвертація помилки JSON-біндингу в `400` вмикається лише поза `Development`-середовищем, а `WebApplicationFactory` в тестах піднімає застосунок саме в `Development`, тож без явної обробки в хендлері помилка "протікала" б як `500`.

## Структура проєкту

```
src/
  HotelAvailability.Api/          # Minimal API проєкт
    Endpoints/                    # extension-методи над IEndpointRouteBuilder
    Models/
      Common/                     # Money, CancellationPolicy, MealPlan — спільні для domain і DTO
      Domain/                     # Hotel, Room, RatePlan, AvailableRoom
      Requests/                   # SearchAvailabilityRequest, CreateBookingRequest, GuestInfo
      Responses/                  # SearchAvailabilityResponse, AvailableRoomDto, RatePlanDto (+ мапінг з domain)
    Services/                     # IHotelCatalog/IAvailabilityService + mock-реалізації
    Validation/                   # SearchAvailabilityRequestValidator
    Infrastructure/                # ApiExceptionHandler
  HotelAvailability.Api.Tests/    # xUnit: Validation / Services / Endpoints
```
