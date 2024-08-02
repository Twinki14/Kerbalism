using Kerbalism.Database;
using Kerbalism.System;


namespace Kerbalism
{
    public static class VesselConfig
    {
        public static void Config(this Panel p, Vessel v)
        {
            // avoid corner-case when this is called in a lambda after scene changes
            v = FlightGlobals.FindVessel(v.id);

            // if vessel doesn't exist anymore, leave the panel empty
            if (v == null) return;

            // get vessel data
            VesselData vd = v.KerbalismData();

            // if not a valid vessel, leave the panel empty
            if (!vd.IsSimulated) return;

            // set metadata
            p.Title(Lib.BuildString(Lib.Ellipsis(v.vesselName, Styles.ScaleStringLength(20)), " ",
                Lib.Color(Local.VESSELCONFIG_title, Lib.Kolor.LightGrey))); //"VESSEL CONFIG"
            p.Width(Styles.ScaleWidthFloat(355.0f));
            p.paneltype = Panel.PanelType.config;

            // toggle rendering
            string tooltip;
            p.AddSection(Local.VESSELCONFIG_RENDERING); //"RENDERING"

            tooltip = Local.VESSELCONFIG_ShowVessel_desc;
            p.AddContent(Local.VESSELCONFIG_ShowVessel, string.Empty, tooltip);
            p.AddRightIcon(vd.cfg_show ? Textures.toggle_green : Textures.toggle_red, tooltip,
                () => p.Toggle(ref vd.cfg_show));

            // toggle messages
            p.AddSection(Local.VESSELCONFIG_MESSAGES); //"MESSAGES"
            tooltip = Local.VESSELCONFIG_EClow; //"Receive a message when\nElectricCharge level is low"
            p.AddContent(Local.VESSELCONFIG_battery, string.Empty, tooltip); //"battery"
            p.AddRightIcon(vd.cfg_ec ? Textures.toggle_green : Textures.toggle_red, tooltip,
                () => p.Toggle(ref vd.cfg_ec));

            if (API.Comm.handlers.Count > 0 || HighLogic.fetch.currentGame.Parameters.Difficulty.EnableCommNet)
            {
                tooltip = Local.VESSELCONFIG_Signallost; //"Receive a message when signal is lost or obtained"
                p.AddContent(Local.VESSELCONFIG_signal, string.Empty, tooltip); //"signal"
                p.AddRightIcon(vd.cfg_signal ? Textures.toggle_green : Textures.toggle_red, tooltip,
                    () => p.Toggle(ref vd.cfg_signal));
            }

            if (Features.SpaceWeather)
            {
                tooltip = Local.VESSELCONFIG_CMEevent; //"Receive a message\nduring CME events"
                p.AddContent(Local.VESSELCONFIG_storm, string.Empty, tooltip); //"storm"
                p.AddRightIcon(vd.cfg_storm ? Textures.toggle_green : Textures.toggle_red, tooltip,
                    () => p.Toggle(ref vd.cfg_storm));
            }

            if (Features.Automation)
            {
                tooltip = Local.VESSELCONFIG_ScriptExe; //"Receive a message when\nscripts are executed"
                p.AddContent(Local.VESSELCONFIG_script, string.Empty, tooltip); //"script"
                p.AddRightIcon(vd.cfg_script ? Textures.toggle_green : Textures.toggle_red, tooltip,
                    () => p.Toggle(ref vd.cfg_script));
            }
        }
    }
} // KERBALISM
