
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OptiShape.Areas.Identity.Pages.Account;
using OptiShape.Data;
using OptiShape.Models;

namespace UnitTestAzra;

[TestClass]
public class RegisterPokrivenostTests
{
    private static (
        RegisterModel Page,
        Mock<UserManager<ApplicationUser>> Manager,
        Mock<SignInManager<ApplicationUser>> SignIn,
        Mock<IEmailSender> Email) Kreiraj(
            ApplicationDbContext db, bool potvrda = false)
    {
        var store = new Mock<IUserEmailStore<ApplicationUser>>();

        store.Setup(x => x.SetUserNameAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        store.Setup(x => x.SetEmailAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, string, CancellationToken>(
                (user, email, token) => user.Email = email)
            .Returns(Task.CompletedTask);

        var manager = TestPomocneKlase.UserManager(store.Object);

        manager.SetupGet(x => x.SupportsUserEmail)
            .Returns(true);

        manager.Object.Options.SignIn.RequireConfirmedAccount =
            potvrda;

        manager.Setup(x => x.CreateAsync(
                It.IsAny<ApplicationUser>(), "Lozinka123!"))
            .ReturnsAsync(IdentityResult.Success);

        manager.Setup(x => x.AddToRoleAsync(
                It.IsAny<ApplicationUser>(), "Korisnik"))
            .ReturnsAsync(IdentityResult.Success);

        manager.Setup(x => x.GetUserIdAsync(
                It.IsAny<ApplicationUser>()))
            .ReturnsAsync("test-user-id");

        manager.Setup(x => x.GenerateEmailConfirmationTokenAsync(
                It.IsAny<ApplicationUser>()))
            .ReturnsAsync("test-token");

        var signIn = TestPomocneKlase.SignInManager(
            manager.Object);

        signIn.Setup(x => x.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(Array.Empty<AuthenticationScheme>());

        signIn.Setup(x => x.SignInAsync(
                It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        var emailSender = new Mock<IEmailSender>();

        emailSender.Setup(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("localhost");

        var actionContext = new ActionContext(
            http, new RouteData(), new ActionDescriptor());

        var url = new Mock<IUrlHelper>();

        url.SetupGet(x => x.ActionContext)
            .Returns(actionContext);

        url.Setup(x => x.Content("~/")).Returns("/");

        url.Setup(x => x.RouteUrl(
                It.IsAny<UrlRouteContext>()))
            .Returns("https://localhost/Account/ConfirmEmail");

        var page = new RegisterModel(
            manager.Object,
            store.Object,
            signIn.Object,
            NullLogger<RegisterModel>.Instance,
            emailSender.Object,
            db)
        {
            PageContext = new PageContext
            {
                HttpContext = http
            },
            Url = url.Object,
            Input = new RegisterModel.InputModel
            {
                Ime = "Ana",
                Prezime = "Test",
                Email = "ana@test.com",
                Password = "Lozinka123!",
                ConfirmPassword = "Lozinka123!",
                BrojTelefona = "061123456",
                DatumRodjenja = new DateTime(2002, 1, 1),
                Visina = 170,
                Tezina = 65,
                Spol = Spol.MUSKO,
                Cilj = Cilj.MRSANJE,
                StudentskiStatus = true
            }
        };

        return (page, manager, signIn, emailSender);
    }

    [TestMethod]
    public async Task OnGet_PostavljaReturnUrl()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var (page, manager, signIn, email) = Kreiraj(db);

            await page.OnGetAsync("/profil");

            Assert.AreEqual("/profil", page.ReturnUrl);
            Assert.AreEqual(0, page.ExternalLogins.Count);
        }
    }

    [TestMethod]
    public async Task OnPost_NeispravanModel_NeRegistrujeKorisnika()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var (page, manager, signIn, email) = Kreiraj(db);

            page.ModelState.AddModelError(
                "Input.Email", "Email nije ispravan");

            var result = await page.OnPostAsync();

            Assert.IsInstanceOfType(result, typeof(PageResult));

            manager.Verify(x => x.CreateAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>()), Times.Never);

            Assert.AreEqual(0, db.Korisnik.Count());
        }
    }

    [TestMethod]
    public async Task OnPost_NeuspjelaRegistracija_PrikazujeGresku()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var (page, manager, signIn, email) = Kreiraj(db);

            manager.Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(), "Lozinka123!"))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError
                    {
                        Description = "Email je vec zauzet"
                    }));

            var result = await page.OnPostAsync("/");

            Assert.IsInstanceOfType(result, typeof(PageResult));

            Assert.IsFalse(page.ModelState.IsValid);

            Assert.AreEqual(0, db.Korisnik.Count());

            email.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task OnPost_UspjesnaRegistracija_KreiraKorisnikaIPlan(
        bool zahtijevaPotvrdu)
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var (page, manager, signIn, email) =
                Kreiraj(db, zahtijevaPotvrdu);

            var result = await page.OnPostAsync("/");

            Assert.AreEqual(1, db.Korisnik.Count());
            Assert.AreEqual(1, db.PlanIshraneTreninga.Count());

            var korisnik = db.Korisnik.Single();

            Assert.AreEqual("Ana", korisnik.Ime);
            Assert.AreEqual("ana@test.com", korisnik.Email);
            Assert.IsTrue(korisnik.StudentskiStatus);

            manager.Verify(x => x.AddToRoleAsync(
                It.IsAny<ApplicationUser>(), "Korisnik"),
                Times.Once);

            email.Verify(x => x.SendEmailAsync(
                "ana@test.com",
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);

            if (zahtijevaPotvrdu)
            {
                Assert.IsInstanceOfType(
                    result, typeof(RedirectToPageResult));

                Assert.AreEqual(
                    "RegisterConfirmation",
                    ((RedirectToPageResult)result).PageName);
            }
            else
            {
                Assert.IsInstanceOfType(
                    result, typeof(LocalRedirectResult));

                Assert.AreEqual(
                    "/Home/Dashboard",
                    ((LocalRedirectResult)result).Url);

                signIn.Verify(x => x.SignInAsync(
                    It.IsAny<ApplicationUser>(), false, null),
                    Times.Once);
            }
        }
    }

    [TestMethod]
    public void Konstruktor_BezEmailStore_BacaIzuzetak()
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var manager = TestPomocneKlase.UserManager(store.Object);

            manager.SetupGet(x => x.SupportsUserEmail)
                .Returns(false);

            var signIn = TestPomocneKlase.SignInManager(
                manager.Object);

            Assert.Throws<NotSupportedException>(() =>
            {
                _ = new RegisterModel(
                    manager.Object,
                    store.Object,
                    signIn.Object,
                    NullLogger<RegisterModel>.Instance,
                    Mock.Of<IEmailSender>(),
                    db);
            });
        }
    }
}
