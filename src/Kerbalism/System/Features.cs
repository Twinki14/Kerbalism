namespace Kerbalism.System
{
    public static class Features
    {
        public static void Detect()
        {
            // set user-specified features
            Science = Settings.Science;
            Automation = Settings.Automation;

            Radiation = true;

            // log features
            Lib.Log("features:");
            Lib.Log("- Science: " + Science);
            Lib.Log("- Automation: " + Automation);
            Lib.Log("- Radiation: " + Radiation);
        }

        // user-specified features
        public static bool Science;
        public static bool Automation;

        // features detected automatically from modifiers
        public static bool Radiation;
    }
}
