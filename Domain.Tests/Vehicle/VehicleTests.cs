
using Domain.Vehicles.Enums;
using Domain.Vehicles.Errors;
using Domain.Vehicles.Events;
using Domain.Vehicles.ValueObjects;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Tests.Vehicle
{
    public class VehicleTests
    {
        private readonly VehiclePlateNumber _plate =
            VehiclePlateNumber.Create("ABC-123").Value;

        private readonly VehicleSpecification _spec =
            VehicleSpecification.Create("Toyota", 2024, "Corolla").Value;

        [Fact]
        public void Register_Should_Create_Vehicle_When_Plate_Is_Unique()
        {
            // Arrange
            var checker = new FakeVehicleUniquenessChecker(true);

            // Act
            var result = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(VehicleStatus.Available);
            result.Value.DomainEvents.Should().ContainSingle(e =>
                e is VehicleRegisteredEvent);
        }

        [Fact]
        public void Register_Should_Fail_When_Plate_Is_Not_Unique()
        {
            var checker = new FakeVehicleUniquenessChecker(false);

            var result = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(VehicleErrors.PlateAlreadyExists);
        }
        [Fact]
        public void ScheduleMaintenance_Should_Change_Status_And_Add_Event()
        {
            var checker = new FakeVehicleUniquenessChecker(true);
            var vehicle = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker).Value;

            var description = MaintenanceDescription.Create("Oil Change").Value;
            var date = DateTime.UtcNow.AddDays(10);

            var result = vehicle.ScheduleMaintenance(date, description);

            result.IsSuccess.Should().BeTrue();
            vehicle.Status.Should().Be(VehicleStatus.InMaintenance);
            vehicle.MaintenanceSchedules.Should().HaveCount(1);
            vehicle.DomainEvents.Should().Contain(e => e is MaintenanceScheduledEvent);
        }
        //[Fact]
        //public void RecordFuelConsumption_Should_Fail_When_Negative()
        //{
        //    var checker = new FakeVehicleUniquenessChecker(true);
        //    var vehicle = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker).Value;

        //    var fuel = FuelConsumption.Create(-5, 1000).Value; 

        //    var result = vehicle.RecordFuelConsumption(fuel);

        //    result.IsFailure.Should().BeTrue();
        //}

        [Fact]
        public void RecordFuelConsumption_Should_Succeed()
        {
            var checker = new FakeVehicleUniquenessChecker(true);
            var vehicle = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker).Value;

            var fuel = FuelConsumption.Create(50, 1200).Value;

            var result = vehicle.RecordFuelConsumption(fuel);

            result.IsSuccess.Should().BeTrue();
            vehicle.FuelConsumption.Should().Be(fuel);
            vehicle.DomainEvents.Should().Contain(e => e is FuelConsumptionRecordedEvent);
        }
        [Fact]
        public void Retire_Should_Set_Status_To_Retired()
        {
            var checker = new FakeVehicleUniquenessChecker(true);
            var vehicle = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker).Value;

            var result = vehicle.Retire();

            result.IsSuccess.Should().BeTrue();
            vehicle.Status.Should().Be(VehicleStatus.Retired);
            vehicle.DomainEvents.Should().Contain(e => e is VehicleRetiredEvent);
        }
        [Fact]
        public void UpdateStatus_Should_Raise_StatusChanged_Event()
        {
            var checker = new FakeVehicleUniquenessChecker(true);
            var vehicle = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker).Value;

            var result = vehicle.UpdateStatus(VehicleStatus.InUse);

            result.IsSuccess.Should().BeTrue();
            vehicle.Status.Should().Be(VehicleStatus.InUse);
            vehicle.DomainEvents.Should().Contain(e => e is VehicleStatusChangedEvent);
        }
        [Fact]
        public void SoftDelete_Should_Mark_Vehicle_As_Deleted()
        {
            var checker = new FakeVehicleUniquenessChecker(true);
            var vehicle = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker).Value;

            vehicle.SoftDelete("admin");

            vehicle.IsDeleted.Should().BeTrue();
            vehicle.DeletedAtUtc.Should().NotBeNull();
            vehicle.UpdatedBy.Should().Be("admin");
        }
        [Fact]
        public void DomainEvents_Should_Be_Cleared()
        {
            var checker = new FakeVehicleUniquenessChecker(true);
            var vehicle = Domain.Vehicles.Vehicle.Register(_plate, _spec, checker).Value;

            vehicle.ClearDomainEvents();

            vehicle.DomainEvents.Should().BeEmpty();
        }
    }
}
