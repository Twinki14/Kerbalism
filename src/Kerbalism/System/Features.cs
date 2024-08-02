using System.Collections.Generic;

namespace Kerbalism.System
{
    public static class Features
    {
        public static void Detect()
        {
            // set user-specified features
            Science = Settings.Science;
            SpaceWeather = Settings.SpaceWeather;
            Automation = Settings.Automation;

            // detect all modifiers in use by current profile
            HashSet<string> modifiers = new HashSet<string>();

            // detect features from modifiers
            Radiation = modifiers.Contains("radiation");

            // log features
            Lib.Log("features:");
            Lib.Log("- Science: " + Science);
            Lib.Log("- SpaceWeather: " + SpaceWeather);
            Lib.Log("- Automation: " + Automation);
            Lib.Log("- Radiation: " + Radiation);
        }

        // user-specified features
        public static bool Science;
        public static bool SpaceWeather;
        public static bool Automation;

        // features detected automatically from modifiers
        public static bool Radiation;

        // features detected in other ways
        public static bool Supplies;
    }
} // KERBALISM
