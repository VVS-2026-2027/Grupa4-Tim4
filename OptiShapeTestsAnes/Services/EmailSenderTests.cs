using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Moq;
using OptiShape.Services;

namespace OptiShapeTestsAnes.Services
{
    [TestClass]
    [TestCategory("EmailSender")]
    public class EmailSenderTests
    {
        private Mock<IOptions<EmailSenderOptions>> opcijeMock = null!;
        private EmailSenderOptions opcije = null!;

        [TestInitialize]
        public void Inicijalizacija()
        {
            // Lokalni server na portu na kojem niko ne sluša: nijedan pravi email se ne šalje
            opcije = new EmailSenderOptions
            {
                SmtpServer = "localhost",
                SmtpPort = 1,
                SenderEmail = "optishape@test.ba",
                SenderName = "OptiShape",
                Password = "testna-lozinka"
            };
            opcijeMock = new Mock<IOptions<EmailSenderOptions>>();
            opcijeMock.Setup(o => o.Value).Returns(opcije);
        }

        [TestMethod]
        [TestCategory("Mock")]
        [Priority(1)]
        public void Konstruktor_MockOpcije_CitaVrijednostTacnoJednom()
        {
            var sender = new EmailSender(opcijeMock.Object);

            Assert.IsNotNull(sender);
            opcijeMock.Verify(o => o.Value, Times.Once);
        }

        [TestMethod]
        public void EmailSenderOptions_NoviObjekat_ImaGmailPodrazumijevaneVrijednosti()
        {
            var podrazumijevane = new EmailSenderOptions();

            Assert.AreEqual("smtp.gmail.com", podrazumijevane.SmtpServer);
            Assert.AreEqual(587, podrazumijevane.SmtpPort);
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        [TestCategory("Mock")]
        public async Task SendEmailAsync_NevalidnaAdresaPrimaoca_BacaFormatException()
        {
            var sender = new EmailSender(opcijeMock.Object);

            await Assert.ThrowsExactlyAsync<FormatException>(
                () => sender.SendEmailAsync("ovo-nije-email", "Naslov", "<p>Poruka</p>"));
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        public async Task SendEmailAsync_PosiljalacNijePodesen_BacaArgumentNullException()
        {
            opcije.SenderEmail = null!;
            var sender = new EmailSender(opcijeMock.Object);

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                () => sender.SendEmailAsync("korisnik@test.ba", "Naslov", "<p>Poruka</p>"));
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        [Timeout(15000, CooperativeCancellation = true)]
        public async Task SendEmailAsync_SmtpServerNedostupan_BacaSmtpException()
        {
            var sender = new EmailSender(opcijeMock.Object);

            await Assert.ThrowsExactlyAsync<SmtpException>(
                () => sender.SendEmailAsync("korisnik@test.ba", "Naslov", "<p>Poruka</p>"));
        }
    }
}
