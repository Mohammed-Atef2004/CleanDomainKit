using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using Domain.Shipments;
using Domain.Shipments.Enums;
using Domain.Shipments.Events;
using Domain.Shipments.Errors;
using Domain.Shipments.ValueObjects;

namespace UnitTests.Domain.Shipments;

public class ShipmentTests
{

    private DeliveryAddress GetValidAddress() =>
        DeliveryAddress.Create(
            street: "123 Main St",
            city: "Cairo",
            state: "Cairo Gov",
            zipCode: "11511",
            country: "Egypt"
        );

    private Weight GetValidWeight() =>
        Weight.InKilograms(5m); // 5 KG using the factory method

    private Dimensions GetValidDimensions() =>
        Dimensions.InCentimeters(10m, 10m, 10m); // 10x10x10 CM using the factory method

    // ─── 1. Creation Tests ────────────────────────────────────────────
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess_AndRaiseShipmentCreatedEvent()
    {
        // Arrange
        var senderId = "SND-123";
        var address = GetValidAddress();
        var trackingNumber = "TRK-001";

        // Act
        var result = Shipment.Create(senderId, address, trackingNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var shipment = result.Value;

        shipment.SenderId.Should().Be(senderId);
        shipment.DestinationAddress.Should().Be(address);
        shipment.Tracking.Status.Should().Be(ShipmentStatus.Created);
        shipment.Version.Should().Be(0);

        // Verify Domain Event
        var domainEvent = shipment.DomainEvents.OfType<ShipmentCreatedEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TrackingNumber.Should().Be(trackingNumber);
    }

    [Fact]
    public void Create_WithEmptySenderId_ShouldReturnFailure()
    {
        // Act
        var result = Shipment.Create("", GetValidAddress(), "TRK-001");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ShipmentErrors.EmptySenderId);
    }

    // ─── 2. Package Management Tests ──────────────────────────────────
    [Fact]
    public void AddPackage_WhenShipmentIsEditable_ShouldAddPackage_AndIncreaseAggregates()
    {
        // Arrange
        var shipment = Shipment.Create("SND-123", GetValidAddress(), "TRK-001").Value;

        // Act
        var result = shipment.AddPackage(
            description: "Laptop",
            weight: GetValidWeight(),
            dimensions: GetValidDimensions(),
            declaredValue: 1000m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        shipment.Packages.Should().HaveCount(1);
        shipment.TotalPackages.Should().Be(1);
        shipment.TotalWeightKg.Should().Be(5m);
        shipment.TotalDeclaredValue.Should().Be(1000m);
        shipment.Version.Should().Be(1);

        // Verify Event
        shipment.DomainEvents.OfType<PackageAddedToShipmentEvent>().Should().ContainSingle();
    }

    [Fact]
    public void AddPackage_WhenShipmentIsDelivered_ShouldReturnFailure()
    {
        // Arrange
        var shipment = Shipment.Create("SND-123", GetValidAddress(), "TRK-001").Value;
        shipment.AddPackage("Item", GetValidWeight(), GetValidDimensions());
        shipment.AssignCarrier("FedEx");
        shipment.Dispatch();
        shipment.MarkDelivered(DateTime.UtcNow);

        // Act
        var result = shipment.AddPackage("New Item", GetValidWeight(), GetValidDimensions());

        // Assert
        result.IsFailure.Should().BeTrue();
        // Since it returns the specific Error from the Rule, we assert it failed
    }

    // ─── 3. Dispatch & Carrier Tests ──────────────────────────────────
    [Fact]
    public void Dispatch_WithoutPackages_ShouldReturnFailure()
    {
        // Arrange
        var shipment = Shipment.Create("SND-123", GetValidAddress(), "TRK-001").Value;
        shipment.AssignCarrier("FedEx");

        // Act
        var result = shipment.Dispatch();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Dispatch_WithPackagesAndCarrier_ShouldUpdateStatusToInTransit()
    {
        // Arrange
        var shipment = Shipment.Create("SND-123", GetValidAddress(), "TRK-001").Value;
        shipment.AddPackage("Laptop", GetValidWeight(), GetValidDimensions());
        shipment.AssignCarrier("Aramex");

        // Act
        var result = shipment.Dispatch();

        // Assert
        result.IsSuccess.Should().BeTrue();
        shipment.Tracking.Status.Should().Be(ShipmentStatus.InTransit);
        shipment.DomainEvents.OfType<ShipmentDispatchedEvent>().Should().ContainSingle();
    }

    // ─── 4. Delivery & Route Tests ────────────────────────────────────
    [Fact]
    public void AddRoutePoint_ShouldUpdateStatusBasedOnType_AndAddLocation()
    {
        // Arrange
        var shipment = Shipment.Create("SND-123", GetValidAddress(), "TRK-001").Value;

        // Act
        var result = shipment.AddRoutePoint("Cairo Hub", "Processing", DateTime.UtcNow, RoutePointType.OutForDelivery);

        // Assert
        result.IsSuccess.Should().BeTrue();
        shipment.Route.Should().HaveCount(1);
        shipment.CurrentLocation!.Location.Should().Be("Cairo Hub");
        shipment.Tracking.Status.Should().Be(ShipmentStatus.OutForDelivery);

        shipment.DomainEvents.OfType<RoutePointAddedEvent>().Should().ContainSingle();
        shipment.DomainEvents.OfType<ShipmentOutForDeliveryEvent>().Should().ContainSingle();
    }

    // ─── 5. Cancellation Tests ────────────────────────────────────────
    [Fact]
    public void Cancel_WhenValidState_ShouldUpdateStatus_AndRecordCancellation()
    {
        // Arrange
        var shipment = Shipment.Create("SND-123", GetValidAddress(), "TRK-001").Value;
        var reason = "Customer requested";

        // Act
        var result = shipment.Cancel(reason, "Admin");

        // Assert
        result.IsSuccess.Should().BeTrue();
        shipment.Status.Should().Be(ShipmentStatus.Cancelled);
        shipment.CancellationReason.Should().Be(reason);
        shipment.CancelledAt.Should().NotBeNull();
        shipment.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Cancel_WhenAlreadyDelivered_ShouldReturnFailure()
    {
        // Arrange
        var shipment = Shipment.Create("SND-123", GetValidAddress(), "TRK-001").Value;
        shipment.AddPackage("Item", GetValidWeight(), GetValidDimensions());
        shipment.AssignCarrier("FedEx");
        shipment.Dispatch();
        shipment.MarkDelivered(DateTime.UtcNow);

        // Act
        var result = shipment.Cancel("No longer needed", "Admin");

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}