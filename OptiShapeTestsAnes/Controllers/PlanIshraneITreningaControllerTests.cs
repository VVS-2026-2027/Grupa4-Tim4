using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OptiShape.Controllers;
using OptiShape.Data;
using OptiShape.Models;
using OptiShapeTestsAnes.Helpers;

namespace OptiShapeTestsAnes.Controllers
{
    [TestClass]
    [TestCategory("PlanIshraneITreninga")]
    public class PlanIshraneITreningaControllerTests
    {
        private const int PlanKorisnikaId = 1;      // plan korisnika kojeg vodi trener 1
        private const int PlanDrugogKorisnikaId = 2; // plan korisnika kojeg vodi trener 2
        private const int NepostojeciPlanId = 99;

        private static DateTime datumPlana;

        private ApplicationDbContext baza = null!;
        private PlanIshraneITreningaController kontroler = null!;

        [ClassInitialize]
        public static void InicijalizacijaKlase(TestContext context)
        {
            datumPlana = new DateTime(2026, 11, 1);
        }

        [ClassCleanup]
        public static void CiscenjeKlase()
        {
            datumPlana = default;
        }

        [TestInitialize]
        public void Inicijalizacija()
        {
            baza = TestHelper.NovaBaza();
            TestHelper.DodajKorisnike(baza);
            baza.PlanIshraneTreninga.AddRange(
                NapraviPlan(PlanKorisnikaId, TestHelper.KorisnikId),
                NapraviPlan(PlanDrugogKorisnikaId, TestHelper.DrugiKorisnikId));
            baza.SaveChanges();
            baza.ChangeTracker.Clear();

            kontroler = new PlanIshraneITreningaController(baza);
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.AdminEmail, "Administrator");
        }

        [TestCleanup]
        public void Ciscenje()
        {
            kontroler.Dispose();
            baza.Dispose();
        }

        private static PlanIshraneTreninga NapraviPlan(int id, int idKorisnika) => new PlanIshraneTreninga
        {
            IdPlana = id,
            IdKorisnika = idKorisnika,
            DatumKreiranja = datumPlana,
            Kalorije = 2000,
            Protein = 150,
            Ugljikohidrati = 200,
            Masti = 60
        };

        // ---------- Index ----------

        [TestMethod]
        [Priority(1)]
        [DataRow("Administrator", TestHelper.AdminEmail, "IndexAdmin", 2)]
        [DataRow("Trener", TestHelper.TrenerEmail, "IndexAdmin", 1)]
        [DataRow("Korisnik", TestHelper.KorisnikEmail, "IndexKorisnik", 1)]
        public async Task Index_PoUlogama_VracaOdgovarajuciViewIBrojPlanova(string uloga, string email, string ocekivaniView, int ocekivaniBroj)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.Index() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivaniView, rezultat.ViewName);
            Assert.AreEqual(ocekivaniBroj, ((List<PlanIshraneTreninga>)rezultat.Model!).Count);
        }

        [TestMethod]
        [DataRow("Trener")]
        [DataRow("Korisnik")]
        [DataRow(null)]
        public async Task Index_KorisnikNijeUBaziIliNemaUlogu_VracaUnauthorized(string? uloga)
        {
            TestHelper.PrijaviKorisnika(kontroler, "nepoznat@optishape.ba", uloga);

            var rezultat = await kontroler.Index();

            Assert.IsInstanceOfType<UnauthorizedResult>(rezultat);
        }

        // ---------- Details ----------

        [TestMethod]
        public async Task Details_IdNull_VracaNotFound()
        {
            var rezultat = await kontroler.Details(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_NepostojeciPlan_VracaNotFound()
        {
            var rezultat = await kontroler.Details(NepostojeciPlanId);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_AdminPostojeciPlan_VracaViewSaPlanom()
        {
            var rezultat = await kontroler.Details(PlanKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(PlanKorisnikaId, ((PlanIshraneTreninga)rezultat.Model!).IdPlana);
        }

        [TestMethod]
        public async Task Details_TrenerSvogKorisnika_VracaView()
        {
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.TrenerEmail, "Trener");

            var rezultat = await kontroler.Details(PlanKorisnikaId);

            Assert.IsInstanceOfType<ViewResult>(rezultat);
        }

        [TestMethod]
        [DataRow(TestHelper.TrenerEmail)]
        [DataRow("nepoznat@optishape.ba")]
        public async Task Details_TrenerTudjegKorisnikaIliNepostojeciTrener_VracaForbid(string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, "Trener");

            var rezultat = await kontroler.Details(PlanDrugogKorisnikaId);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        // ---------- Create (GET) ----------

        [TestMethod]
        [DataRow("Administrator", TestHelper.AdminEmail, 5)]
        [DataRow("Trener", TestHelper.TrenerEmail, 2)]
        [DataRow("Trener", "nepoznat@optishape.ba", 0)]
        public async Task Create_Get_PunPadajuciMeniPremaUlozi(string uloga, string email, int ocekivaniBroj)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.Create() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivaniBroj, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        // ---------- Create (POST) ----------

        [TestMethod]
        [Priority(1)]
        public async Task Create_ValidanPlan_SnimaPlanIPreusmjerava()
        {
            var plan = NapraviPlan(10, TestHelper.KorisnikId);

            var rezultat = await kontroler.Create(plan) as RedirectToActionResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.AreEqual(3, baza.PlanIshraneTreninga.Count());
            Assert.AreEqual("Plan je uspješno dodat.", kontroler.TempData["SuccessMessage"]);
        }

        [TestMethod]
        [DataRow("Administrator", TestHelper.AdminEmail, 5)]
        [DataRow("Trener", TestHelper.TrenerEmail, 2)]
        [DataRow("Trener", "nepoznat@optishape.ba", 0)]
        public async Task Create_NevalidanModel_VracaFormuSaPadajucimMenijem(string uloga, string email, int ocekivaniBroj)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);
            kontroler.ModelState.AddModelError("Kalorije", "Unesite broj kalorija.");
            var plan = NapraviPlan(10, TestHelper.KorisnikId);

            var rezultat = await kontroler.Create(plan) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreSame(plan, rezultat.Model);
            Assert.AreEqual(ocekivaniBroj, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
            Assert.AreEqual(2, baza.PlanIshraneTreninga.Count());
        }

        // ---------- Edit (GET) ----------

        [TestMethod]
        public async Task Edit_Get_IdNull_VracaNotFound()
        {
            var rezultat = await kontroler.Edit(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Get_NepostojeciPlan_VracaNotFound()
        {
            var rezultat = await kontroler.Edit(NepostojeciPlanId);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Get_Admin_VracaViewSaSvimKorisnicima()
        {
            var rezultat = await kontroler.Edit(PlanKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(5, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        [TestMethod]
        public async Task Edit_Get_TrenerSvogKorisnika_VracaViewSaSvojimKorisnicima()
        {
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.TrenerEmail, "Trener");

            var rezultat = await kontroler.Edit(PlanKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(2, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        [TestMethod]
        [DataRow(TestHelper.TrenerEmail)]
        [DataRow("nepoznat@optishape.ba")]
        public async Task Edit_Get_TrenerTudjegKorisnika_VracaForbid(string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, "Trener");

            var rezultat = await kontroler.Edit(PlanDrugogKorisnikaId);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        // ---------- Edit (POST) ----------

        [TestMethod]
        public async Task Edit_Post_IdSeNePoklapa_VracaNotFound()
        {
            var plan = NapraviPlan(PlanKorisnikaId, TestHelper.KorisnikId);

            var rezultat = await kontroler.Edit(PlanDrugogKorisnikaId, plan);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        [Priority(1)]
        public async Task Edit_Post_ValidanPlan_AzuriraKalorijeIPreusmjerava()
        {
            var plan = NapraviPlan(PlanKorisnikaId, TestHelper.KorisnikId);
            plan.Kalorije = 2500;

            var rezultat = await kontroler.Edit(PlanKorisnikaId, plan) as RedirectToActionResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.AreEqual(2500, baza.PlanIshraneTreninga.Single(p => p.IdPlana == PlanKorisnikaId).Kalorije);
        }

        [TestMethod]
        [Timeout(5000, CooperativeCancellation = true)]
        public async Task Edit_Post_PlanObrisanUMedjuvremenu_VracaNotFound()
        {
            var plan = NapraviPlan(NepostojeciPlanId, TestHelper.KorisnikId);

            var rezultat = await kontroler.Edit(NepostojeciPlanId, plan);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModelAdmin_VracaFormu()
        {
            kontroler.ModelState.AddModelError("Kalorije", "Unesite broj kalorija.");
            var plan = NapraviPlan(PlanKorisnikaId, TestHelper.KorisnikId);

            var rezultat = await kontroler.Edit(PlanKorisnikaId, plan) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreSame(plan, rezultat.Model);
            Assert.AreEqual(5, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModelTrenerSvogKorisnika_VracaFormuSaSvojimKorisnicima()
        {
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.TrenerEmail, "Trener");
            kontroler.ModelState.AddModelError("Kalorije", "Unesite broj kalorija.");
            var plan = NapraviPlan(PlanKorisnikaId, TestHelper.KorisnikId);

            var rezultat = await kontroler.Edit(PlanKorisnikaId, plan) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(2, ((SelectList)kontroler.ViewData["IdKorisnika"]!).Count());
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModelTrenerTudjegKorisnika_VracaForbid()
        {
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.TrenerEmail, "Trener");
            kontroler.ModelState.AddModelError("Kalorije", "Unesite broj kalorija.");
            var plan = NapraviPlan(PlanDrugogKorisnikaId, TestHelper.DrugiKorisnikId);

            var rezultat = await kontroler.Edit(PlanDrugogKorisnikaId, plan);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        // ---------- Delete ----------

        [TestMethod]
        public async Task Delete_Get_Trener_VracaForbid()
        {
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.TrenerEmail, "Trener");

            var rezultat = await kontroler.Delete(PlanKorisnikaId);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(NepostojeciPlanId)]
        public async Task Delete_Get_IdNullIliNepostojeci_VracaNotFound(int? id)
        {
            var rezultat = await kontroler.Delete(id);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Delete_Get_PostojeciPlan_VracaViewZaPotvrdu()
        {
            var rezultat = await kontroler.Delete(PlanKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(PlanKorisnikaId, ((PlanIshraneTreninga)rezultat.Model!).IdPlana);
        }

        [TestMethod]
        public async Task DeleteConfirmed_Trener_VracaForbidINeBrise()
        {
            TestHelper.PrijaviKorisnika(kontroler, TestHelper.TrenerEmail, "Trener");

            var rezultat = await kontroler.DeleteConfirmed(PlanKorisnikaId);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
            Assert.AreEqual(2, baza.PlanIshraneTreninga.Count());
        }

        [TestMethod]
        [Priority(1)]
        public async Task DeleteConfirmed_PostojeciPlan_BrisePlanIPostavljaPoruku()
        {
            var rezultat = await kontroler.DeleteConfirmed(PlanKorisnikaId);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual(1, baza.PlanIshraneTreninga.Count());
            Assert.AreEqual("Plan je uspješno obrisan.", kontroler.TempData["DeleteMessage"]);
        }

        [TestMethod]
        public async Task DeleteConfirmed_NepostojeciPlan_PreusmjeravaBezBrisanja()
        {
            var rezultat = await kontroler.DeleteConfirmed(NepostojeciPlanId);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            Assert.AreEqual(2, baza.PlanIshraneTreninga.Count());
        }

        // ---------- PieChart ----------

        [TestMethod]
        public async Task PieChart_NepostojeciPlan_VracaNotFound()
        {
            var rezultat = await kontroler.PieChart(NepostojeciPlanId);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        [DataRow("Administrator", TestHelper.AdminEmail)]
        [DataRow("Korisnik", TestHelper.KorisnikEmail)]
        [DataRow("Trener", TestHelper.TrenerEmail)]
        public async Task PieChart_DozvoljenPristup_VracaViewSaPlanom(string uloga, string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.PieChart(PlanKorisnikaId) as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(PlanKorisnikaId, ((PlanIshraneTreninga)rezultat.Model!).IdPlana);
        }

        [TestMethod]
        [DataRow("Korisnik", TestHelper.KorisnikEmail)]
        [DataRow("Trener", TestHelper.TrenerEmail)]
        public async Task PieChart_TudjiPlan_VracaForbid(string uloga, string email)
        {
            TestHelper.PrijaviKorisnika(kontroler, email, uloga);

            var rezultat = await kontroler.PieChart(PlanDrugogKorisnikaId);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }
    }
}
