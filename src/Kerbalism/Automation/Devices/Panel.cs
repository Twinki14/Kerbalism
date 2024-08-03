using System;
using Kerbalism.Modules;

namespace Kerbalism.Automation.Devices
{
    public sealed class PanelDevice : LoadedDevice<SolarPanelFixer>
    {
        public PanelDevice(SolarPanelFixer module) : base(module)
        {
        }

        public override string Name
        {
            get
            {
                if (Module.SolarPanel.IsRetractable())
                    return Local.SolarPanel_deployable; //"solar panel (deployable)"
                else
                    return Local.SolarPanel_nonretractable; //"solar panel (non retractable)"
            }
        }

        public override string Status
        {
            get
            {
                switch (Module.state)
                {
                    case SolarPanelFixer.PanelState.Retracted:
                        return Lib.Color(Local.Generic_RETRACTED, Lib.Kolor.Yellow);
                    case SolarPanelFixer.PanelState.Extending: return Local.Generic_EXTENDING;
                    case SolarPanelFixer.PanelState.Extended: return Lib.Color(Local.Generic_EXTENDED, Lib.Kolor.Green);
                    case SolarPanelFixer.PanelState.Retracting: return Local.Generic_RETRACTING;
                }

                return Local.Statu_unknown; //"unknown"
            }
        }

        public override bool IsVisible => Module.SolarPanel.SupportAutomation(Module.state);

        public override void Ctrl(bool value)
        {
            if (value && Module.state == SolarPanelFixer.PanelState.Retracted) Module.ToggleState();
            if (!value && Module.state == SolarPanelFixer.PanelState.Extended) Module.ToggleState();
        }

        public override void Toggle()
        {
            if (Module.state == SolarPanelFixer.PanelState.Retracted ||
                Module.state == SolarPanelFixer.PanelState.Extended)
                Module.ToggleState();
        }
    }

    public sealed class ProtoPanelDevice : ProtoDevice<SolarPanelFixer>
    {
        public ProtoPanelDevice(SolarPanelFixer prefab, ProtoPartSnapshot protoPart,
            ProtoPartModuleSnapshot protoModule)
            : base(prefab, protoPart, protoModule)
        {
        }

        public override string Name
        {
            get
            {
                if (Prefab.SolarPanel.IsRetractable())
                    return Local.SolarPanel_deployable; //"solar panel (deployable)"
                else
                    return Local.SolarPanel_nonretractable; //"solar panel (non retractable)"
            }
        }

        public override uint PartId => ProtoPart.flightID;

        public override string Status
        {
            get
            {
                string state = Lib.Proto.GetString(ProtoModule, "state");
                switch (state)
                {
                    case "Retracted": return Lib.Color(Local.Generic_RETRACTED, Lib.Kolor.Yellow);
                    case "Extended": return Lib.Color(Local.Generic_EXTENDED, Lib.Kolor.Green);
                }

                return Local.Statu_unknown; //"unknown"
            }
        }

        public override bool IsVisible => Prefab.SolarPanel.SupportProtoAutomation(ProtoModule);

        public override void Ctrl(bool value)
        {
            SolarPanelFixer.PanelState state =
                (SolarPanelFixer.PanelState) Enum.Parse(typeof(SolarPanelFixer.PanelState),
                    Lib.Proto.GetString(ProtoModule, "state"));
            if ((value && state == SolarPanelFixer.PanelState.Retracted)
                ||
                (!value && state == SolarPanelFixer.PanelState.Extended))
                SolarPanelFixer.ProtoToggleState(Prefab, ProtoModule, state);
        }

        public override void Toggle()
        {
            SolarPanelFixer.PanelState state =
                (SolarPanelFixer.PanelState) Enum.Parse(typeof(SolarPanelFixer.PanelState),
                    Lib.Proto.GetString(ProtoModule, "state"));
            SolarPanelFixer.ProtoToggleState(Prefab, ProtoModule, state);
        }
    }
} // KERBALISM