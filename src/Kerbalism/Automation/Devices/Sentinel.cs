using Kerbalism.Database;
using Kerbalism.Modules;
using KSP.Localization;
using SentinelMission;

namespace Kerbalism.Automation.Devices
{
    public sealed class SentinelDevice : LoadedDevice<KerbalismSentinel>
    {
        public SentinelDevice(KerbalismSentinel module) : base(module)
        {
        }

        public override string Name => SentinelUtilities.SentinelPartTitle;

        public override string Status
        {
            get
            {
                if (!Module.isTrackingEnabled)
                    return Local.Generic_DISABLED;

                return Module.status;
            }
        }

        public override void Ctrl(bool value)
        {
            if (value)
                Module.StartTracking();
            else
                Module.StopTracking();
        }

        public override void Toggle() => Ctrl(!Module.isTrackingEnabled);
    }

    public sealed class ProtoSentinelDevice : ProtoDevice<KerbalismSentinel>
    {
        public ProtoSentinelDevice(KerbalismSentinel prefab, ProtoPartSnapshot protoPart,
            ProtoPartModuleSnapshot protoModule, Vessel vessel)
            : base(prefab, protoPart, protoModule)
        {
            this.vessel = vessel;
        }

        private readonly Vessel vessel;

        public override string Name => SentinelUtilities.SentinelPartTitle;

        public override string Status
        {
            get
            {
                if (!Lib.Proto.GetBool(ProtoModule, "isTrackingEnabled"))
                    return Local.Generic_DISABLED;

                if (Lib.Proto.GetBool(ProtoModule, "isTracking"))
                {
                    if (SentinelUtilities.FindInnerAndOuterBodies(vessel, out var innerBody, out var outerBody))
                    {
                        return (SentinelUtilities.SentinelCanScan(vessel, innerBody, outerBody)
                            ? Localizer.Format("#autoLOC_6002291", outerBody.displayName)
                            : Localizer.Format("#autoLOC_6002292", outerBody.displayName));
                    }
                    else
                    {
                        return Localizer.Format("#autoLOC_6002290");
                    }
                }

                VesselData vd = vessel.KerbalismData();

                if (!vd.Connection.linked || vd.Connection.rate < Prefab.comms_rate)
                {
                    return "Comms connection too weak";
                }

                ResourceInfo ec = ResourceCache.GetResource(vessel, "ElectricCharge");

                if (ec.Amount <= double.Epsilon)
                {
                    return Local.Module_Experiment_issue4; // "no Electricity"
                }

                return Localizer.Format("#autoLOC_6002296");
            }
        }

        public override void Ctrl(bool value) => Lib.Proto.Set(ProtoModule, "isTrackingEnabled", value);

        public override void Toggle() => Ctrl(!Lib.Proto.GetBool(ProtoModule, "isTrackingEnabled"));
    }
}