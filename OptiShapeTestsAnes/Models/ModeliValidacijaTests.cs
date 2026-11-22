using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using OptiShape.Models;
using OptiShapeTestsAnes.Helpers;

namespace OptiShapeTestsAnes.Models
{
    [TestClass]
    [TestCategory("Modeli")]
    public class ModeliValidacijaTests
    {
        private static List<ValidationResult> Validiraj(object model)
        {
            var greske = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), greske, validateAllProperties: true);
            return greske;
        }

        private static StatistikeNapretka ValidnaStatistika(double tezina, double bmi) => new StatistikeNapretka
        {
            IdZapisa = 1,
            Datum = new DateTime(2026, 11, 22),
            Tezina = tezina,
            Bmi = bmi,
            KalorijskiUnos = 2300,
            IdKorisnika = TestHelper.KorisnikId
        };

        [TestMethod]
        [DataRow(30.0, 0.0, true)]
        [DataRow(200.0, 100.0, true)]
        [DataRow(80.0, 24.69, true)]
        [DataRow(29.0, 24.0, false)]
        [DataRow(201.0, 24.0, false)]
        [DataRow(80.0, -1.0, false)]
        [DataRow(80.0, 101.0, false)]
        public void StatistikeNapretka_GranicneVrijednostiTezineIBmi_IspravnaValidacija(double tezina, double bmi, bool ocekivanoValidno)
        {
            var statistika = ValidnaStatistika(tezina, bmi);

            var greske = Validiraj(statistika);

            Assert.AreEqual(ocekivanoValidno, greske.Count == 0);
        }

        /// <summary>
        /// [Range(30, 200)] je zadan cijelim brojevima, pa se double vrijednost prije poređenja
        /// zaokružuje na int. Zato 29.9 kg i 200.4 kg prolaze validaciju. Test dokumentuje to ponašanje
        /// (greška u modelu: trebalo bi [Range(30.0, 200.0)]).
        /// </summary>
        [TestMethod]
        [TestCategory("PronadjenaGreska")]
        [DataRow(29.9)]
        [DataRow(200.4)]
        public void StatistikeNapretka_TezinaMaloIzvanOpsega_ProlaziZbogCjelobrojnihGranica(double tezina)
        {
            var statistika = ValidnaStatistika(tezina, 24);

            var greske = Validiraj(statistika);

            Assert.AreEqual(0, greske.Count);
        }

        [TestMethod]
        public void StatistikeNapretka_TezinaIzvanOpsega_VracaPorukuOGresci()
        {
            var statistika = ValidnaStatistika(250, 24);

            var greske = Validiraj(statistika);

            Assert.AreEqual("Težina mora biti između 30 i 200 kg.", greske.Single().ErrorMessage);
        }

        [TestMethod]
        public void StatistikeNapretka_PostavljenKorisnik_CuvaVezuNaKorisnika()
        {
            var korisnik = TestHelper.NapraviKorisnika(TestHelper.KorisnikId, TestHelper.KorisnikEmail);

            var statistika = new StatistikeNapretka { Korisnik = korisnik };

            Assert.AreSame(korisnik, statistika.Korisnik);
        }

        [TestMethod]
        public void PlanIshraneTreninga_SvaPoljaPopunjena_JeValidan()
        {
            var plan = new PlanIshraneTreninga
            {
                IdPlana = 1,
                DatumKreiranja = new DateTime(2026, 11, 22),
                Kalorije = 2000,
                Protein = 150,
                Ugljikohidrati = 200,
                Masti = 60,
                IdKorisnika = TestHelper.KorisnikId,
                Korisnik = TestHelper.NapraviKorisnika(TestHelper.KorisnikId, TestHelper.KorisnikEmail)
            };

            var greske = Validiraj(plan);

            Assert.AreEqual(0, greske.Count);
            Assert.AreEqual(TestHelper.KorisnikId, plan.Korisnik.IdKorisnika);
        }
    }
}
