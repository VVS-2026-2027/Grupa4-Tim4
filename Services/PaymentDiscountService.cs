using System;
using OptiShape.Models;

namespace OptiShape.Services
{
    public class PaymentDiscountService
    {
        private const double StudentskiPopustStopa = 0.20;

        public static bool ApplyStudentDiscount(Placanje placanje, Korisnik korisnik)
        {
            if (placanje == null)
            {
                throw new ArgumentNullException(nameof(placanje));
            }

            if (korisnik == null)
            {
                throw new ArgumentNullException(nameof(korisnik));
            }

            if (!korisnik.StudentskiStatus || placanje.PopustPrimijenjen)
            {
                return false;
            }

            placanje.Iznos = Math.Round(placanje.Iznos * (1 - StudentskiPopustStopa), 2);
            placanje.PopustPrimijenjen = true;
            return true;
        }
    }
}