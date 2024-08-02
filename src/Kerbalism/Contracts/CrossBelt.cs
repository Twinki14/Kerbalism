using Contracts;
using Kerbalism.Database;
using Kerbalism.System;

namespace Kerbalism.Contracts
{
    // Cross radiation belt
    public sealed class CrossBelt : Contract
    {
        private bool _meetRequirements;

        protected override bool Generate()
        {
            // never expire
            deadlineType = DeadlineType.None;
            expiryType = DeadlineType.None;

            // set reward
            SetScience(10.0f);
            SetReputation(10.0f, 5.0f);
            SetFunds(5000.0f, 25000.0f);

            // add parameters
            AddParameter(new CrossBeltCondition());
            return true;
        }

        protected override string GetHashString() => "CrossBelt";

        protected override string GetTitle() => Local.Contracts_radTitle;

        protected override string GetDescription() => Local.Contracts_radDesc;

        protected override string MessageCompleted() => Local.Contracts_radComplete;

        public override bool MeetRequirements()
        {
            // stop checking when requirements are met
            if (_meetRequirements)
            {
                return _meetRequirements;
            }

            var progress = ProgressTracking.Instance;

            _meetRequirements =
                Features.Radiation // radiation is enabled
                && progress != null && progress.reachSpace.IsComplete // first suborbit flight completed
                && !DB.landmarks.BeltCrossing; // belt never crossed before

            return _meetRequirements;
        }
    }

    public sealed class CrossBeltCondition : ContractParameter
    {
        protected override string GetHashString() => "CrossBeltCondition";

        protected override string GetTitle() => Local.Contracts_radTitle;

        protected override void OnUpdate()
        {
            if (DB.landmarks.BeltCrossing)
            {
                SetComplete();
            }
        }
    }
}
