using Domain.Drivers.ValueObjects;
using Domain.Shifts;
using Domain.Shifts.Enums;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftStatus = Domain.Shifts.ShiftStatus;

namespace Domain.Tests
{
    public class ShiftTests
    {
        [Fact]
        public void Create_Should_Fail_When_StartAfterEnd()
        {
            var result = Shift.Create(new DriverId(Guid.NewGuid()),
                DateTime.UtcNow, DateTime.UtcNow.AddHours(-1));

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Shift.InvalidDuration");
        }

        [Fact]
        public void Start_Should_Change_Status_To_Active()
        {
            var shift = Shift.Create(new DriverId(Guid.NewGuid()),
                DateTime.UtcNow, DateTime.UtcNow.AddHours(2)).Value;

            var result = shift.Start();

            result.IsSuccess.Should().BeTrue();
            shift.Status.Should().Be(ShiftStatus.Active);
        }
    }
}
