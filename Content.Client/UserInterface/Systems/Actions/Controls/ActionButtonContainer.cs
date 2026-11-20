using System.Linq;
using Content.Client.Actions;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Actions.Controls;

[Virtual]
public partial class ActionButtonContainer : GridContainer
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IInputManager _input = default!;

    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionPressed;
    public event Action<GUIBoundKeyEventArgs, ActionButton>? ActionUnpressed;
    public event Action<ActionButton>? ActionFocusExited;

    public ActionButtonContainer()
    {
        IoCManager.InjectDependencies(this);
    }

    public ActionButton this[int index]
    {
        get => (ActionButton) GetChild(index);
    }

    public void SetActionData(ActionsSystem system, params EntityUid?[] actionTypes)
    {
        var uniqueCount = GetButtonCount(system, actionTypes); // Arcane-Edit
        var keys = ContentKeyFunctions.GetHotbarBoundKeys();

        for (var i = 0; i < uniqueCount; i++)
        {
            if (i >= ChildCount)
            {
                AddChild(MakeButton(i));
            }

            if (!actionTypes.TryGetValue(i, out var action))
                action = null;
            ((ActionButton) GetChild(i)).UpdateData(action, system);
        }

        for (var i = ChildCount - 1; i >= uniqueCount; i--)
        {
            RemoveChild(GetChild(i));
        }

        ActionButton MakeButton(int index)
        {
            var button = CreateButton(_entity); // Arcane-Edit

            if (!keys.TryGetValue(index, out var boundKey))
                return button;

            button.KeyBind = boundKey;
            if (_input.TryGetKeyBinding(boundKey, out var binding))
            {
                button.Label.Text = binding.GetKeyString();
            }

            return button;
        }
    }

    public void ClearActionData()
    {
        foreach (var button in Children)
        {
            ((ActionButton) button).ClearData();
        }
    }

    // Arcane-Start
    protected virtual ActionButton CreateButton(IEntityManager entities) => new(entities);

    protected virtual int GetButtonCount(ActionsSystem system, EntityUid?[] actions)
        => Math.Min(system.GetClientActions().Count(), actions.Length + 1);
    // Arcane-End

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);

        if (newChild is not ActionButton button)
            return;

        button.ActionPressed += ActionPressed;
        button.ActionUnpressed += ActionUnpressed;
        button.ActionFocusExited += ActionFocusExited;
    }

    protected override void ChildRemoved(Control newChild)
    {
        if (newChild is not ActionButton button)
            return;

        button.ActionPressed -= ActionPressed;
        button.ActionUnpressed -= ActionUnpressed;
        button.ActionFocusExited -= ActionFocusExited;
    }

    public bool TryGetButtonIndex(ActionButton button, out int position)
    {
        if (button.Parent != this)
        {
            position = 0;
            return false;
        }

        position = button.GetPositionInParent();
        return true;
    }

    public IEnumerable<ActionButton> GetButtons()
    {
        foreach (var control in Children)
        {
            if (control is ActionButton button)
                yield return button;
        }
    }
}
