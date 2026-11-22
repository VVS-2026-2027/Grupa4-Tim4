using System;
using System.Collections.Generic;
using OptiShape.Models;
using OptiShape.Services;
using Xunit;

namespace OptiShape.Tests.Services
{
    public class TerminAvailabilityServiceTests
    {
        [Fact]
        public void CanSchedule_WhenSlotIsAvailableWithinWorkingHours_ReturnsTrue()
        {
            // Arrange
            var datum = new DateTime(2026, 12, 1);
            var postojeci = new List<Termin>
            {
                new Termin { Datum = datum, VrijemeOd = new TimeSpan(10, 0, 0) }
            };
            var noviTermin = new Termin
            {
                Datum = datum,
                VrijemeOd = new TimeSpan(12, 0, 0)
            };

            // Act
            var result = TerminAvailabilityService.CanSchedule(noviTermin, postojeci);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanSchedule_WhenOutsideWorkingHours_ReturnsFalse()
        {
            // Arrange
            var noviTermin = new Termin
            {
                Datum = new DateTime(2026, 12, 1),
                VrijemeOd = new TimeSpan(21, 30, 0)
            };

            // Act
            var result = TerminAvailabilityService.CanSchedule(noviTermin, new List<Termin>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanSchedule_WhenSlotOverlapsWithExistingTermin_ReturnsFalse()
        {
            // Arrange
            var datum = new DateTime(2026, 12, 1);
            var postojeci = new List<Termin>
            {
                new Termin { Datum = datum, VrijemeOd = new TimeSpan(10, 0, 0) }
            };
            var noviTermin = new Termin
            {
                Datum = datum,
                VrijemeOd = new TimeSpan(10, 30, 0)
            };

            // Act
            var result = TerminAvailabilityService.CanSchedule(noviTermin, postojeci);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanSchedule_WhenTerminIsNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => TerminAvailabilityService.CanSchedule(null!));
        }
    }
}