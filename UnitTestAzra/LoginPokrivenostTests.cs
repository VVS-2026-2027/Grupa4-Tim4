
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OptiShape.Areas.Identity.Pages.Account;
using OptiShape.Models;

namespace UnitTestAzra;

[TestClass]
public class LoginPokrivenostTests
{
    private static (LoginModel, Mock<SignInManager<ApplicationUser>>,
        Mock<IAuthenticationService>) Kreiraj()
    {
        var manager = TestPomocneKlase.UserManager();
        var signIn = TestPomocneKlase.SignInManager(manager.Object);

        signIn.Setup(x => x.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(Array.Empty<AuthenticationScheme>());

        var auth = new Mock<IAuthenticationService>();

        auth.Setup(x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                IdentityConstants.ExternalScheme,
                It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var http = new DefaultHttpContext();
        http.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(auth.Object)
            .BuildServiceProvider();

        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Content("~/")).Returns("/");

        var page = new LoginModel(
            signIn.Object,
            NullLogger<LoginModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = http },
            Url = url.Object,
            Input = new LoginModel.InputModel
            {
                Email = "ana@test.com",
                Password = "lozinka",
                RememberMe = false
            }
        };

        return (page, signIn, auth);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task OnGet_PostavljaPovratniUrlIObradjujeGresku(
        bool imaGresku)
    {
        var (page, signIn, auth) = Kreiraj();

        if (imaGresku)
            page.ErrorMessage = "Greska pri prijavi";

        await page.OnGetAsync();

        Assert.AreEqual("/", page.ReturnUrl);
        Assert.AreEqual(0, page.ExternalLogins.Count);

        Assert.AreEqual(
            imaGresku ? 1 : 0,
            page.ModelState.ErrorCount);

        auth.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(),
            IdentityConstants.ExternalScheme,
            It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [TestMethod]
    public async Task OnPost_UspjehSaPovratnimUrlom_Redirekcija()
    {
        var (page, signIn, auth) = Kreiraj();

        signIn.Setup(x => x.PasswordSignInAsync(
                "ana@test.com", "lozinka", false, false))
            .ReturnsAsync(
                Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await page.OnPostAsync("/moj-profil");

        Assert.IsInstanceOfType(
            result, typeof(LocalRedirectResult));

        Assert.AreEqual(
            "/moj-profil",
            ((LocalRedirectResult)result).Url);
    }

    [TestMethod]
    public async Task OnPost_DvofaktorskaPrijava_Preusmjerava()
    {
        var (page, signIn, auth) = Kreiraj();

        signIn.Setup(x => x.PasswordSignInAsync(
                "ana@test.com", "lozinka", false, false))
            .ReturnsAsync(
                Microsoft.AspNetCore.Identity.SignInResult
                    .TwoFactorRequired);

        var result = await page.OnPostAsync("/profil");

        Assert.IsInstanceOfType(
            result, typeof(RedirectToPageResult));

        Assert.AreEqual(
            "./LoginWith2fa",
            ((RedirectToPageResult)result).PageName);
    }

    [TestMethod]
    public async Task OnPost_NeispravanModel_NePozivaPrijavu()
    {
        var (page, signIn, auth) = Kreiraj();

        page.ModelState.AddModelError(
            "Input.Email", "Email je obavezan");

        var result = await page.OnPostAsync("/");

        Assert.IsInstanceOfType(result, typeof(PageResult));

        signIn.Verify(x => x.PasswordSignInAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task OnPost_BezPovratnogUrla_PrikazujeGresku()
    {
        var (page, signIn, auth) = Kreiraj();

        signIn.Setup(x => x.PasswordSignInAsync(
                "ana@test.com", "lozinka", false, false))
            .ReturnsAsync(
                Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await page.OnPostAsync();

        Assert.IsInstanceOfType(result, typeof(PageResult));
        Assert.IsFalse(page.ModelState.IsValid);
    }
}
