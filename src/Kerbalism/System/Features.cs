using System.Collections.Generic;
using Kerbalism.Profile;

namespace Kerbalism.System
{
    public static class Features
    {
        public static void Detect()
        {
            // set user-specified features
            Deploy = Settings.Deploy;
            Science = Settings.Science;
            SpaceWeather = Settings.SpaceWeather;
            Automation = Settings.Automation;

            // detect all modifiers in use by current profile
            HashSet<string> modifiers = new HashSet<string>();
            foreach (Rule rule in Profile.Profile.rules)
            {
                foreach (string s in rule.modifiers) modifiers.Add(s);
            }

            foreach (Process process in Profile.Profile.processes)
            {
                foreach (string s in process.modifiers) modifiers.Add(s);
            }

            // detect features from modifiers
            Radiation = modifiers.Contains("radiation");

            // supplies is enabled if any non-EC supply exist
            Supplies = Profile.Profile.supplies.Find(k => k.resource != "ElectricCharge") != null;

            // log features
            Lib.Log("features:");
            Lib.Log("- Deploy: " + Deploy);
            Lib.Log("- Science: " + Science);
            Lib.Log("- SpaceWeather: " + SpaceWeather);
            Lib.Log("- Automation: " + Automation);
            Lib.Log("- Radiation: " + Radiation);
            Lib.Log("- Supplies: " + Supplies);
        }

        // user-specified features
        public static bool Deploy;
        public static bool Science;
        public static bool SpaceWeather;
        public static bool Automation;

        // features detected automatically from modifiers
        public static bool Radiation;
        public static bool Shielding;

        // features detected in other ways
        public static bool Supplies;
    }
} // KERBALISM
