// Arcane-Start: the same job can appear in multiple departments
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Content.Client.Lobby.UI;
using Content.Client.Lobby.UI.Roles;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class AlternateTitleSelectorsTest : GameTest
{
    private static readonly ProtoId<JobPrototype> Quartermaster = "Quartermaster";

    [Test]
    public async Task ChangingOneDepartmentUpdatesTheOther()
    {
        await Client.WaitAssertion(() =>
        {
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var job = prototypes.Index(Quartermaster);
            Assert.That(job.AlternateTitles, Is.Not.Empty);
            var editor = (HumanoidProfileEditor) RuntimeHelpers.GetUninitializedObject(typeof(HumanoidProfileEditor));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(HumanoidProfileEditor).GetField("_prototypeManager", flags)!.SetValue(editor, prototypes);
            var first = new RequirementsSelector();
            var second = new RequirementsSelector();
            first.Setup([], job.LocalizedName, 200, null);
            second.Setup([], job.LocalizedName, 200, null);
            var setup = typeof(HumanoidProfileEditor).GetMethod("SetupJobAltTitle", flags)!;
            setup.Invoke(editor, [first, job]);
            setup.Invoke(editor, [second, job]);
            typeof(HumanoidProfileEditor).GetField("_jobPriorities", flags)!.SetValue(editor,
                new List<(string, RequirementsSelector)> { (job.ID, first), (job.ID, second) });
            var firstDropdown = first.Children.OfType<OptionButton>().Single();
            var secondDropdown = second.Children.OfType<OptionButton>().Single();
            var handler = (Action<OptionButton.ItemSelectedEventArgs>) typeof(OptionButton)
                .GetField("OnItemSelected", flags)!.GetValue(firstDropdown)!;
            handler(new OptionButton.ItemSelectedEventArgs(1, firstDropdown));
            Assert.That(firstDropdown.SelectedId, Is.EqualTo(1));
            Assert.That(secondDropdown.SelectedId, Is.EqualTo(1));
        });
    }
}
// Arcane-End
