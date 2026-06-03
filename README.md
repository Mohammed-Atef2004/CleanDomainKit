<div align="center">

<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/dotnetcore/dotnetcore-original.svg" width="80" height="80" alt=".NET"/>

# CleanDomainKit

> A pragmatic .NET toolkit for Domain-Driven Design and Clean Architecture — built from multiple rewrites of a real-world logistics system.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![NuGet](https://img.shields.io/badge/NuGet-coming_soon-gray)](https://nuget.org)
</div>


---

## Why CleanDomainKit?

After rebuilding the same logistics domain multiple times, I noticed something:

Business rules evolved constantly. Architectural patterns didn't.

Every version still needed Aggregate Roots, Value Objects, Domain Events, Business Rules, the Result Pattern, and Mediator Pipelines. At first I copied them between projects. Then I realized: if you're copying the same code repeatedly, you're probably missing an abstraction.

CleanDomainKit extracts those recurring patterns into a reusable toolkit — so you focus on domain logic, not infrastructure plumbing.

---

## Why Not Just Use EF Core Directly?

EF Core solves persistence. CleanDomainKit focuses on modeling business behavior.

Instead of fat services and business logic scattered across controllers, you get:

- **Aggregate Roots** with explicit consistency boundaries
- **Domain Events** dispatched after commit — not before
- **Business Rules** as first-class objects, not `if` statements in a handler
- **Result-based error handling** — no exceptions for business failures

---

## 📚 Table of Contents

- [Architecture](#-architecture)
- [Features](#-features)
- [Getting Started](#-getting-started)
- [Result Pattern](#-result-pattern)
- [Project Structure](#-recommended-project-structure)
- [Real World Usage](#-real-world-usage)
- [Roadmap](#-roadmap)
- [Built With](#-built-with)
- [Contributing](#-contributing)
- [License](#-license)
- [Acknowledgements](#-acknowledgements)

---

## 🏗 Architecture

![CleanDomainKit Architecture](docs/images/architecture.svg)

```
Request
    ↓
[LoggingBehavior]       ← logs every request in/out
    ↓
[ValidationBehavior]    ← runs FluentValidation, blocks handler if invalid
    ↓
Handler
    ↓
Repository.AddAsync()
    ↓
UnitOfWork.CompleteAsync()   ← DB commit happens here
    ↓
Domain Events dispatched     ← side-effects run after commit, state already persisted
```

> **Note on reliability:** Current dispatch happens in-process after commit. An Outbox Pattern for guaranteed delivery across failures is on the roadmap.

---

## ✨ Features

| | Feature | Description |
|---|---|---|
| 🏛️ | Clean Architecture | Enforced layer separation: Domain → Application → Infrastructure → Presentation |
| 🧩 | DDD Building Blocks | `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject` with structural equality |
| 📋 | Built-in Auditing | `IAuditable` — `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` on every entity |
| 🗑️ | Soft Delete | `ISoftDeletable` — `IsDeleted` + `DeletedAtUtc`, no hard deletes |
| 🔄 | Unit of Work | `IUnitOfWork` with `CompleteAsync` / `RollbackAsync` |
| 📦 | Generic Repository | `IGenericRepository<T>` with deferred `IQueryable` and `CountAsync` |
| 🛡️ | Business Rules | `CheckRule(IBusinessRule)` keeps invariants inside the domain |
| 🎯 | Domain Events | `DomainEvent` record with auto `Id` + `OccurredOnUtc` |
| ✅ | Result Pattern | `Result` / `ValidationResult` — zero exceptions for business failures |
| 📡 | Mediator Pipelines | `LoggingBehavior` + `ValidationBehavior` on every MediatR request |
| 🎮 | Base API Controller | Maps `Result` → structured `ProblemDetails` automatically |

---

## 🚀 Getting Started

### 1. Define an Entity

Every entity gets identity-based equality, auditing, and soft-delete for free:

```csharp
public class Product : Entity<Guid>
{
    public string  Name  { get; private set; }
    public decimal Price { get; private set; }

    private Product() { } // required by EF Core

    public static Product Create(Guid id, string name, decimal price)
        => new() { Id = id, Name = name, Price = price };
}
```

`Entity<TId>` automatically provides:

| Property | Type | Set by |
|---|---|---|
| `Id` | `TId` | Constructor |
| `CreatedAt` | `DateTime` | `SetCreated(user)` |
| `CreatedBy` | `string?` | `SetCreated(user)` |
| `UpdatedAt` | `DateTime?` | `SetUpdated(user)` |
| `UpdatedBy` | `string?` | `SetUpdated(user)` |
| `IsDeleted` | `bool` | `Delete()` |
| `DeletedAtUtc` | `DateTime?` | `Delete()` |

---

### 2. Define an Aggregate Root

```csharp
public class Order : AggregateRoot<Guid>
{
    public Guid        CustomerId { get; private set; }
    public decimal     Total      { get; private set; }
    public OrderStatus Status     { get; private set; }

    private Order() { }

    public static Result Create(Guid customerId, decimal total)
    {
        var order = new Order
        {
            Id         = Guid.NewGuid(),
            CustomerId = customerId,
            Total      = total,
            Status     = OrderStatus.Pending
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return Result.Success();
    }

    public Result Confirm()
    {
        var check = CheckRule(new OrderMustBePendingRule(Status));
        if (check.IsFailure) return check;

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
        return Result.Success();
    }
}
```

---

### 3. Enforce Business Rules

Business rules live inside the domain — not in handlers or controllers:

```csharp
public class OrderMustBePendingRule : IBusinessRule
{
    private readonly OrderStatus _status;
    public OrderMustBePendingRule(OrderStatus status) => _status = status;

    public bool IsBroken() => _status != OrderStatus.Pending;

    public Error Error => new("Order.NotPending", "Only pending orders can be confirmed.");
}
```

`AggregateRoot<TId>` exposes `CheckRule` — all invariant logic stays where it belongs:

```csharp
protected Result CheckRule(IBusinessRule rule)
    => rule.IsBroken() ? Result.Failure(rule.Error) : Result.Success();
```

---

### 4. Publish Domain Events

```csharp
// 1. Inherit from DomainEvent — gets Id and OccurredOnUtc for free
public record OrderCreatedEvent(Guid OrderId, Guid CustomerId) : DomainEvent;

// 2. Raise it inside the aggregate
order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));

// 3. Handle it anywhere in the Application layer
public class SendConfirmationEmailHandler : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent e, CancellationToken ct)
    {
        // send email, update read model, trigger integration event …
    }
}
```

---

### 5. Persist with Repository + Unit of Work

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result>
{
    private readonly IGenericRepository<Order> _orders;
    private readonly IUnitOfWork               _uow;

    public CreateOrderHandler(IGenericRepository<Order> orders, IUnitOfWork uow)
        => (_orders, _uow) = (orders, uow);

    public async Task<Result> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var result = Order.Create(cmd.CustomerId, cmd.Total);
        if (result.IsFailure) return result;

        await _orders.AddAsync(result.Value);
        await _uow.CompleteAsync(ct);   // commit + dispatch domain events

        return Result.Success();
    }
}
```

---

### 6. Define a Value Object

```csharp
public class Money : ValueObject
{
    public decimal Amount   { get; }
    public string  Currency { get; }

    public Money(decimal amount, string currency)
        => (Amount, Currency) = (amount, currency);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

var a = new Money(100, "USD");
var b = new Money(100, "USD");
Console.WriteLine(a == b); // true
```

---

### 7. Mediator Pipeline Behaviors

Every MediatR request flows through registered behaviors in order:

```
Request → [LoggingBehavior] → [ValidationBehavior] → Handler → Response
```

**LoggingBehavior** — structured logging before and after every request automatically.

**ValidationBehavior** — runs all `IValidator<TRequest>` before the handler. Fails early if invalid.

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Total).GreaterThan(0).WithMessage("Total must be positive.");
    }
}
```

---

### 8. Expose via API Controller

```csharp
[Route("api/orders")]
public class OrdersController : ApiController
{
    public OrdersController(ISender sender) : base(sender) { }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        var result = await _sender.Send(new CreateOrderCommand(req.CustomerId, req.Total));
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }

    [HttpPut("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _sender.Send(new ConfirmOrderCommand(id));
        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
```

`HandleFailure` mapping:

| Result type | HTTP Status | Body |
|---|---|---|
| `IValidationResult` | 400 Bad Request | `ProblemDetails` + `errors[]` |
| Any `Result.Failure` | 400 Bad Request | `ProblemDetails` with `code` + `detail` |
| `Result.Success` (mistake) | throws `InvalidOperationException` | — |

---

## 🔷 Result Pattern

```csharp
// Producing
Result success    = Result.Success();
Result failure    = Result.Failure(new Error("Order.NotFound", "Order does not exist."));
Result validation = ValidationResult.WithErrors(new[]
{
    new Error("Name.Empty",    "Name is required."),
    new Error("Price.Invalid", "Price must be positive.")
});

// Consuming
if (result.IsFailure) return HandleFailure(result);   // controllers
if (result.IsFailure) return result;                  // handlers
```

`Error` is a simple record — no inheritance, no ceremony:

```csharp
public record Error(string Code, string Message)
{
    public static readonly Error None = new("", "");
}
```

---

## 📂 Recommended Project Structure

```
YourProject/
└── src/
    ├── Domain/
    │   ├── SharedKernel/       # AggregateRoot, Entity, ValueObject, Result, DomainEvent
    │   ├── Aggregates/         # Your aggregate roots
    │   ├── ValueObjects/       # Money, Address, Email …
    │   └── Interfaces/
    │       └── Repositories/   # IGenericRepository<T>, IUnitOfWork
    ├── Application/
    │   ├── Common/
    │   │   └── Behaviors/      # LoggingBehavior, ValidationBehavior
    │   └── Features/           # Commands, Queries, Handlers, Validators
    ├── Infrastructure/         # EF Core DbContext, Repository implementations
    └── WebApi/
        └── Controllers/        # ApiController base + feature controllers
```

---

## 🏭 Real World Usage

CleanDomainKit is the architectural foundation of [Smart Logistics & Fleet Management System (SLFMS)](https://github.com/your-link-here) — rebuilt multiple times as domain understanding grew.

The system models: Fleet Management · Drivers · Shipments · Warehouses · Inventory · Payments · Insurance Claims

Every pattern in CleanDomainKit survived multiple rewrites of a real system. That's the actual proof it works.

---

## 🗺 Roadmap

### ✅ Completed

- [x] `AggregateRoot<TId>` with `AddDomainEvent` + `CheckRule`
- [x] `Entity<TId>` with `IAuditable` + `ISoftDeletable`
- [x] `ValueObject` with structural equality
- [x] `Result` / `ValidationResult` / `Error`
- [x] `IGenericRepository<T>` with deferred `IQueryable`
- [x] `IUnitOfWork` with `CompleteAsync` / `RollbackAsync`
- [x] `LoggingBehavior` + `ValidationBehavior` MediatR pipelines
- [x] `ApiController` base with `ProblemDetails` mapping
- [x] `DomainEvent` record (`MediatR INotification`)

### 🔜 Upcoming

- [ ] `Result<T>` generic version
- [ ] `TransactionBehavior` pipeline
- [ ] Outbox pattern for guaranteed domain event delivery
- [ ] OpenTelemetry tracing integration
- [ ] NuGet package

---

## 🔧 Built With

- [.NET 9](https://dotnet.microsoft.com)
- [EF Core](https://learn.microsoft.com/en-us/ef/core/)
- [MediatR](https://github.com/jbogard/MediatR)
- [FluentValidation](https://fluentvalidation.net/)
- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)

---

## 🤝 Contributing

Contributions are warmly welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first.

```bash
git checkout -b feature/my-feature
git commit -m "feat: add my feature"
git push origin feature/my-feature
# then open a Pull Request
```

---

## 📄 License

Licensed under the [MIT License](LICENSE).

---

## 🙏 Acknowledgements

Inspired by the work of:

- [MediatR](https://github.com/jbogard/MediatR) — Jimmy Bogard
- [FluentValidation](https://fluentvalidation.net/) — Jeremy Skinner
- [Milan Jovanović](https://www.milanjovanovic.tech/) — Clean Architecture & DDD in .NET
- [Vladimir Khorikov](https://enterprisecraftsmanship.com/) — Enterprise Craftsmanship

Made with ❤️ for the .NET community

---

⭐ If CleanDomainKit saves you time, please star the repo!
