using Content.Arcane.Client.DiscordRoles;
using Content.Arcane.Shared.DiscordRoles;

namespace Content.Arcane.Client.IoC;

internal static class ClientArcaneContentIoC
{
    internal static void Register(IDependencyCollection instance)
    {
        instance.Register<DiscordRoleManager>();
        instance.Register<ISharedDiscordRoleManager, DiscordRoleManager>();
    }
}
