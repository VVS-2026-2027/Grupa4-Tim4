
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OptiShape.Data;
using OptiShape.Models;
using ProfilModel =
    OptiShape.Areas.Identity.Pages.Account.Manage.IndexModel;

namespace UnitTestAzra;

[TestClass]
public class ProfilPokrivenostTests
{
    private static (
        ProfilModel Page,
        Mock<UserManager<ApplicationUser>> Manager,
        Mock<SignInManager<ApplicationUser>> SignIn,
        ApplicationUser User) Kreiraj(ApplicationDbContext db)
    {
        var user = new ApplicationUser
        {
            Id = "korisnik-1",
            Email = "ana@test.com",
            Ime = "Ana",
            Prezime = "Test",
            DatumRodjenja = new DateTime(2002, 1, 1),
            Visina = 170,
            Tezina = 65,
            Spol = Spol.MUSKO,
            Cilj = Cilj.MRSANJE,
            StudentskiStatus = false
        };

        var manager = TestPomocneKlase.UserManager();

        manager.Setup(x => x.GetUserAsync(
                It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        manager.Setup(x => x.GetUserNameAsync(user))
            .ReturnsAsync("ana@test.com");

        manager.Setup(x => x.GetPhoneNumberAsync(user))
            .ReturnsAsync("061123456");

        manager.Setup(x => x.SetPhoneNumberAsync(
                user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        manager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var signIn = TestPomocneKlase.SignInManager(
            manager.Object);

        signIn.Setup(x => x.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var http = new DefaultHttpContext();
        http.RequestServices = new ServiceCollection()
            .AddSingleton<ApplicationDbContext>(db)
            .BuildServiceProvider();

        var page = new ProfilModel(
            manager.Object, signIn.Object)
        {
            PageContext = new PageContext { HttpContext = http },
            Input = new ProfilModel.InputModel
            {
                PhoneNumber = "062222222",
                Ime = "Amra",
                Prezime = "Nova",
                DatumRodjenja = new DateTime(2001, 5, 5),
                Visina = 175,
                Tezina = 70,
                Spol = Spol.MUSKO,
                Cilj = Cilj.MRSANJE,
                StudentskiStatus = true
            }
        };

        return (page, manager, signIn, user);
    }

    [TestMethod]
    public async Task OnGet_PopunjavaPodatkeKorisnika()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            TestPomocneKlase.DodajKorisnika(
                db, false, "ana@test.com");

            var (page, manager, signIn, user) = Kreiraj(db);

            var result = await page.OnGetAsync();

            Assert.IsInstanceOfType(
                result, typeof(PageResult));

            Assert.AreEqual("ana@test.com", page.Username);
            Assert.AreEqual("Ana", page.Input.Ime);
            Assert.AreEqual("061123456", page.Input.PhoneNumber);
            Assert.AreEqual(170.0, page.Input.Visina);
        }
    }

    [TestMethod]
    public async Task OnPost_NoviTelefon_AzuriraProfil()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            TestPomocneKlase.DodajKorisnika(
                db, false, "ana@test.com");

            var (page, manager, signIn, user) = Kreiraj(db);

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(
                result, typeof(RedirectToPageResult));

            var korisnik = db.Korisnik.Single();

            Assert.AreEqual("Amra", korisnik.Ime);
            Assert.AreEqual("Nova", korisnik.Prezime);
            Assert.AreEqual("062222222", korisnik.BrojTelefona);
            Assert.AreEqual(70.0, korisnik.Tezina);
            Assert.IsTrue(korisnik.StudentskiStatus);
            Assert.AreEqual("Amra", user.Ime);

            manager.Verify(x => x.SetPhoneNumberAsync(
                user, "062222222"), Times.Once);

            manager.Verify(x => x.UpdateAsync(user), Times.Once);

            signIn.Verify(x => x.RefreshSignInAsync(user),
                Times.Once);
        }
    }

    [TestMethod]
    public async Task OnPost_IstiTelefon_NePozivaPromjenuTelefona()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            TestPomocneKlase.DodajKorisnika(
                db, false, "ana@test.com");

            var (page, manager, signIn, user) = Kreiraj(db);

            page.Input.PhoneNumber = "061123456";

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(
                result, typeof(RedirectToPageResult));

            manager.Verify(x => x.SetPhoneNumberAsync(
                user, It.IsAny<string>()), Times.Never);

            signIn.Verify(x => x.RefreshSignInAsync(user),
                Times.Once);
        }
    }

    [TestMethod]
    public async Task OnPost_NeispravanModel_VracaPostojecePodatke()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            TestPomocneKlase.DodajKorisnika(
                db, false, "ana@test.com");

            var (page, manager, signIn, user) = Kreiraj(db);

            page.ModelState.AddModelError(
                "Input.Tezina", "Neispravna tezina");

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(
                result, typeof(PageResult));

            Assert.AreEqual("Ana", page.Input.Ime);
            Assert.AreEqual("061123456", page.Input.PhoneNumber);

            manager.Verify(x => x.UpdateAsync(
                It.IsAny<ApplicationUser>()), Times.Never);
        }
    }

    [TestMethod]
    public async Task OnPost_NemaZapisaUBazi_AzuriraIdentity()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var (page, manager, signIn, user) = Kreiraj(db);

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(
                result, typeof(RedirectToPageResult));

            Assert.AreEqual(0, db.Korisnik.Count());
            Assert.AreEqual("Amra", user.Ime);

            signIn.Verify(x => x.RefreshSignInAsync(user),
                Times.Once);
        }
    }

    [TestMethod]
    public async Task OnPost_NepostojeciIdentityKorisnik_NotFound()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var (page, manager, signIn, user) = Kreiraj(db);

            manager.Setup(x => x.GetUserAsync(
                    It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser?)null);

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(
                result, typeof(NotFoundObjectResult));

            manager.Verify(x => x.UpdateAsync(
                It.IsAny<ApplicationUser>()), Times.Never);
        }
    }
}
