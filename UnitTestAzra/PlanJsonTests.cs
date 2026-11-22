
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OptiShape.Areas.Identity.Pages.Account;
using OptiShape.Models;

namespace UnitTestAzra;

public class PlanSlucaj
{
    public string Spol { get; set; } = "";
    public string Cilj { get; set; } = "";
    public double Tezina { get; set; }
    public double Visina { get; set; }
    public int Godine { get; set; }
    public int Kalorije { get; set; }
    public int Protein { get; set; }
    public int Ugljikohidrati { get; set; }
    public int Masti { get; set; }
}

[TestClass]
public class PlanJsonTests
{
    public static IEnumerable<object[]> TestSlucajevi()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "plan-cases.json"));

        var json = File.ReadAllText(path);

        var cases = JsonSerializer.Deserialize<List<PlanSlucaj>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

        return cases.Select(x => new object[] { x });
    }

    [DataTestMethod]
    [DynamicData(nameof(TestSlucajevi),
        DynamicDataSourceType.Method)]
    public async Task Register_KalorijeIMakroi_IzJsona(
        PlanSlucaj ulaz)
    {
        var (db, connection) = TestPomocneKlase.NovaBaza();

        using (connection)
        using (db)
        {
            var korisnik = TestPomocneKlase.DodajKorisnika(
                db, true, "json-test@test.com");

            var store = new Mock<IUserEmailStore<ApplicationUser>>();

            var manager = TestPomocneKlase.UserManager(store.Object);
            manager.CallBase = true;

            var signIn = TestPomocneKlase.SignInManager(
                manager.Object);

            var register = new RegisterModel(
                manager.Object,
                store.Object,
                signIn.Object,
                NullLogger<RegisterModel>.Instance,
                Mock.Of<IEmailSender>(),
                db);

            Spol spol = ulaz.Spol == "musko"
                ? Spol.MUSKO
                : Enum.GetValues<Spol>()
                    .First(x => x != Spol.MUSKO);

            Cilj cilj = ulaz.Cilj == "mrsanje"
                ? Cilj.MRSANJE
                : Cilj.NABIJANJEMISICNEMASE;

            var user = new ApplicationUser
            {
                Spol = spol,
                Cilj = cilj,
                Tezina = ulaz.Tezina,
                Visina = ulaz.Visina,
                DatumRodjenja = new DateTime(
                    DateTime.Today.Year - ulaz.Godine, 1, 1)
            };

            var metoda = typeof(RegisterModel).GetMethod(
                "CreateInitialPlan",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            await (Task)metoda.Invoke(
                register,
                new object[] { user, korisnik })!;

            var plan = await db.PlanIshraneTreninga.SingleAsync(
                x => x.IdKorisnika == korisnik.IdKorisnika);

            Assert.AreEqual(
                ulaz.Kalorije, plan.Kalorije);

            Assert.AreEqual(
                ulaz.Protein, plan.Protein);

            Assert.AreEqual(
                ulaz.Ugljikohidrati, plan.Ugljikohidrati);

            Assert.AreEqual(
                ulaz.Masti, plan.Masti);
        }
    }
}
