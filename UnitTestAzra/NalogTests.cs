using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OptiShape.Areas.Identity.Pages.Account;
using OptiShape.Models;
using ProfilModel = OptiShape.Areas.Identity.Pages.Account.Manage.IndexModel;

namespace UnitTestAzra;

[TestClass]
public class LoginMockTests
{
    [DataTestMethod]
    [DataRow("uspjeh")]
    [DataRow("zakljucan")]
    [DataRow("pogresna")]
    public async Task Login_SignInManagerMock_RezultatPrijave(
        string slucaj)
    {
        var manager = TestPomocneKlase.UserManager();
        var signIn = TestPomocneKlase.SignInManager(manager.Object);

        signIn.Setup(x => x.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(
                (IEnumerable<AuthenticationScheme>)
                Array.Empty<AuthenticationScheme>());

        var rezultatPrijave = slucaj switch
        {
            "uspjeh" => SignInResult.Success,
            "zakljucan" => SignInResult.LockedOut,
            _ => SignInResult.Failed
        };

        signIn.Setup(x => x.PasswordSignInAsync(
                "ana@test.com", "lozinka", false, false))
            .ReturnsAsync(rezultatPrijave);

        var stranica = new LoginModel(
            signIn.Object,
            NullLogger<LoginModel>.Instance)
        {
            Input = new LoginModel.InputModel
            {
                Email = "ana@test.com",
                Password = "lozinka",
                RememberMe = false
            },
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var rezultat = await stranica.OnPostAsync("/");

        if (slucaj == "uspjeh")
        {
            Assert.IsInstanceOfType(
                rezultat, typeof(RedirectToActionResult));

            var redirekcija = (RedirectToActionResult)rezultat;

            Assert.AreEqual("Dashboard", redirekcija.ActionName);
        }
        else if (slucaj == "zakljucan")
        {
            Assert.IsInstanceOfType(
                rezultat, typeof(RedirectToPageResult));

            Assert.AreEqual(
                "./Lockout",
                ((RedirectToPageResult)rezultat).PageName);
        }
        else
        {
            Assert.IsInstanceOfType(
                rezultat, typeof(PageResult));

            Assert.IsFalse(stranica.ModelState.IsValid);
        }

        signIn.Verify(
            x => x.PasswordSignInAsync(
                "ana@test.com", "lozinka", false, false),
            Times.Once);
    }
}

[TestClass]
public class RegisterValidacijaTests
{
    private static RegisterModel.InputModel NoviUnos()
    {
        return new RegisterModel.InputModel
        {
            Ime = "Ana",
            Prezime = "Test",
            Email = "ana@test.com",
            Password = "Lozinka123!",
            ConfirmPassword = "Lozinka123!",
            BrojTelefona = "061123456",
            DatumRodjenja = new DateTime(2002, 1, 1),
            Visina = 170,
            Tezina = 60,
            Spol = Spol.MUSKO,
            Cilj = Cilj.MRSANJE
        };
    }

    private static bool ImaGresku(
        RegisterModel.InputModel unos, string polje)
    {
        var greske = new List<ValidationResult>();

        Validator.TryValidateObject(
            unos,
            new ValidationContext(unos),
            greske,
            true);

        return greske.Any(
            x => x.MemberNames.Contains(polje));
    }

    [TestMethod]
    public void Register_IspravanUnos_ProlaziValidaciju()
    {
        var unos = NoviUnos();
        var greske = new List<ValidationResult>();

        Assert.IsTrue(
            Validator.TryValidateObject(
                unos,
                new ValidationContext(unos),
                greske,
                true));
    }

    [TestMethod]
    public void Register_NeispravanEmail_VracaGresku()
    {
        var unos = NoviUnos();
        unos.Email = "pogresan-email";

        Assert.IsTrue(
            ImaGresku(unos, nameof(unos.Email)));
    }

    [TestMethod]
    public void Register_RazliciteLozinke_VracajuGresku()
    {
        var unos = NoviUnos();
        unos.ConfirmPassword = "DrugaLozinka123!";

        Assert.IsTrue(
            ImaGresku(unos, nameof(unos.ConfirmPassword)));
    }

    [DataTestMethod]
    [DataRow(99.0, true)]
    [DataRow(100.0, false)]
    [DataRow(250.0, false)]
    [DataRow(251.0, true)]
    public void Register_Visina_ProvjeraGranica(
        double visina, bool greska)
    {
        var unos = NoviUnos();
        unos.Visina = visina;

        Assert.AreEqual(
            greska,
            ImaGresku(unos, nameof(unos.Visina)));
    }
}

[TestClass]
public class ProfilTests
{
    [TestMethod]
    public async Task Profil_NepostojeciKorisnik_VracaNotFound()
    {
        var manager = TestPomocneKlase.UserManager();

        manager.Setup(
            x => x.GetUserAsync(
                It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var signIn = TestPomocneKlase.SignInManager(manager.Object);

        var stranica = new ProfilModel(
            manager.Object,
            signIn.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var rezultat = await stranica.OnGetAsync();

        Assert.IsInstanceOfType(
            rezultat, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public async Task Profil_NeuspjesnaPromjenaTelefona_VracaPoruku()
    {
        var manager = TestPomocneKlase.UserManager();

        var user = new ApplicationUser
        {
            Email = "ana@test.com"
        };

        manager.Setup(
            x => x.GetUserAsync(
                It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        manager.Setup(
            x => x.GetPhoneNumberAsync(user))
            .ReturnsAsync("061111111");

        manager.Setup(
            x => x.SetPhoneNumberAsync(user, "062222222"))
            .ReturnsAsync(
                IdentityResult.Failed(
                    new IdentityError
                    {
                        Description = "Greska"
                    }));

        var signIn = TestPomocneKlase.SignInManager(manager.Object);

        var stranica = new ProfilModel(
            manager.Object,
            signIn.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            Input = new ProfilModel.InputModel
            {
                PhoneNumber = "062222222"
            }
        };

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType(
            rezultat, typeof(RedirectToPageResult));

        StringAssert.Contains(
            stranica.StatusMessage,
            "Neočekivana greška");

        manager.Verify(
            x => x.SetPhoneNumberAsync(user, "062222222"),
            Times.Once);
    }
}
