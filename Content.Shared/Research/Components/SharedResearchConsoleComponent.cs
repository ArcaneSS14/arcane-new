using Content.Goobstation.Common.Research;
using Robust.Shared.Serialization;
// Arcane-Start
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
// Arcane-End

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public int Points;

        /// <summary>
        /// Goobstation field - all researches and their availablities
        /// </summary>
        public Dictionary<string, ResearchAvailability> Researches;
        // Arcane-Start
        public List<ProtoId<TechnologyPrototype>> VisibleTechnologies;
        public List<ProtoId<TechnologyPrototype>> AvailableTechnologies;
        public List<ProtoId<TechnologyPrototype>> ResearchedTechnologies;
        public List<string> CompletedExperiments;
        public List<ResearchConsoleExperimentData> Experiments;
        public Dictionary<string, ResearchTechnologyLockReason> TechnologyLockReasons;
        public string NetworkId;
        public List<ResearchPointAmount> PointBalances;
        public List<ResearchLogEntry> Logs;
        // Arcane-End

        // Arcane-Edit-Start
        public ResearchConsoleBoundInterfaceState(
            int points,
            Dictionary<string, ResearchAvailability> researches,
            List<ProtoId<TechnologyPrototype>> visibleTechnologies,
            List<ProtoId<TechnologyPrototype>> availableTechnologies,
            List<ProtoId<TechnologyPrototype>> researchedTechnologies,
            List<string> completedExperiments,
            List<ResearchConsoleExperimentData> experiments,
            Dictionary<string, ResearchTechnologyLockReason> technologyLockReasons,
            string networkId,
            List<ResearchPointAmount> pointBalances,
            List<ResearchLogEntry> logs) // Goobstation R&D console rework = researches field
        // Arcane-Edit-End
        {
            Points = points;
            Researches = researches;    // Goobstation R&D console rework
            // Arcane-Start
            VisibleTechnologies = visibleTechnologies;
            AvailableTechnologies = availableTechnologies;
            ResearchedTechnologies = researchedTechnologies;
            CompletedExperiments = completedExperiments;
            Experiments = experiments;
            TechnologyLockReasons = technologyLockReasons;
            NetworkId = networkId;
            PointBalances = pointBalances;
            Logs = logs;
            // Arcane-End
        }
    }

    // Arcane-Start
    [Serializable, NetSerializable]
    public sealed class ResearchConsoleExperimentData
    {
        public string Id;
        public int Progress;
        public int Target;
        public ResearchExperimentState State;

        public ResearchConsoleExperimentData(string id, int progress, int target, ResearchExperimentState state)
        {
            Id = id;
            Progress = progress;
            Target = target;
            State = state;
        }
    }
    // Arcane-End
}
