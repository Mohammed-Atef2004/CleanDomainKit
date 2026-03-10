using Domain.Drivers;
using Domain.Drivers.Enums;
using Domain.Drivers.Errors;
using Domain.Drivers.ValueObjects;
using Domain.Shifts.ValueObjects;
using FluentAssertions;
using Xunit;

public class DriverTests
{
    [Fact]
    public void Hire_Should_Create_Driver_With_Valid_Data()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;

        var result = Driver.Hire("Mohamed Atef", license);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Mohamed Atef");
        result.Value.Status.Should().Be(DriverStatus.Active);
        result.Value.License.Should().BeEquivalentTo(license);
        result.Value.Rating.Value.Should().Be(5.0);
    }

    [Fact]
    public void Hire_Should_Fail_When_Name_Is_Empty()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;

        var result = Driver.Hire("", license);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DriverErrors.EmptyName);
    }

    [Fact]
    public void Suspend_Should_Work_When_Active()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;

        var result = driver.Suspend(DriverSuspensionReason.Misconduct);

        result.IsSuccess.Should().BeTrue();
        driver.Status.Should().Be(DriverStatus.Suspended);
    }

    [Fact]
    public void Reactivate_Should_Work_When_Suspended()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;
        driver.Suspend(DriverSuspensionReason.Misconduct);

        var result = driver.Reactivate();

        result.IsSuccess.Should().BeTrue();
        driver.Status.Should().Be(DriverStatus.Active);
    }

    [Fact]
    public void AssignShift_Should_Assign_When_Active_And_No_CurrentShift()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;
        var shiftId = new ShiftId(Guid.NewGuid());

        var result = driver.AssignShift(shiftId);

        result.IsSuccess.Should().BeTrue();
        driver.CurrentShiftId.Should().Be(shiftId);
    }

    [Fact]
    public void ClearShift_Should_Clear_CurrentShift()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;
        var shiftId = new ShiftId(Guid.NewGuid());
        driver.AssignShift(shiftId);

        driver.ClearShift();

        driver.CurrentShiftId.Should().BeNull();
    }

    [Fact]
    public void RecordTripRating_Should_Update_Rating()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;

        driver.RecordTripRating(4.0);

        driver.Rating.Value.Should().Be(4.0); // (5*0 +4)/1=4
    }

    [Fact]
    public void UpdateName_Should_Fail_When_Empty()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;

        var result = driver.UpdateName("");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DriverErrors.EmptyName);
    }

    [Fact]
    public void UpdateLicense_Should_Change_License()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var newLicense = DriverLicense.Create("999999", DateTime.UtcNow.AddYears(2), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;

        var result = driver.UpdateLicense(newLicense);

        result.IsSuccess.Should().BeTrue();
        driver.License.Should().BeEquivalentTo(newLicense);
    }

    [Fact]
    public void IsAvailable_Should_Return_True_When_Active_And_No_Shift()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;

        driver.IsAvailable().Should().BeTrue();

        driver.AssignShift(new ShiftId(Guid.NewGuid()));
        driver.IsAvailable().Should().BeFalse();
    }

    [Fact]
    public void IsUnderperforming_Should_Return_True_When_Rating_Less_Than_3_5()
    {
        var license = DriverLicense.Create("123456", DateTime.UtcNow.AddYears(1), "B").Value;
        var driver = Driver.Hire("Mohamed", license).Value;

        driver.RecordTripRating(2.0);
        driver.IsUnderperforming().Should().BeTrue();
    }
}