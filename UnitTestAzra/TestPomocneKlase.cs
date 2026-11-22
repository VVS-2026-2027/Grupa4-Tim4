
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OptiShape.Data;
using OptiShape.Models;

namespace UnitTestAzra;

internal static class TestPomocneKlase
{
    public static (ApplicationDbContext Db, SqliteConnection Connection) NovaBaza()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();

        return (db, connection);
    }

    public static Korisnik DodajKorisnika(
        ApplicationDbContext db, bool student, string email)
    {
        var korisnik = new Korisnik
        {
            Ime = "Ana",
            Prezime = "Test",
            Email = email,
            Sifra = "Lozinka123!",
            BrojTelefona = "061123456",
            DatumRodjenja = new DateTime(2001, 1, 1),
            Visina = 170,
            Tezina = 65,
            Spol = Spol.MUSKO,
            Cilj = Cilj.MRSANJE,
            StudentskiStatus = student
        };

        db.Korisnik.Add(korisnik);
        db.SaveChanges();

        return korisnik;
    }

    public static Mock<UserManager<ApplicationUser>> UserManager(
        IUserStore<ApplicationUser>? store = null)
    {
        return new Mock<UserManager<ApplicationUser>>(
            store ?? Mock.Of<IUserStore<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    public static Mock<SignInManager<ApplicationUser>> SignInManager(
        UserManager<ApplicationUser> userManager)
    {
        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            new HttpContextAccessor(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
    }
}

internal sealed class PrazniTempDataProvider :
    Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>();
    }

    public void SaveTempData(
        HttpContext context,
        IDictionary<string, object> values)
    {
    }
}
