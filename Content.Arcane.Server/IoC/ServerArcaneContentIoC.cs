using Content.Arcane.Common.Sponsors;
using Content.Arcane.Server.DiscordRoles;
using Content.Arcane.Server.Sponsor;
using Content.Arcane.Server.TTS;
using Content.Arcane.Shared.DiscordRoles;

namespace Content.Arcane.Server.IoC;

internal static class ServerArcaneContentIoC
{
    internal static void Register(IDependencyCollection instance)
    {
        instance.Register<DiscordRoleManager>();
        instance.Register<ISharedDiscordRoleManager, DiscordRoleManager>();
        instance.Register<ISharedSponsorsManager, DiscordRoleSponsorsManager>();
        instance.Register<TTSManager>();
    }
}
