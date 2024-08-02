namespace Kerbalism.System
{
    public class PreferencesScience : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("#KERBALISM_TransmitScienceImmediately",
            toolTip = "#KERBALISM_TransmitScienceImmediately_desc")]
        //Transmit Science Immediately--Automatically flag science files for transmission
        public bool transmitScience = true;

        [GameParameters.CustomParameterUI("#KERBALISM_AnalyzeSamplesImmediately",
            toolTip = "#KERBALISM_AnalyzeSamplesImmediately_desc")]
        //Analyze Samples Immediately--Automatically flag samples for analysis in a lab
        public bool analyzeSamples = true;

        [GameParameters.CustomFloatParameterUI("#KERBALISM_AntennaSpeed", asPercentage = true, minValue = 0.01f,
            maxValue = 2f, displayFormat = "F2", toolTip = "#KERBALISM_AntennaSpeed_desc")]
        //Antenna Speed--Antenna Bandwidth factor
        public float transmitFactor = 1.0f;

        [GameParameters.CustomFloatParameterUI("#KERBALISM_Alwaysallowsampletransfers",
            toolTip = "#KERBALISM_Alwaysallowsampletransfers_desc")]
        //Always allow sample transfers---When off, sample transfer is only available in crewed vessels
        public bool sampleTransfer = true;

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

        public override bool HasPresets => true;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    sampleTransfer = true;
                    transmitFactor = 2f;
                    break;
                case GameParameters.Preset.Normal:
                    sampleTransfer = true;
                    transmitFactor = 1.5f;
                    break;
                case GameParameters.Preset.Moderate:
                    sampleTransfer = true;
                    transmitFactor = 1.2f;
                    break;
                case GameParameters.Preset.Hard:
                    sampleTransfer = false;
                    transmitFactor = 1.0f;
                    break;
                default:
                    break;
            }
        }

        public override string DisplaySection => "Kerbalism (1)";

        public override string Section => "Kerbalism (1)";

        public override int SectionOrder => 2;

        public override string Title => Local.Preferences_Science; //"Science"

        private static PreferencesScience instance;

        public static PreferencesScience Instance
        {
            get
            {
                if (instance == null)
                {
                    if (HighLogic.CurrentGame != null)
                    {
                        instance = HighLogic.CurrentGame.Parameters.CustomParams<PreferencesScience>();
                    }
                }

                return instance;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            instance = null;
        }
    }

    public class PreferencesMessages : GameParameters.CustomParameterNode
    {
        [GameParameters.CustomParameterUI("#KERBALISM_ElectricalCharge",
            toolTip = "#KERBALISM_ElectricalCharge_desc")]
        //Electrical Charge--Show a message when EC level is low\n(Preset, can be changed per vessel)
        public bool ec = true;

        [GameParameters.CustomParameterUI("#KERBALISM_Supplies",
            toolTip = "#KERBALISM_Supplies_desc")]
        //Supplies--Show a message when supply resources level is low\n(Preset, can be changed per vessel)
        public bool supply = true;

        [GameParameters.CustomParameterUI("#KERBALISM_Signal",
            toolTip = "#KERBALISM_Signal_desc")]
        //Signal--Show a message when signal is lost or obtained\n(Preset, can be changed per vessel)
        public bool signal = false;

        [GameParameters.CustomParameterUI("#KERBALISM_Scripts",
            toolTip = "#KERBALISM_Scripts_desc")]
        //Scripts--Show a message when scripts are executed\n(Preset, can be changed per vessel)
        public bool script = false;

        [GameParameters.CustomParameterUI("#KERBALISM_StockMessages",
            toolTip = "#KERBALISM_StockMessages_desc")]
        //Stock Messages---Use the stock message system instead of our own
        public bool stockMessages = false;

        [GameParameters.CustomIntParameterUI("#KERBALISM_MessageDuration", minValue = 0, maxValue = 30,
            toolTip = "#KERBALISM_MessageDuration_desc")]
        //Message Duration--Duration of messages on screen in seconds
        public int messageLength = 4;

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

        public override bool HasPresets => false;

        public override string DisplaySection => "Kerbalism (2)";

        public override string Section => "Kerbalism (2)";

        public override int SectionOrder => 0;

        public override string Title => Local.Preferences_Notifications; //"Notifications"

        private static PreferencesMessages instance;

        public static PreferencesMessages Instance
        {
            get
            {
                if (instance == null)
                {
                    if (HighLogic.CurrentGame != null)
                    {
                        instance = HighLogic.CurrentGame.Parameters.CustomParams<PreferencesMessages>();
                    }
                }

                return instance;
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            instance = null;
        }
    }
}
