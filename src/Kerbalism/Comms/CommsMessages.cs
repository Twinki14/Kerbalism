using Kerbalism.Database;
using Kerbalism.System;

namespace Kerbalism.Comms
{
    public static class CommsMessages
    {
        public static void Update(Vessel v, VesselData vd)
        {
            if (!Lib.IsVessel(v))
                return;

            // do nothing if network is not ready
            if (vd.CommHandler == null || !vd.CommHandler.IsReady)
                return;

            // maintain and send messages
            // - do not send messages during/after solar storms
            // - do not send messages for EVA kerbals
            if (v.isEVA || v.situation == Vessel.Situations.PRELAUNCH)
            {
                return;
            }

            if (!vd.msg_signal && !vd.Connection.linked)
            {
                vd.msg_signal = true;

                if (!vd.cfg_signal)
                {
                    return;
                }

                var subtext = Local.UI_transmissiondisabled;

                if (vd.CrewCount == 0)
                {
                    switch (Settings.UnlinkedControl)
                    {
                        case UnlinkedCtrl.none:
                            subtext = Local.UI_noctrl;
                            break;
                        case UnlinkedCtrl.limited:
                            subtext = Local.UI_limitedcontrol;
                            break;
                    }
                }

                Message.Post(Severity.warning, Lib.BuildString(Local.UI_signallost, " <b>", v.vesselName, "</b>"), subtext);
            }
            else if (vd.msg_signal && vd.Connection.linked)
            {
                vd.msg_signal = false;
                if (vd.cfg_signal)
                {
                    Message.Post(Severity.relax, Lib.BuildString("<b>", v.vesselName, "</b> ", Local.UI_signalback),
                        vd.Connection.Status == (int) LinkStatus.direct_link
                            ? Local.UI_directlink
                            : Lib.BuildString(Local.UI_relayby, " <b>", vd.Connection.target_name, "</b>"));
                }
            }
        }
    }
}
