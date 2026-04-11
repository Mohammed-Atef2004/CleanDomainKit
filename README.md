<div align="center">

<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/dotnetcore/dotnetcore-original.svg" width="80" height="80" alt=".NET"/>

# CleanDomainKit

**A professional .NET toolkit combining Domain-Driven Design with Clean Architecture**

[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)

_Stop writing the same boilerplate. Start building real features._

</div>

---

## 📚 Table of Contents

- [📖 Overview](#-overview)
- [✨ Features](#-features)
- [🏗️ Architecture](#️-architecture)
- [🚀 Getting Started](#-getting-started)
  - [1. Define an Entity](#1-define-an-entity)
  - [2. Define an Aggregate Root](#2-define-an-aggregate-root)
  - [3. Enforce Business Rules](#3-enforce-business-rules)
  - [4. Publish Domain Events](#4-publish-domain-events)
  - [5. Persist with Repository + Unit of Work](#5-persist-with-repository--unit-of-work)
  - [6. Define a Value Object](#6-define-a-value-object)
  - [7. Mediator Pipeline Behaviors](#7-mediator-pipeline-behaviors)
  - [8. Expose via API Controller](#8-expose-via-api-controller)
- [🔷 Result Pattern](#-result-pattern)
- [📂 Recommended Project Structure](#-recommended-project-structure)
- [🗺️ Roadmap](#️-roadmap)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)
- [🙏 Acknowledgements](#-acknowledgements)

---

## 📖 Overview

**CleanDomainKit** is a production-ready .NET toolkit that gives your team a clean, opinionated foundation for enterprise applications. It wires together battle-tested patterns — DDD aggregates, Clean Architecture layers, Mediator pipelines, Unit of Work, and Result-based error handling — into a single cohesive library.

> 💡 Built-in auditing, soft deletes, domain event publishing, validation pipelines, and structured error handling — all working together out of the box.

---

## ✨ Features

| Feature | Description |
|---|---|
| 🏛️ **Clean Architecture** | Enforced layer separation: Domain → Application → Infrastructure → Presentation |
| 🧩 **DDD Building Blocks** | `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject` with identity-based equality |
| 📋 **Built-in Auditing** | `IAuditable` — `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` on every entity |
| 🗑️ **Soft Delete** | `ISoftDeletable` — `IsDeleted` + `DeletedAtUtc`, no data is ever hard-deleted |
| 🔄 **Unit of Work** | `IUnitOfWork` with `CompleteAsync` / `RollbackAsync` for transactional consistency |
| 📦 **Generic Repository** | `IGenericRepository<T>` with `IQueryable` deferred execution and `CountAsync` |
| 🛡️ **Business Rules** | `CheckRule(IBusinessRule)` on every aggregate root — keeps invariants inside the domain |
| 🎯 **Domain Events** | `DomainEvent` record (MediatR `INotification`) with auto `Id` + `OccurredOnUtc` |
| ✅ **Result Pattern** | `Result` / `ValidationResult` — zero exceptions for business rule failures |
| 📡 **Mediator Pipelines** | `LoggingBehavior` + `ValidationBehavior` wired into every MediatR request |
| 🎮 **Base API Controller** | `ApiController` maps `Result` → structured `ProblemDetails` automatically |

---

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────┐
│               Presentation  (WebApi)             │
│         ApiController  ·  Middleware             │
├──────────────────────────────────────────────────┤
│               Application Layer                  │
│    Commands · Queries · Handlers · Behaviors     │
│       LoggingBehavior · ValidationBehavior       │
├──────────────────────────────────────────────────┤
│              Infrastructure Layer                │
│      EF Core · Repositories · UnitOfWork         │
├──────────────────────────────────────────────────┤
│                  Domain Layer                    │
│  AggregateRoot · Entity · ValueObject · Result   │
│        DomainEvent · IBusinessRule               │
└──────────────────────────────────────────────────┘
          ↑  Dependencies only flow inward  ↑
```

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

        // Raised inside the aggregate, dispatched after CompleteAsync()
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return Result.Success();
    }

    public Result Confirm()
    {
        // Business rule enforced inside the aggregate boundary
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

Business rules live inside the domain layer, not in handlers or controllers:

```csharp
public class OrderMustBePendingRule : IBusinessRule
{
    private readonly OrderStatus _status;

    public OrderMustBePendingRule(OrderStatus status) => _status = status;

    public bool IsBroken() => _status != OrderStatus.Pending;

    public Error Error => new(
        "Order.NotPending",
        "Only pending orders can be confirmed.");
}
```

`AggregateRoot<TId>` exposes `CheckRule` — keeping all invariant logic where it belongs:

```csharp
protected Result CheckRule(IBusinessRule rule)
    => rule.IsBroken()
        ? Result.Failure(rule.Error)
        : Result.Success();
```

---

### 4. Publish Domain Events

```csharp
// 1. Inherit from DomainEvent — gets Id and OccurredOnUtc for free
public record OrderCreatedEvent(Guid OrderId, Guid CustomerId) : DomainEvent;

// 2. Raise it inside the aggregate
order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));

// 3. Handle it anywhere in the Application layer
public class SendConfirmationEmailHandler
    : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent e, CancellationToken ct)
    {
        // send email, update read model, trigger integration event …
    }
}
```

> Domain events are dispatched **after** `IUnitOfWork.CompleteAsync()` commits — guaranteeing the aggregate state is already persisted before side-effects run.

---

### 5. Persist with Repository + Unit of Work

```csharp
// IQueryable<T> EntityQuery gives you deferred, composable queries
// without leaking EF Core into the Application layer
var pendingOrders = await _orders.EntityQuery
    .Where(o => o.Status == OrderStatus.Pending && !o.IsDeleted)
    .OrderByDescending(o => o.CreatedAt)
    .ToListAsync(ct);
```

**Usage inside a command handler:**

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

        await _orders.AddAsync(result.Value);  // stage
        await _uow.CompleteAsync(ct);          // commit + dispatch domain events

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

    // Equality is value-based, not reference-based
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// Usage
var a = new Money(100, "USD");
var b = new Money(100, "USD");
Console.WriteLine(a == b); // true
```

---

### 7. Mediator Pipeline Behaviors

Every MediatR request flows through the registered behaviors in order:

```
Request → [LoggingBehavior] → [ValidationBehavior] → Handler → Response
```

**LoggingBehavior** — structured logging before and after every request:

```csharp
// Logged automatically for every request — no manual logging needed
// "Handling CreateOrderCommand with data: { CustomerId: ..., Total: ... }"
// "Handled CreateOrderCommand"
```

**ValidationBehavior** — runs all registered `IValidator<TRequest>` before the handler. If any validator fails, a `ValidationException` is thrown — the handler is never reached.

```csharp
// Register a validator — picked up automatically via DI
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

`ApiController` maps every `Result` to a properly structured `ProblemDetails` response — no manual status-code logic in your controllers:

```csharp
[Route("api/orders")]
public class OrdersController : ApiController
{
    public OrdersController(ISender sender) : base(sender) { }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        var result = await _sender.Send(
            new CreateOrderCommand(req.CustomerId, req.Total));

        return result.IsSuccess
            ? Ok()
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _sender.Send(new ConfirmOrderCommand(id));

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }
}
```

**`HandleFailure` response mapping:**

| Result type | HTTP Status | Body |
|---|---|---|
| `IValidationResult` | `400 Bad Request` | `ProblemDetails` + `errors[]` |
| Any `Result.Failure` | `400 Bad Request` | `ProblemDetails` with `code` + `detail` |
| `Result.Success` (mistake) | throws `InvalidOperationException` | — |

---

## 🔷 Result Pattern

CleanDomainKit uses `Result` and `ValidationResult` — no exceptions for business rule failures.

```csharp
// --- Producing results ---
Result success    = Result.Success();
Result failure    = Result.Failure(new Error("Order.NotFound", "Order does not exist."));

// Validation failure carries multiple errors
Result validation = ValidationResult.WithErrors(new[]
{
    new Error("Name.Empty",    "Name is required."),
    new Error("Price.Invalid", "Price must be positive.")
});

// --- Consuming results ---
if (result.IsFailure)
    return HandleFailure(result);   // in controllers

if (result.IsFailure)
    return result;                  // propagate in handlers
```

**`Error` is a simple record — no inheritance, no ceremony:**

```csharp
public record Error(string Code, string Message)
{
    public static readonly Error None = new("", "");
}
```

---

## 📂 Recommended Project Structure

```
CleanDomainKit/
└── src/
    ├── Domain/
    │   ├── SharedKernel/       # AggregateRoot, Entity, ValueObject
    │   │                       # Result, ValidationResult, Error, DomainEvent
    │   ├── Aggregates/         # Your domain aggregate roots
    │   ├── ValueObjects/       # Money, Address, Email …
    │   └── Interfaces/
    │       └── Repositories/   # IGenericRepository<T>, IUnitOfWork
    ├── Application/
    │   ├── Common/
    │   │   └── Behaviors/      # LoggingBehavior, ValidationBehavior
    │   └── Features/           # Commands, Queries, Handlers, Validators (per feature)
    ├── Infrastructure/         # EF Core DbContext, Repository implementations
    └── WebApi/
        └── Controllers/        # ApiController base + feature controllers
```

---

## 🗺️ Roadmap

- [x] `AggregateRoot<TId>` with `AddDomainEvent` + `CheckRule`
- [x] `Entity<TId>` with `IAuditable` + `ISoftDeletable`
- [x] `ValueObject` with structural equality
- [x] `Result` / `ValidationResult` / `Error`
- [x] `IGenericRepository<T>` with deferred `IQueryable`
- [x] `IUnitOfWork` with `CompleteAsync` / `RollbackAsync`
- [x] `LoggingBehavior` + `ValidationBehavior` MediatR pipelines
- [x] `ApiController` base with `ProblemDetails` mapping
- [x] `DomainEvent` record (MediatR `INotification`)
- [x] `Result<T>` generic version
- [ ] `TransactionBehavior` pipeline
- [ ] Outbox pattern for reliable domain event dispatch
- [ ] OpenTelemetry tracing integration
- [ ] NuGet package

---

## 🤝 Contributing

Contributions are warmly welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting a pull request.

```bash
git checkout -b feature/my-feature
git commit -m "feat: add my feature"
git push origin feature/my-feature
# then open a Pull Request
```

---

## 📄 License

Licensed under the **MIT License** — see [LICENSE](LICENSE) for details.

---

## 🙏 Acknowledgements

CleanDomainKit is inspired by the work of:

- [MediatR](https://github.com/jbogard/MediatR) — Jimmy Bogard
- [FluentValidation](https://github.com/FluentValidation/FluentValidation) — Jeremy Skinner
- [Milan Jovanović](https://www.milanjovanovic.tech) — Clean Architecture & DDD in .NET
- [Vladimir Khorikov](https://enterprisecraftsmanship.com) — Enterprise Craftsmanship

---

<div align="center">

Made with ❤️ for the .NET community

⭐ **If CleanDomainKit saves you time, please consider giving it a star!** ⭐

</div>
