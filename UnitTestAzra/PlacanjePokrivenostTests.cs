
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptiShape.Controllers;
using OptiShape.Data;
using OptiShape.Models;

namespace UnitTestAzra
{
    [TestClass]
    public class PlacanjePokrivenostTests
    {
        private static PlacanjeController Kontroler(
            ApplicationDbContext db, string email = "ana@test.com")
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

        private static Placanje NovoPlacanje(int idKorisnika)
        {
            return new Placanje
            {
                DatumPlacanja = DateTime.Today,
                Iznos = 10,
                NacinPlacanja = NacinPlacanja.KARTICA,
                IdKorisnika = idKorisnika
            };
        }

        [TestMethod]
        public async Task Index_VracaSvaPlacanja()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                db.Placanje.Add(NovoPlacanje(korisnik.IdKorisnika));
                db.Placanje.Add(NovoPlacanje(korisnik.IdKorisnika));
                await db.SaveChangesAsync();

                var rezultat = await Kontroler(db).Index();

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

                var view = (ViewResult)rezultat;
                var placanja = (System.Collections.Generic.IEnumerable<Placanje>)view.Model!;

                Assert.AreEqual(2, placanja.Count());
                Assert.IsTrue(placanja.All(p => p.Korisnik != null));
            }
        }

        [TestMethod]
        public async Task Details_PostojeciId_VracaPlacanje()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var placanje = NovoPlacanje(korisnik.IdKorisnika);
                db.Placanje.Add(placanje);
                await db.SaveChangesAsync();

                var rezultat = await Kontroler(db)
                    .Details(placanje.IdPlacanja);

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

                var model = (Placanje)((ViewResult)rezultat).Model!;

                Assert.AreEqual(placanje.IdPlacanja, model.IdPlacanja);
                Assert.AreEqual(10.0, model.Iznos);
            }
        }

        [TestMethod]
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var rezultat = await Kontroler(db).Details(999);

                Assert.IsInstanceOfType(
                    rezultat, typeof(NotFoundResult));
            }
        }

        [TestMethod]
        public void CreateGet_VracaFormuSaKorisnicima()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var kontroler = Kontroler(db);
                var rezultat = kontroler.Create();

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
                Assert.IsInstanceOfType(
                    kontroler.ViewData["IdKorisnika"],
                    typeof(SelectList));
            }
        }

        [TestMethod]
        public async Task CreatePost_IspravnoPlacanje_SacuvaUBazu()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var rezultat = await Kontroler(db).Create(
                    NovoPlacanje(korisnik.IdKorisnika));

                Assert.IsInstanceOfType(
                    rezultat, typeof(RedirectToActionResult));

                Assert.AreEqual(1, db.Placanje.Count());
                Assert.AreEqual(10.0, db.Placanje.Single().Iznos);
            }
        }

        [TestMethod]
        public async Task CreatePost_NeispravanModel_NeSacuvaPlacanje()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var kontroler = Kontroler(db);
                kontroler.ModelState.AddModelError(
                    "Iznos", "Neispravan iznos");

                var rezultat = await kontroler.Create(
                    NovoPlacanje(korisnik.IdKorisnika));

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
                Assert.AreEqual(0, db.Placanje.Count());

                Assert.IsNotNull(
                    kontroler.ViewData["IdKorisnika"]);
            }
        }

        [TestMethod]
        public async Task EditGet_PostojeciId_VracaPlacanje()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var placanje = NovoPlacanje(korisnik.IdKorisnika);
                db.Placanje.Add(placanje);
                await db.SaveChangesAsync();

                var rezultat = await Kontroler(db)
                    .Edit((int?)placanje.IdPlacanja);

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

                var model = (Placanje)((ViewResult)rezultat).Model!;

                Assert.AreEqual(placanje.IdPlacanja, model.IdPlacanja);
            }
        }

        [TestMethod]
        public async Task EditGet_NepostojeciId_VracaNotFound()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var rezultat = await Kontroler(db).Edit((int?)999);

                Assert.IsInstanceOfType(
                    rezultat, typeof(NotFoundResult));
            }
        }

        [TestMethod]
        public async Task EditPost_IspravnoPlacanje_AzuriraIznos()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var placanje = NovoPlacanje(korisnik.IdKorisnika);
                db.Placanje.Add(placanje);
                await db.SaveChangesAsync();

                placanje.Iznos = 30;

                var rezultat = await Kontroler(db)
                    .Edit(placanje.IdPlacanja, placanje);

                Assert.IsInstanceOfType(
                    rezultat, typeof(RedirectToActionResult));

                var sacuvano = await db.Placanje
                    .FindAsync(placanje.IdPlacanja);

                Assert.AreEqual(30.0, sacuvano!.Iznos);
            }
        }

        [TestMethod]
        public async Task EditPost_NeispravanModel_VracaFormu()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var placanje = NovoPlacanje(korisnik.IdKorisnika);
                db.Placanje.Add(placanje);
                await db.SaveChangesAsync();

                var kontroler = Kontroler(db);
                kontroler.ModelState.AddModelError(
                    "Iznos", "Neispravan iznos");

                var rezultat = await kontroler.Edit(
                    placanje.IdPlacanja, placanje);

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

                Assert.IsNotNull(
                    kontroler.ViewData["IdKorisnika"]);
            }
        }

        [TestMethod]
        public async Task EditPost_NepostojeciZapis_VracaNotFound()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var placanje = NovoPlacanje(1);
                placanje.IdPlacanja = 999;

                var rezultat = await Kontroler(db)
                    .Edit(999, placanje);

                Assert.IsInstanceOfType(
                    rezultat, typeof(NotFoundResult));
            }
        }

        [TestMethod]
        public async Task DeleteGet_PostojeciId_VracaPlacanje()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, false, "ana@test.com");

                var placanje = NovoPlacanje(korisnik.IdKorisnika);
                db.Placanje.Add(placanje);
                await db.SaveChangesAsync();

                var rezultat = await Kontroler(db)
                    .Delete(placanje.IdPlacanja);

                Assert.IsInstanceOfType(rezultat, typeof(ViewResult));

                var model = (Placanje)((ViewResult)rezultat).Model!;

                Assert.AreEqual(placanje.IdPlacanja, model.IdPlacanja);
            }
        }

        [TestMethod]
        public async Task DeleteGet_NepostojeciId_VracaNotFound()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var rezultat = await Kontroler(db).Delete(999);

                Assert.IsInstanceOfType(
                    rezultat, typeof(NotFoundResult));
            }
        }

        [TestMethod]
        public async Task DeleteConfirmed_NepostojeciId_NeBacaGresku()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var rezultat = await Kontroler(db)
                    .DeleteConfirmed(999);

                Assert.IsInstanceOfType(
                    rezultat, typeof(RedirectToActionResult));

                Assert.AreEqual(0, db.Placanje.Count());
            }
        }

        [TestMethod]
        public async Task CreateForUserPost_NepostojeciKorisnik_VracaNotFound()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var kontroler = Kontroler(
                    db, "nepostoji@test.com");

                var rezultat = await kontroler.CreateForUser(
                    NovoPlacanje(0));

                Assert.IsInstanceOfType(
                    rezultat, typeof(NotFoundObjectResult));

                Assert.AreEqual(0, db.Placanje.Count());
            }
        }

        [TestMethod]
        public async Task CreateForUserPost_NeispravanModel_NeSacuvaPlacanje()
        {
            var (db, connection) = TestPomocneKlase.NovaBaza();
            using (connection)
            using (db)
            {
                var korisnik = TestPomocneKlase.DodajKorisnika(
                    db, true, "ana@test.com");

                var kontroler = Kontroler(db);

                kontroler.ModelState.AddModelError(
                    "NacinPlacanja", "Neispravan nacin placanja");

                var rezultat = await kontroler.CreateForUser(
                    NovoPlacanje(korisnik.IdKorisnika));

                Assert.IsInstanceOfType(
                    rezultat, typeof(ViewResult));

                Assert.AreEqual(0, db.Placanje.Count());
                Assert.IsTrue(kontroler.ModelState.ErrorCount > 0);
            }
        }
    }
}
