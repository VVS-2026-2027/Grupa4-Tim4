using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiShape.Data;
using OptiShape.Models;

namespace OptiShapeTestsAnes.Helpers
{
    /// <summary>
    /// Zajednički pomoćni kod za testove: InMemory baza (zamjena za pravu bazu),
    /// mock UserManager-a, prijavljeni korisnik i testni podaci.
    /// </summary>
    public static class TestHelper
    {
        public const string AdminEmail = "admin@optishape.ba";
        public const string TrenerEmail = "trener@optishape.ba";
        public const string DrugiTrenerEmail = "trener2@optishape.ba";
        public const string KorisnikEmail = "korisnik@optishape.ba";
        public const string DrugiKorisnikEmail = "korisnik2@optishape.ba";

        public const int TrenerId = 1;
        public const int DrugiTrenerId = 2;
        public const int KorisnikId = 3;
        public const int DrugiKorisnikId = 4;
        public const int KorisnikBezVisineId = 5;

        // Svaki test dobija svoju, praznu bazu (testovi su međusobno neovisni)
        public static ApplicationDbContext NovaBaza()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        public static Korisnik NapraviKorisnika(int id, string email, double visina = 180, int? idTrenera = null)
        {
            return new Korisnik
            {
                IdKorisnika = id,
                Ime = "Ime" + id,
                Prezime = "Prezime" + id,
                Email = email,
                Sifra = "sifra123",
                DatumRodjenja = new DateTime(2000, 1, 1),
                Visina = visina,
                Tezina = 80,
                Spol = Spol.MUSKO,
                Cilj = Cilj.MRSANJE,
                StudentskiStatus = false,
                BrojTelefona = "061111222",
                IdTrenera = idTrenera
            };
        }

        /// <summary>
        /// Dva trenera, korisnik trenera 1, korisnik trenera 2 i korisnik bez visine (visina 0).
        /// </summary>
        public static void DodajKorisnike(ApplicationDbContext baza)
        {
            baza.Korisnik.AddRange(
                NapraviKorisnika(TrenerId, TrenerEmail),
                NapraviKorisnika(DrugiTrenerId, DrugiTrenerEmail),
                NapraviKorisnika(KorisnikId, KorisnikEmail, 180, TrenerId),
                NapraviKorisnika(DrugiKorisnikId, DrugiKorisnikEmail, 165, DrugiTrenerId),
                NapraviKorisnika(KorisnikBezVisineId, "bezvisine@optishape.ba", 0, TrenerId));
            baza.SaveChanges();
            baza.ChangeTracker.Clear();
        }

        /// <summary>
        /// Postavlja prijavljenog korisnika (ime = email, uloga) i TempData na kontroler.
        /// Uloga null znači korisnik bez uloge.
        /// </summary>
        public static void PrijaviKorisnika(Controller kontroler, string email, string? uloga)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, email) };
            if (uloga != null)
                claims.Add(new Claim(ClaimTypes.Role, uloga));

            var korisnik = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = korisnik }
            };
            kontroler.TempData = new TempDataDictionary(kontroler.HttpContext, Mock.Of<ITempDataProvider>());
        }

        public static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        public static string PutanjaPodataka(string fajl)
            => Path.Combine(AppContext.BaseDirectory, "TestData", fajl);
    }
}
