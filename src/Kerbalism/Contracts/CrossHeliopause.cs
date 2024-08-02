using System.Linq;
using Contracts;
using Kerbalism.Database;
using Kerbalism.System;

namespace Kerbalism.Contracts
{
    // Cross the heliopause
    public sealed class CrossHeliopause : Contract
    {
        private bool _meetRequirements;

        protected override bool Generate()
        {
            // never expire
            deadlineType = DeadlineType.None;
            expiryType = DeadlineType.None;

            // set reward
            SetScience(100.0f);
            SetReputation(100.0f, 50.0f);
            SetFunds(100000.0f, 500000.0f);

            // add parameters
            AddParameter(new CrossHeliopauseCondition());
            return true;
        }

        protected override string GetHashString() => "CrossHeliopause";

        protected override string GetTitle() => Local.Contracts_heliopauseTitle;

        protected override string GetDescription() => Local.Contracts_heliopauseDesc;

        protected override string MessageCompleted() => Local.Contracts_heliopauseComplete;

        public override bool MeetRequirements()
        {
            // stop checking when requirements are met
            if (_meetRequirements)
            {
                return _meetRequirements;
            }

            var progress = ProgressTracking.Instance;
            if (progress == null)
            {
                return false;
            }

            var known = progress.celestialBodyNodes.Sum(bodyProgress => bodyProgress.flyBy != null && bodyProgress.flyBy.IsComplete ? 1 : 0);

            var endGame = known > FlightGlobals.Bodies.Count / 2;

            _meetRequirements =
                Features.Radiation // radiation is enabled
                && endGame // entered SOI of half the bodies
                && Radiation.Info(FlightGlobals.Bodies[0]).model.has_pause // there is an actual heliopause
                && !DB.landmarks.HeliopauseCrossing; // heliopause never crossed before

            return _meetRequirements;
        }
    }

    // Cross radiation belt - condition
    public sealed class CrossHeliopauseCondition : ContractParameter
    {
        protected override string GetHashString() => "CrossHeliopauseCondition";

        protected override string GetTitle() => Local.Contracts_heliopauseTitle;

        protected override void OnUpdate()
        {
            if (DB.landmarks.HeliopauseCrossing)
            {
                SetComplete();
            }
        }
    }
}
