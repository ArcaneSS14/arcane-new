using Content.Arcane.Shared.ModSuits;
using JetBrains.Annotations;

namespace Content.Arcane.Client.ModSuits;

[UsedImplicitly]
public sealed class ModSuitBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    protected override void Open()
    {
        base.Open();
        var window = this.CreateWindow<ModSuitWindow>();
        window.SetSuit(Owner);
        window.OnActivate += () => SendMessage(new ModSuitActivateMessage());
        window.OnToggleAll += () => SendMessage(new ModSuitToggleAllPartsMessage());
        window.OnTogglePart += slot => SendMessage(new ModSuitTogglePartMessage(slot));
        window.OnUseModule += module => SendMessage(new ModSuitModuleToggleMessage(module));
        window.OnPinModule += module => SendMessage(new ModSuitModulePinMessage(module));
        window.OnRemoveModule += module => SendMessage(new ModSuitModuleRemoveMessage(module));
    }
}
