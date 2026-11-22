
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptiShape.Controllers;
using OptiShape.Models;

namespace UnitTestAzra;

[TestClass]
public class PlacanjeDodatniTests
{
    private static PlacanjeController Kontroler(
        OptiShape.Data.ApplicationDbContext db, string email)
    {
        var http = new DefaultHttpContext();

        http.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, email) },
                "Test"));

        return new PlacanjeController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = http
            },
            TempData = new TempDataDictionary(
                http, new PrazniTempDataProvider())
        };
    }

    [DataTestMethod]
    [DataRow(true, 7.0)]
    [DataRow(false, 10.0)]
    public async Task CreateForUser_Get_IspravanIznos(
        bool student, double iznos)
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            TestPomocneKlase.DodajKorisnika(
                db, student, "ana@test.com");

            var result = await Kontroler(
                db, "ana@test.com").CreateForUser();

            Assert.IsInstanceOfType(result, typeof(ViewResult));

            var placanje = (Placanje)((ViewResult)result).Model!;

            Assert.AreEqual(iznos, placanje.Iznos);
        }
    }

    [DataTestMethod]
    [DataRow(true, 7.0)]
    [DataRow(false, 10.0)]
    public async Task CreateForUser_Post_IznosSeNeMozeFalsifikovati(
        bool student, double iznos)
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var korisnik = TestPomocneKlase.DodajKorisnika(
                db, student, "ana@test.com");

            var placanje = new Placanje
            {
                DatumPlacanja = DateTime.Today,
                Iznos = 9999,
                NacinPlacanja = NacinPlacanja.KARTICA
            };

            var result = await Kontroler(
                db, "ana@test.com").CreateForUser(placanje);

            Assert.IsInstanceOfType(
                result, typeof(RedirectToActionResult));

            var sacuvano = db.Placanje.Single();

            Assert.AreEqual(
                korisnik.IdKorisnika, sacuvano.IdKorisnika);

            Assert.AreEqual(iznos, sacuvano.Iznos);

            Assert.AreEqual(
                student, sacuvano.PopustPrimijenjen);
        }
    }

    [TestMethod]
    public async Task CreateForUser_NepostojeciKorisnik_VracaNotFound()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var result = await Kontroler(
                db, "nepostoji@test.com").CreateForUser();

            Assert.IsInstanceOfType(
                result, typeof(NotFoundObjectResult));
        }
    }

    [TestMethod]
    public async Task DeleteConfirmed_BrisePostojecePlacanje()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var korisnik = TestPomocneKlase.DodajKorisnika(
                db, false, "ana@test.com");

            var placanje = new Placanje
            {
                DatumPlacanja = DateTime.Today,
                Iznos = 10,
                IdKorisnika = korisnik.IdKorisnika,
                NacinPlacanja = NacinPlacanja.KARTICA
            };

            db.Placanje.Add(placanje);
            await db.SaveChangesAsync();

            var result = await Kontroler(
                db, "ana@test.com")
                .DeleteConfirmed(placanje.IdPlacanja);

            Assert.IsInstanceOfType(
                result, typeof(RedirectToActionResult));

            Assert.AreEqual(0, db.Placanje.Count());
        }
    }
}
