// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Arcane.Common.ModSuits;

[ByRefEvent]
public record struct ModifyGrabEvent()
{
    public bool UpgradeSoft;
    public float EscapeMultiplier = 1f;
    public float EscapeOffset;
    public float SpeedMultiplier = 1f;
}
