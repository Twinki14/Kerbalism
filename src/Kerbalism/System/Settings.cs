using System.Collections.Generic;

namespace Kerbalism.System
{
    public enum UnlinkedCtrl
    {
        none, // disable all controls
        limited, // disable all controls except full/zero throttle and staging
        full // do not disable controls at all
    }


    public static class Settings
    {
        private static string MODS_INCOMPATIBLE = "TacLifeSupport,Snacks,KolonyTools,USILifeSupport";
        private static string MODS_WARNING = "RemoteTech,CommNetAntennasInfo";
        private static string MODS_SCIENCE = "KEI,[x] Science!";

        public static void Parse()
        {
            var kerbalismConfigNodes = GameDatabase.Instance.GetConfigs("Kerbalism");
            if (kerbalismConfigNodes.Length < 1) return;
            ConfigNode cfg = kerbalismConfigNodes[0].config;

            // profile used
            Profile = Lib.ConfigValue(cfg, "Profile", string.Empty);

            // user-defined features
            Deploy = Lib.ConfigValue(cfg, "Deploy", false);
            Science = Lib.ConfigValue(cfg, "Science", false);
            SpaceWeather = Lib.ConfigValue(cfg, "SpaceWeather", false);
            Automation = Lib.ConfigValue(cfg, "Automation", false);

            // signal
            UnlinkedControl = Lib.ConfigEnum(cfg, "UnlinkedControl", UnlinkedCtrl.none);
            DataRateMinimumBitsPerSecond = Lib.ConfigValue(cfg, "DataRateMinimumBitsPerSecond", 1.0f);
            DataRateSurfaceExperiment = Lib.ConfigValue(cfg, "DataRateSurfaceExperiment", 0.3f);
            TransmitterActiveEcFactor = Lib.ConfigValue(cfg, "TransmitterActiveEcFactor", 1.5);
            TransmitterPassiveEcFactor = Lib.ConfigValue(cfg, "TransmitterPassiveEcFactor", 0.04);
            TransmitterActiveEcFactorRT = Lib.ConfigValue(cfg, "TransmitterActiveEcFactorRT", 1.0);
            TransmitterPassiveEcFactorRT = Lib.ConfigValue(cfg, "TransmitterPassiveEcFactorRT", 1.0);
            DampingExponentOverride = Lib.ConfigValue(cfg, "DampingExponentOverride", 0.0);

            // science
            ScienceDialog = Lib.ConfigValue(cfg, "ScienceDialog", true);
            AsteroidSampleMassPerMB = Lib.ConfigValue(cfg, "AsteroidSampleMassPerMB", 0.00002);

            // crew level
            LaboratoryCrewLevelBonus = Lib.ConfigValue(cfg, "LaboratoryCrewLevelBonus", 0.2);
            MaxLaborartoryBonus = Lib.ConfigValue(cfg, "MaxLaborartoryBonus", 2.0);
            HarvesterCrewLevelBonus = Lib.ConfigValue(cfg, "HarvesterCrewLevelBonus", 0.1);
            MaxHarvesterBonus = Lib.ConfigValue(cfg, "MaxHarvesterBonus", 2.0);

            // misc
            EnforceCoherency = Lib.ConfigValue(cfg, "EnforceCoherency", true);
            HeadLampsCost = Lib.ConfigValue(cfg, "HeadLampsCost", 0.002);
            LowQualityRendering = Lib.ConfigValue(cfg, "LowQualityRendering", false);
            UIScale = Lib.ConfigValue(cfg, "UIScale", 1.0f);
            UIPanelWidthScale = Lib.ConfigValue(cfg, "UIPanelWidthScale", 1.0f);
            KerbalDeathReputationPenalty = Lib.ConfigValue(cfg, "KerbalDeathReputationPenalty", 100.0f);
            KerbalBreakdownReputationPenalty = Lib.ConfigValue(cfg, "KerbalBreakdownReputationPenalty", 30f);

            // save game settings presets
            StormFrequency = Lib.ConfigValue(cfg, "StormFrequency", 0.4f);
            StormRadiation = Lib.ConfigValue(cfg, "StormRadiation", 5.0f);
            StormDurationHours = Lib.ConfigValue(cfg, "StormDurationHours", 2);
            StormEjectionSpeed = Lib.ConfigValue(cfg, "StormEjectionSpeed", 0.33f);
            ShieldingEfficiency = Lib.ConfigValue(cfg, "ShieldingEfficiency", 0.9f);
            ShieldingEfficiencyEasyMult = Lib.ConfigValue(cfg, "ShieldingEfficiencyEasyMult", 1.1f);
            ShieldingEfficiencyModerateMult = Lib.ConfigValue(cfg, "ShieldingEfficiencyModerateMult", 0.9f);
            ShieldingEfficiencyHardMult = Lib.ConfigValue(cfg, "ShieldingEfficiencyHardMult", 0.8f);
            ExternRadiation = Lib.ConfigValue(cfg, "ExternRadiation", 0.04f);
            RadiationInSievert = Lib.ConfigValue(cfg, "RadiationInSievert", false);
            UseSIUnits = Lib.ConfigValue(cfg, "UseSIUnits", false);

            ModsIncompatible = Lib.ConfigValue(cfg, "ModsIncompatible", MODS_INCOMPATIBLE);
            ModsWarning = Lib.ConfigValue(cfg, "ModsWarning", MODS_WARNING);
            ModsScience = Lib.ConfigValue(cfg, "ModsScience", MODS_SCIENCE);
            CheckForCRP = Lib.ConfigValue(cfg, "CheckForCRP", true);

            UseSamplingSunFactor = Lib.ConfigValue(cfg, "UseSamplingSunFactor", false);
            UseResourcePriority = Lib.ConfigValue(cfg, "UseResourcePriority", false);

            loaded = true;
        }

        // profile used
        public static string Profile;

        internal static List<string> IncompatibleMods()
        {
            var result = Lib.Tokenize(ModsIncompatible.ToLower(), ',');
            return result;
        }

        internal static List<string> WarningMods()
        {
            var result = Lib.Tokenize(ModsWarning.ToLower(), ',');
            if (Features.Science) result.AddRange(Lib.Tokenize(ModsScience.ToLower(), ','));
            return result;
        }

        // name of profile to use, if any

        // user-defined features
        public static bool Deploy; // add EC cost to keep module working/animation, add EC cost to Extend\Retract
        public static bool Science; // science data storage, transmission and analysis
        public static bool SpaceWeather; // coronal mass ejections
        public static bool Automation; // control vessel components using scripts

        // pressure
        public static double PressureThreshold; // level of atmosphere resource that determine pressurized status

        // signal
        public static UnlinkedCtrl
            UnlinkedControl; // available control for unlinked vessels: 'none', 'limited' or 'full'

        public static float
            DataRateMinimumBitsPerSecond; // as long as there is a control connection, the science data rate will never go below this.

        public static float DataRateSurfaceExperiment; // transmission rate for surface experiments (Serenity DLC)

        public static double
            TransmitterActiveEcFactor; // how much of the configured EC rate is used while transmitter is active

        public static double
            TransmitterPassiveEcFactor; // how much of the configured EC rate is used while transmitter is passive

        public static double
            TransmitterActiveEcFactorRT; // RemoteTech, how much of the configured EC rate is used while transmitter is active, (transmitting)

        public static double
            TransmitterPassiveEcFactorRT; // RemoteTech, how much of the configured EC rate is used while transmitter is passive, (idle)

        public static double
            DampingExponentOverride; // Kerbalism will calculate a damping exponent to achieve good data communication rates (see log file, search for DataRateDampingExponent). If the calculated value is not good for you, you can set your own.

        // science
        public static bool ScienceDialog; // keep showing the stock science dialog

        public static double
            AsteroidSampleMassPerMB; // When taking an asteroid sample, mass (in t) per MB of sample (baseValue * dataScale). default of 0.00002 => 34 Kg in stock


        // crew level
        public static double
            LaboratoryCrewLevelBonus; // factor for laboratory rate speed gain per crew level above minimum

        public static double MaxLaborartoryBonus; // max bonus to be gained by having skilled crew on a laboratory

        public static double
            HarvesterCrewLevelBonus; // factor for harvester speed gain per engineer level above minimum

        public static double MaxHarvesterBonus; // max bonus to be gained by having skilled engineers on a mining rig

        // misc
        public static bool EnforceCoherency; // detect and avoid issues at high timewarp in external modules
        public static double HeadLampsCost; // EC/s cost if eva headlamps are on
        public static bool LowQualityRendering; // use less particles to render the magnetic fields

        public static float
            UIScale; // scale UI elements by this factor, relative to KSP scaling settings, useful for high PPI screens

        public static float
            UIPanelWidthScale; // scale UI Panel Width by this factor, relative to KSP scaling settings, useful for high PPI screens

        public static float KerbalDeathReputationPenalty; // Reputation penalty when Kerbals dies

        public static float
            KerbalBreakdownReputationPenalty; // Reputation removed when Kerbals loose their marbles in space


        // presets for save game preferences
        public static float StormFrequency;
        public static int StormDurationHours;
        public static float StormEjectionSpeed;
        public static float ShieldingEfficiency;
        public static float ShieldingEfficiencyEasyMult;
        public static float ShieldingEfficiencyModerateMult;
        public static float ShieldingEfficiencyHardMult;
        public static float StormRadiation;
        public static float ExternRadiation;
        public static bool RadiationInSievert; // use Sievert iso. rad
        public static bool UseSIUnits; // use SI units instead of human-readable pretty-printing when available

        // sanity check settings
        public static string ModsIncompatible;
        public static string ModsWarning;
        public static string ModsScience;
        public static bool CheckForCRP;

        public static bool UseSamplingSunFactor;
        public static bool UseResourcePriority;

        public static bool loaded { get; private set; } = false;
    }
} // KERBALISM
