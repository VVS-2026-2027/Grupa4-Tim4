# TASK 3 – TDD RAZVOJ FUNKCIONALNOSTI APLIKACIJE OPTISHAPE

## 1. Uvod

U okviru trećeg zadatka izvršen je razvoj dvije nove funkcionalnosti aplikacije OptiShape primjenom TDD
(Test-Driven Development) pristupa. TDD predstavlja metodologiju razvoja softvera u kojoj se prije
pisanja same poslovne logike definišu automatski jedinični testovi koji opisuju očekivano ponašanje
sistema, nakon čega slijedi implementacija koda potrebna da svi testovi uspješno prođu (ciklus RED –
GREEN – REFACTOR).

Za razvoj i testiranje korišteni su programski jezik C#, .NET 8, ASP.NET Core MVC i xUnit testni
framework.

## 2. Prva funkcionalnost – Provjera dostupnosti i radnog vremena termina

### 2.1. Specifikacija funkcionalnosti

Prva funkcionalnost omogućava provjeru da li se novi trening termin može zakazati prije njegovog upisa
u bazu podataka.

Sistem zadovoljava sljedeće zahtjeve:
- Termin se može zakazati samo unutar definisanog radnog vremena (od 08:00 do 22:00, pri čemu svaki
termin traje 1 sat).
- Termin se ne može zakazati ukoliko se vremenski preklapa sa već postojećim terminom na isti datum.
- Ako se metodi proslijedi `null` umjesto objekta `Termin`, potrebno je baciti izuzetak
`ArgumentNullException`.
- Ukoliko termin nije dostupan ili je izvan radnog vremena, korisniku se prikazuje odgovarajuća
validacijska poruka.

### 2.2. TDD implementacija

- **RED faza:** Kreiran je testni projekat `OptiShape.Tests` i testna klasa
`TerminAvailabilityServiceTests.cs` sa četiri jedinična testa:
1. `CanSchedule_WhenSlotIsAvailableWithinWorkingHours_ReturnsTrue` – provjerava da li sistem
dozvoljava zakazivanje slobodnog termina unutar radnog vremena.
2. `CanSchedule_WhenOutsideWorkingHours_ReturnsFalse` – provjerava da li sistem odbija termin čiji
završetak prelazi kraj radnog vremena (npr. početak u 21:30).
3. `CanSchedule_WhenSlotOverlapsWithExistingTermin_ReturnsFalse` – provjerava da li sistem odbija
termin koji se preklapa sa postojećim terminom na isti datum.
4. `CanSchedule_WhenTerminIsNull_ThrowsArgumentNullException` – provjerava pravilno bacanje
izuzetka kada je proslijeđeni objekat `null`.

Prilikom prvog pokretanja testova metoda `CanSchedule` sadržavala je `NotImplementedException`,
zbog čega su sva 4 testa pala.

- **GREEN faza:** Implementirana je klasa `TerminAvailabilityService.cs` u direktoriju `Services` sa
statičkom metodom `CanSchedule`, koja provjerava granice radnog vremena (`08:00` – `22:00`) i
preklapanje intervala postojećih termina na isti datum. Ponovnim pokretanjem testova sva 4 testa su
uspješno prošla.

### 2.3. Integracija u aplikaciju

Funkcionalnost je integrisana u POST metodu `Create` unutar `TerminController.cs`. Prije spajanja
datuma i vremena i upisa novog termina u bazu, dohvaćaju se postojeći termini za odabrani datum i
poziva se `TerminAvailabilityService.CanSchedule`. U slučaju da termin nije validan, dodaje se greška u
`ModelState` sa porukom: *„Odabrani termin nije dostupan ili je izvan radnog vremena (08:00 -
22:00).“*

## 3. Druga funkcionalnost – Obračun studentskog popusta pri plaćanju

### 3.1. Specifikacija funkcionalnosti

Druga funkcionalnost automatizuje provjeru prava na studentski popust i obračun umanjenog iznosa
plaćanja.

Sistem zadovoljava sljedeće zahtjeve:
- Ako korisnik ima aktivan studentski status (`StudentskiStatus == true`) i popust još nije primijenjen
(`PopustPrimijenjen == false`), iznos plaćanja se umanjuje za 20%, a oznaka `PopustPrimijenjen`
postavlja se na `true`.
- Ako korisnik nema studentski status (`StudentskiStatus == false`), iznos plaćanja ostaje nepromijenjen i
metoda vraća `false`.
- Ako je popust već ranije primijenjen (`PopustPrimijenjen == true`), sistem sprječava ponovno
umanjenje iznosa i vraća `false`.
- Ako je objekat `Placanje` ili `Korisnik` jednak `null`, baca se izuzetak `ArgumentNullException`.

### 3.2. TDD implementacija

- **RED faza:** Kreirana je testna klasa `PaymentDiscountServiceTests.cs` sa pet jediničnih testova:
1. `ApplyStudentDiscount_WhenUserIsStudentAndDiscountNotApplied_Applies20PercentDiscount` –
potvrđuje umanjenje iznosa za 20% i postavljanje zastavice `PopustPrimijenjen`.
2. `ApplyStudentDiscount_WhenUserIsNotStudent_DoesNotApplyDiscount` – potvrđuje da korisnici
bez studentskog statusa plaćaju puni iznos.
3. `ApplyStudentDiscount_WhenDiscountAlreadyApplied_DoesNotApplyDiscountAgain` – potvrđuje
zaštitu od dvostrukog obračuna popusta.
4. `ApplyStudentDiscount_WhenPlacanjeIsNull_ThrowsArgumentNullException` – provjerava bacanje
izuzetka kada je objekat `Placanje` `null`.
5. `ApplyStudentDiscount_WhenKorisnikIsNull_ThrowsArgumentNullException` – provjerava bacanje
izuzetka kada je objekat `Korisnik` `null`.

U početnoj fazi metoda `ApplyStudentDiscount` bacala je `NotImplementedException`, pa je svih 5
novih testova palo (ukupno 5 neuspješnih i 4 uspješna testa).

- **GREEN faza:** Implementirana je klasa `PaymentDiscountService.cs` u direktoriju `Services` sa
metodom `ApplyStudentDiscount`. Nakon implementacije svih 9 testova u rješenju je uspješno prošlo.

### 3.3. Integracija u aplikaciju

Funkcionalnost je povezana sa POST metodom `Create` unutar `PlacanjeController.cs`. Prije čuvanja
novog plaćanja u bazu podataka pronalazi se odabrani korisnik i poziva se
`PaymentDiscountService.ApplyStudentDiscount(placanje, korisnik)` kako bi se automatski obračunao
popust ukoliko korisnik ima studentski status.

## 4. Rezultati testiranja

Testiranje je izvršeno naredbom `dotnet test OptiShape.sln` korištenjem xUnit testnog okvira.

| Testna klasa | Broj testova | Prošlo | Palo |
|---|---|---|---|
| `TerminAvailabilityServiceTests` | 4 | 4 | 0 |
| `PaymentDiscountServiceTests` | 5 | 5 | 0 |
| **Ukupno** | **9** | **9** | **0** |

## 5. Zaključak

Primjenom TDD metodologije u aplikaciji OptiShape uspješno su razvijene i integrisane dvije nove
funkcionalnosti: provjera dostupnosti i radnog vremena trening termina te automatski obračun
studentskog popusta pri plaćanju. Kroz jasno odvojene RED i GREEN faze u historiji Git repozitorija
potvrđeno je da su testovi definisani prije implementacije poslovne logike, čime je osigurana pouzdanost
koda i lakše buduće održavanje sistema.
