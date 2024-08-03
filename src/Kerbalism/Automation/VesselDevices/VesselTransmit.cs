using Kerbalism.Database;

namespace Kerbalism.Automation.VesselDevices
{
    public class VesselDeviceTransmit : VesselDevice
    {
        public VesselDeviceTransmit(Vessel v, VesselData vd) : base(vd)
        {
        }

        public override string Name => "data transmission";

        public override string Status => Lib.Color(VesselData.deviceTransmit, Local.Generic_ENABLED, Lib.Kolor.Green,
            Local.Generic_DISABLED, Lib.Kolor.Yellow);

        public override void Ctrl(bool value) => VesselData.deviceTransmit = value;

        public override void Toggle() => VesselData.deviceTransmit = !VesselData.deviceTransmit;
    }
}
