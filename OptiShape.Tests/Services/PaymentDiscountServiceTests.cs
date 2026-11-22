using System;
using OptiShape.Models;
using OptiShape.Services;
using Xunit;

namespace OptiShape.Tests.Services
{
    public class PaymentDiscountServiceTests
    {
        [Fact]
        public void ApplyStudentDiscount_WhenUserIsStudentAndDiscountNotApplied_Applies20PercentDiscount()
        {
            // Arrange
            var korisnik = new Korisnik { StudentskiStatus = true };
            var placanje = new Placanje { Iznos = 100.0, PopustPrimijenjen = false };

            // Act
            var result = PaymentDiscountService.ApplyStudentDiscount(placanje, korisnik);

            // Assert
            Assert.True(result);
            Assert.Equal(80.0, placanje.Iznos);
            Assert.True(placanje.PopustPrimijenjen);
        }

        [Fact]
        public void ApplyStudentDiscount_WhenUserIsNotStudent_DoesNotApplyDiscount()
        {
            // Arrange
            var korisnik = new Korisnik { StudentskiStatus = false };
            var placanje = new Placanje { Iznos = 100.0, PopustPrimijenjen = false };

            // Act
            var result = PaymentDiscountService.ApplyStudentDiscount(placanje, korisnik);

            // Assert
            Assert.False(result);
            Assert.Equal(100.0, placanje.Iznos);
            Assert.False(placanje.PopustPrimijenjen);
        }

        [Fact]
        public void ApplyStudentDiscount_WhenDiscountAlreadyApplied_DoesNotApplyDiscountAgain()
        {
            // Arrange
            var korisnik = new Korisnik { StudentskiStatus = true };
            var placanje = new Placanje { Iznos = 80.0, PopustPrimijenjen = true };

            // Act
            var result = PaymentDiscountService.ApplyStudentDiscount(placanje, korisnik);

            // Assert
            Assert.False(result);
            Assert.Equal(80.0, placanje.Iznos);
            Assert.True(placanje.PopustPrimijenjen);
        }

        [Fact]
        public void ApplyStudentDiscount_WhenPlacanjeIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var korisnik = new Korisnik { StudentskiStatus = true };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => PaymentDiscountService.ApplyStudentDiscount(null!, korisnik));
        }

        [Fact]
        public void ApplyStudentDiscount_WhenKorisnikIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var placanje = new Placanje { Iznos = 100.0 };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => PaymentDiscountService.ApplyStudentDiscount(placanje, null!));
        }
    }
}