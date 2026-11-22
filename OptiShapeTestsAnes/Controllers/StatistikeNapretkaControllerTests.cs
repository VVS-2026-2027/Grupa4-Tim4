using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using OptiShape.Controllers;
using OptiShape.Data;
using OptiShape.Models;
using OptiShapeTestsAnes.Helpers;

namespace OptiShapeTestsAnes.Controllers
{
    [TestClass]
    [TestCategory("StatistikeNapretka")]
    public class StatistikeNapretkaControllerTests
    {
        private const int StatistikaKorisnikaId = 1;       // korisnik trenera 1
        private const int StatistikaDrugogKorisnikaId = 2; // korisnik trenera 2
        private const int NepostojecaStatistikaId = 99;
        private const int NepostojeciKorisnikId = 999;
        private const int DataDrivenKorisnikId = 50;
        private const string DataDrivenEmail = "datadriven@optishape.ba";

        private ApplicationDbContext baza = null!;
        private Mock<UserManager<ApplicationUser>> userManagerMock = null!;
        private StatistikeNapretkaController kontroler = null!;

        // ---------- Podaci za data-driven testove (CSV i JSON) ----------

        public static IEnumerable<object[]> BmiPodaciIzCsv =>
            File.ReadAllLines(TestHelper.PutanjaPodataka("bmi_podaci.csv"))
                .Skip(1)
                .Select(red => red.Split(','))
                .Select(k => new object[]
                {
                    double.Parse(k[0], CultureInfo.InvariantCulture),
                    double.Parse(k[1], CultureInfo.InvariantCulture),
                    double.Parse(k[2], CultureInfo.InvariantCulture)
                });

        public static IEnumerable<object[]> StatistikeIzJson =>
            JsonSerializer.Deserialize<List<StatistikaSlucaj>>(
                    File.ReadAllText(TestHelper.PutanjaPodataka("statistike_korisnika.json")))!
                .Select(s => new object[] { s.Tezina, s.Visina, s.OcekivaniBmi, s.OcekivaniKalorijskiUnos });

        public record StatistikaSlucaj(double Tezina, double Visina, double OcekivaniBmi, int OcekivaniKalorijskiUnos);

        // ---------- Priprema ----------

        [TestInitialize]
        public void Inicijalizacija()
        {
            baza = TestHelper.NovaBaza();
            TestHelper.DodajKorisnike(baza);
            baza.StatistikeNapretka.AddRange(
                NapraviStatistiku(StatistikaKorisnikaId, TestHelper.KorisnikId, new DateTime(2026, 11, 10)),
                NapraviStatistiku(StatistikaDrugogKorisnikaId, TestHelper.DrugiKorisnikId, new DateTime(2026, 11, 5)),
                NapraviStatistiku(3, TestHelper.KorisnikId, new DateTime(2026, 11, 1)));
            baza.SaveChanges();
            baza.ChangeTracker.Clear();

            userManagerMock = TestHelper.MockUserManager();
            kontroler = new StatistikeNapretkaController(baza, userManagerMock.Object);
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.AdminEmail, "Administrator");
        }

        [TestCleanup]
        public void Ciscenje()
        {
            kontroler.Dispose();
            baza.Dispose();
        }

        private static StatistikeNapretka NapraviStatistiku(int id, int idKorisnika, DateTime datum) => new StatistikeNapretka
        {
            IdZapisa = id,
            IdKorisnika = idKorisnika,
            Datum = datum,
            Tezina = 80,
            Bmi = 24.69,
            KalorijskiUnos = 2300
        };

        private void DodajKorisnikaSaVisinom(double visina)
        {
            baza.Korisnik.Add(TestHelper.NapraviKorisnika(DataDrivenKorisnikId, DataDrivenEmail, visina));
            baza.SaveChanges();
            baza.ChangeTracker.Clear();
        }

        // ---------- Konstruktor (zamjenski objekat) ----------

        [TestMethod]
        [TestCategory("Mock")]
        public async Task Index_Administrator_KorisnikaCitaIzClaimovaANeIzUserManagera()
        {
            var rezultat = await kontroler.Index();

            Assert.IsInstanceOfType<ViewResult>(rezultat);
            userManagerMock.Verify(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
            userManagerMock.Verify(u => u.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        // ---------- Index ----------

        [TestMethod]
        [Priority(1)]
        [DataRow("Administrator", TestHelper.AdminEmail, 3)]
        [DataRow("Trener", TestHelper.TrenerEmail, 2)]
        [DataRow("Korisnik", TestHelper.KorisnikEmail, 2)]
        public async Task Index_PoUlogama_VracaOdgovarajuciBrojStatistika(string uloga, string email, int ocekivaniBroj)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.Index() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivaniBroj, ((List<StatistikeNapretka>)rezultat.Model!).Count);
        }

        [TestMethod]
        public async Task Index_TrenerNijeUBazi_VracaUnauthorized()
        {
            TestHelper.PrijaviKorisnika(kontroler, "nepoznat@optishape.ba", "Trener");

            var rezultat = await kontroler.Index();

            Assert.IsInstanceOfType<UnauthorizedResult>(rezultat);
        }

        [TestMethod]
        public async Task Index_KorisnikNijeUBazi_VracaNotFoundSaPorukom()
        {
            TestHelper.PrijaviKorisnika(kontroler, "nepoznat@optishape.ba", "Korisnik");

            var rezultat = await kontroler.Index() as NotFoundObjectResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Korisnik nije pronađen.", rezultat.Value);
        }

        // ---------- Details ----------

        [TestMethod]
        [DataRow(null)]
        [DataRow(NepostojecaStatistikaId)]
        public async Task Details_IdNullIliNepostojeci_VracaNotFound(int? id)
        {
            var rezultat = await kontroler.Details(id);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        [DataRow("Administrator", TestHelper.AdminEmail)]
        [DataRow("Korisnik", TestHelper.KorisnikEmail)]
        [DataRow("Trener", TestHelper.TrenerEmail)]
        public async Task Details_DozvoljenPristup_VracaViewSaStatistikom(string uloga, string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.Details(StatistikaKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(StatistikaKorisnikaId, ((StatistikeNapretka)rezultat.Model!).IdZapisa);
        }

        [TestMethod]
        [DataRow("Korisnik", TestHelper.KorisnikEmail)]
        [DataRow("Korisnik", "nepoznat@optishape.ba")]
        [DataRow("Trener", TestHelper.TrenerEmail)]
        [DataRow("Trener", "nepoznat@optishape.ba")]
        public async Task Details_TudjaStatistika_VracaForbid(string uloga, string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.Details(StatistikaDrugogKorisnikaId);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        // ---------- Create (GET) ----------

        [TestMethod]
        public void Create_Get_PunPadajuciMeniSaSvimKorisnicima()
        {
            var rezultat = kontroler.Create() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(5, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        // ---------- Create (POST) ----------

        [TestMethod]
        [TestCategory("DataDriven")]
        [TestProperty("Izvor", "CSV")]
        [DynamicData(nameof(BmiPodaciIzCsv))]
        public async Task Create_PodaciIzCsv_RacunaBmiPremaVisiniKorisnika(double tezina, double visina, double ocekivaniBmi)
        {
            DodajKorisnikaSaVisinom(visina);
            var statistika = new StatistikeNapretka { IdKorisnika = DataDrivenKorisnikId, Datum = DateTime.Today, Tezina = tezina };

            await kontroler.Create(statistika);

            Assert.AreEqual(ocekivaniBmi, statistika.Bmi, 0.001);
        }

        [TestMethod]
        [Priority(1)]
        public async Task Create_ValidnaStatistika_SnimaIPreusmjerava()
        {
            var statistika = new StatistikeNapretka { IdKorisnika = TestHelper.KorisnikId, Datum = DateTime.Today, Tezina = 81 };

            var rezultat = await kontroler.Create(statistika) as RedirectToActionResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.AreEqual(4, baza.StatistikeNapretka.Count());
            Assert.AreEqual("Statistika napretka je uspješno dodana.", kontroler.TempData["SuccessMessage"]);
        }

        [TestMethod]
        [DataRow(NepostojeciKorisnikId, "Korisnik nije pronađen.")]
        [DataRow(TestHelper.KorisnikBezVisineId, "Visina korisnika nije validna.")]
        public async Task Create_NepostojeciKorisnikIliNevalidnaVisina_VracaFormuSaGreskom(int idKorisnika, string ocekivanaGreska)
        {
            var statistika = new StatistikeNapretka { IdKorisnika = idKorisnika, Datum = DateTime.Today, Tezina = 80 };

            var rezultat = await kontroler.Create(statistika) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivanaGreska, kontroler.ModelState["IdKorisnika"]!.Errors[0].ErrorMessage);
            Assert.AreEqual(3, baza.StatistikeNapretka.Count());
        }

        // ---------- Edit (GET) ----------

        [TestMethod]
        [DataRow(null)]
        [DataRow(NepostojecaStatistikaId)]
        public async Task Edit_Get_IdNullIliNepostojeci_VracaNotFound(int? id)
        {
            var rezultat = await kontroler.Edit(id);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        [DataRow("Administrator", TestHelper.AdminEmail)]
        [DataRow("Trener", TestHelper.TrenerEmail)]
        public async Task Edit_Get_DozvoljenPristup_VracaViewSaPadajucimMenijem(string uloga, string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.Edit(StatistikaKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(5, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        [TestMethod]
        [DataRow(TestHelper.TrenerEmail)]
        [DataRow("nepoznat@optishape.ba")]
        public async Task Edit_Get_TrenerTudjegKorisnika_VracaForbid(string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, "Trener");

            var rezultat = await kontroler.Edit(StatistikaDrugogKorisnikaId);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        // ---------- Edit (POST) ----------

        [TestMethod]
        public async Task Edit_Post_IdSeNePoklapa_VracaNotFound()
        {
            var statistika = NapraviStatistiku(StatistikaKorisnikaId, TestHelper.KorisnikId, DateTime.Today);

            var rezultat = await kontroler.Edit(StatistikaDrugogKorisnikaId, statistika);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        [Priority(1)]
        [DataRow("Administrator", TestHelper.AdminEmail)]
        [DataRow("Trener", TestHelper.TrenerEmail)]
        public async Task Edit_Post_ValidnaIzmjena_PreracunavaBmiIPreusmjerava(string uloga, string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);
            var statistika = NapraviStatistiku(StatistikaKorisnikaId, TestHelper.KorisnikId, DateTime.Today);
            statistika.Tezina = 90;

            var rezultat = await kontroler.Edit(StatistikaKorisnikaId, statistika) as RedirectToActionResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(27.78, baza.StatistikeNapretka.Single(s => s.IdZapisa == StatistikaKorisnikaId).Bmi, 0.001);
            Assert.AreEqual("Statistika napretka je uspješno ažurirana.", kontroler.TempData["SuccessMessage"]);
        }

        [TestMethod]
        [DataRow(TestHelper.TrenerEmail)]
        [DataRow("nepoznat@optishape.ba")]
        public async Task Edit_Post_TrenerTudjegKorisnika_VracaForbid(string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, "Trener");
            var statistika = NapraviStatistiku(StatistikaDrugogKorisnikaId, TestHelper.DrugiKorisnikId, DateTime.Today);

            var rezultat = await kontroler.Edit(StatistikaDrugogKorisnikaId, statistika);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        [DataRow(NepostojeciKorisnikId, "Korisnik nije pronađen.")]
        [DataRow(TestHelper.KorisnikBezVisineId, "Visina korisnika nije validna.")]
        public async Task Edit_Post_NepostojeciKorisnikIliNevalidnaVisina_VracaFormuSaGreskom(int idKorisnika, string ocekivanaGreska)
        {
            var statistika = NapraviStatistiku(StatistikaKorisnikaId, idKorisnika, DateTime.Today);

            var rezultat = await kontroler.Edit(StatistikaKorisnikaId, statistika) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivanaGreska, kontroler.ModelState["IdKorisnika"]!.Errors[0].ErrorMessage);
            Assert.AreEqual(5, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task Edit_Post_StatistikaObrisanaUMedjuvremenu_VracaNotFound()
        {
            var statistika = NapraviStatistiku(NepostojecaStatistikaId, TestHelper.KorisnikId, DateTime.Today);

            var rezultat = await kontroler.Edit(NepostojecaStatistikaId, statistika);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        // ---------- Delete ----------

        [TestMethod]
        [DataRow(null)]
        [DataRow(NepostojecaStatistikaId)]
        public async Task Delete_Get_IdNullIliNepostojeci_VracaNotFound(int? id)
        {
            var rezultat = await kontroler.Delete(id);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_Get_PostojecaStatistika_VracaViewZaPotvrdu()
        {
            var rezultat = await kontroler.Delete(StatistikaKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(StatistikaKorisnikaId, ((StatistikeNapretka)rezultat.Model!).IdZapisa);
        }

        [TestMethod]
        public async Task DeleteConfirmed_PostojecaStatistika_BriseIPostavljaPoruku()
        {
            var rezultat = await kontroler.DeleteConfirmed(StatistikaKorisnikaId);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual(2, baza.StatistikeNapretka.Count());
            Assert.AreEqual("Statistika napretka je uspješno obrisana.", kontroler.TempData["DeleteMessage"]);
        }

        [TestMethod]
        public async Task DeleteConfirmed_NepostojecaStatistika_PreusmjeravaBezBrisanja()
        {
            var rezultat = await kontroler.DeleteConfirmed(NepostojecaStatistikaId);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual(3, baza.StatistikeNapretka.Count());
        }

        // ---------- CreateForUser ----------

        [TestMethod]
        public void CreateForUser_Get_VracaFormuSaDanasnjimDatumom()
        {
            var rezultat = kontroler.CreateForUser() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("CreateForUser", rezultat.ViewName);
            Assert.AreEqual(DateTime.Today, ((StatistikeNapretka)rezultat.Model!).Datum);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [TestProperty("Izvor", "JSON")]
        [DynamicData(nameof(StatistikeIzJson))]
        public async Task CreateForUser_PodaciIzJson_RacunaBmiIKalorijskiUnos(double tezina, double visina, double ocekivaniBmi, int ocekivaniUnos)
        {
            DodajKorisnikaSaVisinom(visina);
            TestHelper.PrijaviKorisnika(kontroler, DataDrivenEmail, "Korisnik");
            var statistika = new StatistikeNapretka { Datum = DateTime.Today, Tezina = tezina };

            var rezultat = await kontroler.CreateForUser(statistika);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual(ocekivaniBmi, statistika.Bmi, 0.001);
            Assert.AreEqual(ocekivaniUnos, statistika.KalorijskiUnos);
            Assert.AreEqual(DataDrivenKorisnikId, statistika.IdKorisnika);
        }

        [TestMethod]
        public async Task CreateForUser_KorisnikNijeUBazi_VracaNotFound()
        {
            TestHelper.PrijaviKorisnika(kontroler, "nepoznat@optishape.ba", "Korisnik");

            var rezultat = await kontroler.CreateForUser(new StatistikeNapretka { Tezina = 80 });

            Assert.IsInstanceOfType<NotFoundObjectResult>(rezultat);
        }

        [TestMethod]
        public async Task CreateForUser_KorisnikBezVisine_VracaFormuSaGreskom()
        {
            TestHelper.PrijaviKorisnika(kontroler, "bezvisine@optishape.ba", "Korisnik");

            var rezultat = await kontroler.CreateForUser(new StatistikeNapretka { Tezina = 80 }) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Visina korisnika nije validna.", kontroler.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        [TestMethod]
        public async Task CreateForUser_NevalidanModel_VracaFormuBezSnimanja()
        {
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.KorisnikEmail, "Korisnik");
            kontroler.ModelState.AddModelError("Tezina", "Težina mora biti između 30 i 200 kg.");

            var rezultat = await kontroler.CreateForUser(new StatistikeNapretka { Tezina = 500 }) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("CreateForUser", rezultat.ViewName);
            Assert.AreEqual(3, baza.StatistikeNapretka.Count());
        }

        // ---------- Graf ----------

        [TestMethod]
        [DataRow("Administrator", TestHelper.AdminEmail, 3)]
        [DataRow("Trener", TestHelper.TrenerEmail, 2)]
        [DataRow("Korisnik", TestHelper.KorisnikEmail, 2)]
        public async Task Graf_PoUlogama_VracaStatistikeSortiranePoDatumu(string uloga, string email, int ocekivaniBroj)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.Graf() as ViewResult;
            var statistike = (List<StatistikeNapretka>)rezultat!.Model!;

            Assert.AreEqual("Graf", rezultat.ViewName);
            Assert.AreEqual(ocekivaniBroj, statistike.Count);
            Assert.IsTrue(statistike.First().Datum <= statistike.Last().Datum);
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        [DataRow("Trener")]
        [DataRow("Korisnik")]
        public async Task Graf_PrijavljeniNijeUBazi_BacaInvalidOperationException(string uloga)
        {
            TestHelper.PrijaviKorisnika(kontroler, "nepoznat@optishape.ba", uloga);

            var izuzetak = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => kontroler.Graf());

            Assert.IsInstanceOfType<NullReferenceException>(izuzetak.InnerException);
        }
    }
}
