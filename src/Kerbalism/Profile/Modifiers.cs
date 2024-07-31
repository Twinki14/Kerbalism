using System;
using System.Collections.Generic;
using Kerbalism.Database;
using Kerbalism.Planner;
using Kerbalism.System;

namespace Kerbalism.Profile
{
    public static class Modifiers
    {
        ///<summary> Modifiers Evaluate method used for the Monitors background and current vessel simulation </summary>
        public static double Evaluate(Vessel v, VesselData vd, VesselResources resources, List<string> modifiers)
        {
            double k = 1.0;
            foreach (string mod in modifiers)
            {
                switch (mod)
                {
                    case "zerog":
                        k *= vd.EnvZeroG ? 1.0 : 0.0;
                        break;

                    case "landed":
                        k *= vd.EnvLanded ? 1.0 : 0.0;
                        break;

                    case "breathable":
                        k *= vd.EnvBreathable ? 1.0 : 0.0;
                        break;

                    case "non_breathable":
                        k *= vd.EnvBreathable ? 0.0 : 1.0;
                        break;

                    case "per_capita":
                        k /= (double) Math.Max(vd.CrewCount, 1);
                        break;

                    default:
                        k *= resources.GetResource(v, mod).Amount;
                        break;
                }
            }

            return k;
        }


        ///<summary> Modifiers Evaluate method used for the Planners vessel simulation in the VAB/SPH </summary>
        public static double Evaluate(EnvironmentAnalyzer env, VesselAnalyzer va, ResourceSimulator sim,
            List<string> modifiers)
        {
            double k = 1.0;
            foreach (string mod in modifiers)
            {
                switch (mod)
                {
                    case "zerog":
                        k *= env.zerog ? 1.0 : 0.0;
                        break;

                    case "landed":
                        k *= env.landed ? 1.0 : 0.0;
                        break;

                    case "breathable":
                        k *= env.breathable ? 1.0 : 0.0;
                        break;

                    case "non_breathable":
                        k *= env.breathable ? 0.0 : 1.0;
                        break;

                    case "radiation":
                        k *= Math.Max(Radiation.Nominal,
                            env.landed ? env.surface_rad : env.magnetopause_rad);
                        break;
                    case "per_capita":
                        k /= (double) Math.Max(va.crew_count, 1);
                        break;

                    default:
                        k *= sim.Resource(mod).amount;
                        break;
                }
            }

            return k;
        }
    }
} // KERBALISM
