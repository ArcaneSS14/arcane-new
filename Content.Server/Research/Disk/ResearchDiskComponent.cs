
// Arcane-Start
using Content.Shared.Research;
using Content.Arcane.Common.Research;
// Arcane-End

namespace Content.Server.Research.Disk
{
    [RegisterComponent]
    public sealed partial class ResearchDiskComponent : Component
    {
        [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
        public int Points = 1000;

        // Arcane-Start
        [DataField]
        public List<ResearchPointAmount> PointBalances = new();
        // Arcane-End

        /// <summary>
        /// If true, the value of this disk will be set to the sum
        /// of all the technologies in the game.
        /// </summary>
        /// <remarks>
        /// This is for debug purposes only.
        /// </remarks>
        [DataField("unlockAllTech")]
        public bool UnlockAllTech = false;
    }
}
