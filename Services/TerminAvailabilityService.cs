using System;
using System.Collections.Generic;
using System.Linq;
using OptiShape.Models;

namespace OptiShape.Services
{
    public class TerminAvailabilityService
    {
        private static readonly TimeSpan PocetakRadnogVremena = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan KrajRadnogVremena = new TimeSpan(22, 0, 0);

        public static bool CanSchedule(Termin noviTermin, IEnumerable<Termin>? postojeciTermini = null)
        {
            if (noviTermin == null)
            {
                throw new ArgumentNullException(nameof(noviTermin));
            }

            if (noviTermin.VrijemeOd < PocetakRadnogVremena || noviTermin.VrijemeDo > KrajRadnogVremena)
            {
                return false;
            }

            if (postojeciTermini != null)
            {
                bool postojiPreklapanje = postojeciTermini.Any(t =>
                    t.Datum.Date == noviTermin.Datum.Date &&
                    noviTermin.VrijemeOd < t.VrijemeDo &&
                    noviTermin.VrijemeDo > t.VrijemeOd);

                if (postojiPreklapanje)
                {
                    return false;
                }
            }

            return true;
        }
    }
}