
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptiShape.Controllers;
using OptiShape.Models;

namespace UnitTestAzra
{
    [TestClass]
    public class PlacanjeModelTests
    {
        private static List<ValidationResult> Validiraj(Placanje placanje)
        {
            var greske = new List<ValidationResult>();

            Validator.TryValidateObject(
                placanje,
                new ValidationContext(placanje),
                greske,
                validateAllProperties: true);

            return greske;
        }

        [DataTestMethod]
        [DataRow(7.0, true)]
        [DataRow(10.0, true)]
        [DataRow(0.01, true)]
        [DataRow(10000.0, true)]
        [DataRow(0.0, false)]
        [DataRow(-5.0, false)]
        [DataRow(10000.01, false)]
        public void Iznos_ValidacijaProvjeravaGranice(
            double iznos, bool ispravno)
        {
            var placanje = new Placanje
            {
                DatumPlacanja = DateTime.Today,
                Iznos = iznos,
                IdKorisnika = 1
            };

            var greske = Validiraj(placanje);

            bool postojiGreskaIznosa = greske.Exists(g =>
                g.MemberNames != null &&
                System.Linq.Enumerable.Contains(
                    g.MemberNames, nameof(Placanje.Iznos)));

            Assert.AreEqual(ispravno, !postojiGreskaIznosa);
        }

        [TestMethod]
        public void Placanje_CuvaPodatkeKojiSuPostavljeni()
        {
            var datum = new DateTime(2026, 11, 22);

            var placanje = new Placanje
            {
                IdPlacanja = 3,
                DatumPlacanja = datum,
                Iznos = 7.0,
                PopustPrimijenjen = true,
                IdKorisnika = 12
            };

            Assert.AreEqual(3, placanje.IdPlacanja);
            Assert.AreEqual(datum, placanje.DatumPlacanja);
            Assert.AreEqual(7.0, placanje.Iznos);
            Assert.IsTrue(placanje.PopustPrimijenjen);
            Assert.AreEqual(12, placanje.IdKorisnika);
        }
    }

    [TestClass]
    public class PlacanjeControllerTests
    {
        // Ove metode vracaju rezultat prije pristupa bazi.
        private static PlacanjeController KreirajKontroler()
        {
            return new PlacanjeController(null!);
        }

        [TestMethod]
        public async Task Details_BezId_VracaNotFound()
        {
            var rezultat = await KreirajKontroler().Details(null);

            Assert.IsTrue(rezultat is NotFoundResult);
        }

        [TestMethod]
        public async Task EditGet_BezId_VracaNotFound()
        {
            var rezultat = await KreirajKontroler().Edit((int?)null);

            Assert.IsTrue(rezultat is NotFoundResult);
        }

        [TestMethod]
        public async Task DeleteGet_BezId_VracaNotFound()
        {
            var rezultat = await KreirajKontroler().Delete((int?)null);

            Assert.IsTrue(rezultat is NotFoundResult);
        }

        [TestMethod]
        public async Task EditPost_NeusaglasenId_VracaNotFound()
        {
            var placanje = new Placanje
            {
                IdPlacanja = 3
            };

            var rezultat = await KreirajKontroler().Edit(8, placanje);

            Assert.IsTrue(rezultat is NotFoundResult);
        }
    }
}
