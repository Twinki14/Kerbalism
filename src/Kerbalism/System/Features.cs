using System.Collections.Generic;

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

        // features detected in other ways
        public static bool Supplies;
    }
} // KERBALISM
