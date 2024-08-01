using Contracts;
using Kerbalism.Database;


namespace Kerbalism.Contracts
{
    // put a kerbal in orbit for 30 days


    public sealed class MannedOrbitCondition : ContractParameter
    {
        protected override string GetHashString()
        {
            return "MannedOrbitCondition";
        }

        protected override string GetTitle()
        {
            return Local.Contracts_orbitTitle;
        }

        protected override void OnUpdate()
        {
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                VesselData vd = v.KerbalismData();
                if (!vd.IsSimulated) continue;
                bool manned = vd.CrewCount > 0;
                bool in_orbit = Sim.Apoapsis(v) > v.mainBody.atmosphereDepth &&
                                Sim.Periapsis(v) > v.mainBody.atmosphereDepth;
                bool for_30days = v.missionTime > 60.0 * 60.0 * Lib.HoursInDay * 30.0;
                if (manned && in_orbit && for_30days)
                {
                    SetComplete();
                    DB.landmarks.manned_orbit = true; //< remember that contract was completed
                    break;
                }
            }
        }
    }
} // KERBALISM.CONTRACTS
