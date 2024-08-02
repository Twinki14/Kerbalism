using Contracts;
using Kerbalism.Database;
using Kerbalism.System;

namespace Kerbalism.Contracts
{
    // First sample analysis
    public sealed class SpaceAnalysis : Contract
    {
        private bool _meetRequirements;

        protected override bool Generate()
        {
            // never expire
            deadlineType = DeadlineType.None;
            expiryType = DeadlineType.None;

            // set reward
            SetScience(25.0f);
            SetReputation(30.0f, 10.0f);
            SetFunds(25000.0f, 100000.0f);

            // add parameters
            AddParameter(new SpaceAnalysisCondition());
            return true;
        }

        protected override string GetHashString() => "SpaceAnalysis";

        protected override string GetTitle() => Local.Contracts_sampleTitle;

        protected override string GetDescription() => Local.Contracts_sampleDesc;

        protected override string MessageCompleted() => Local.Contracts_sampleComplete;

        public override bool MeetRequirements()
        {
            // stop checking when requirements are met
            if (_meetRequirements)
            {
                return _meetRequirements;
            }

            var lab = PartLoader.getPartInfoByName("Large_Crewed_Lab");

            _meetRequirements =
                Features.Science // science is enabled
                && lab != null // lab part is present
                && ResearchAndDevelopment.PartTechAvailable(lab) // lab part is unlocked
                && !DB.landmarks.SpaceAnalysis; // never analyzed samples in space before

            return _meetRequirements;
        }
    }

    public sealed class SpaceAnalysisCondition : ContractParameter
    {
        protected override string GetHashString()
        {
            return "SpaceAnalysisCondition";
        }

        protected override string GetTitle()
        {
            return Local.Contracts_sampleTitle;
        }

        protected override void OnUpdate()
        {
            if (DB.landmarks.SpaceAnalysis) SetComplete();
        }
    }
}
