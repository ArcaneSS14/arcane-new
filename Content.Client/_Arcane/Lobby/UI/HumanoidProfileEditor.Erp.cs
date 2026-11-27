// Arcane-Start
using Content.Arcane.Common.Preferences;
using Content.Shared.Preferences;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

/// <summary>
/// ERP preference selector in the character editor, right under the sex row.
/// Characters under <see cref="HumanoidCharacterProfile.ErpMinAge"/> are locked to <see cref="ErpPreference.No"/>.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    private OptionButton _erpPreference = default!;
    private bool _updatingErp;

    private void InitializeErp()
    {
        var row = new BoxContainer { HorizontalExpand = true };
        row.AddChild(new Label { Text = Loc.GetString("humanoid-profile-editor-erp-preference-label") });
        row.AddChild(new Control { HorizontalExpand = true });
        _erpPreference = new OptionButton { HorizontalAlignment = HAlignment.Right };
        foreach (var value in Enum.GetValues<ErpPreference>())
            _erpPreference.AddItem(Loc.GetString($"humanoid-profile-editor-erp-preference-{value.ToString().ToLowerInvariant()}"), (int) value);
        row.AddChild(_erpPreference);

        if (SexButton.Parent is { Parent: { } list } sexRow)
        {
            list.AddChild(row);
            row.SetPositionInParent(sexRow.GetPositionInParent() + 1);
        }

        _erpPreference.OnItemSelected += args =>
        {
            if (_updatingErp || Profile == null)
                return;

            _erpPreference.SelectId(args.Id);
            Profile = Profile.WithErpPreference((ErpPreference) args.Id);
            SetDirty();
        };

        AgeEdit.OnTextChanged += _ => UpdateErp();
        OnSetProfile += _ => UpdateErp();
    }

    private void UpdateErp()
    {
        if (Profile == null)
            return;

        _updatingErp = true;
        var adult = Profile.Age >= HumanoidCharacterProfile.ErpMinAge;
        if (!adult && Profile.ErpPreference != ErpPreference.No)
            Profile = Profile.WithErpPreference(ErpPreference.No);

        _erpPreference.Disabled = !adult;
        _erpPreference.ToolTip = adult ? null : Loc.GetString("humanoid-profile-editor-erp-preference-adults-only");
        _erpPreference.SelectId((int) Profile.ErpPreference);
        _updatingErp = false;
    }
}
// Arcane-End
