using Content.Arcane.Server.AlmanacBlade;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P26AlmanacCalendarTest
{
    [Test]
    public void SpecialDatesUseTheServerCalendar()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AlmanacCalendar.IsLeapDay(new DateOnly(2028, 2, 29)), Is.True);
            Assert.That(AlmanacCalendar.IsLeapDay(new DateOnly(2028, 3, 1)), Is.False);
            Assert.That(AlmanacCalendar.IsEclipse(new DateOnly(2026, 8, 12)), Is.True);
            Assert.That(AlmanacCalendar.IsEclipse(new DateOnly(2026, 8, 13)), Is.False);
            Assert.That(AlmanacCalendar.IsChristmas(new DateOnly(2027, 12, 25)), Is.True);
            Assert.That(AlmanacCalendar.IsWednesdayBeforeChristmas(new DateOnly(2027, 12, 22)), Is.True);
            Assert.That(AlmanacCalendar.IsSaturdayAfterEaster(new DateOnly(2027, 3, 27)), Is.False);
            Assert.That(AlmanacCalendar.IsSaturdayAfterEaster(new DateOnly(2027, 4, 3)), Is.True);
            Assert.That(AlmanacCalendar.IsAprilFoolsOver(new DateTime(2027, 4, 1, 11, 59, 59)), Is.False);
            Assert.That(AlmanacCalendar.IsAprilFoolsOver(new DateTime(2027, 4, 1, 12, 0, 0)), Is.True);
        });
    }

    [Test]
    public void CargoRequiresSundayFullMoonPopulationAndPreset()
    {
        var newMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
        var fullMoon = newMoon.AddDays(AlmanacCalendar.SynodicMonth / 2);
        var sunday = new DateTime(2000, 1, 23);

        Assert.Multiple(() =>
        {
            Assert.That(AlmanacBladeSystem.CanOrder(sunday, fullMoon, 29, "Extended"), Is.True);
            Assert.That(AlmanacBladeSystem.CanOrder(sunday, fullMoon, 29, "Greenshift"), Is.True);
            Assert.That(AlmanacBladeSystem.CanOrder(sunday, fullMoon, 30, "Extended"), Is.False);
            Assert.That(AlmanacBladeSystem.CanOrder(sunday, fullMoon, 29, "Traitor"), Is.False);
            Assert.That(AlmanacBladeSystem.CanOrder(sunday.AddDays(1), fullMoon, 29, "Extended"), Is.False);
            Assert.That(AlmanacBladeSystem.CanOrder(sunday, newMoon, 29, "Extended"), Is.False);
        });
    }
}
