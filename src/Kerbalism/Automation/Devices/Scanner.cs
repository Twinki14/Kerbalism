using Kerbalism.External;

namespace Kerbalism.Automation.Devices
{
    public sealed class ScannerDevice : LoadedDevice<PartModule>
    {
        public ScannerDevice(PartModule module) : base(module)
        {
        }

        public override string Status => Lib.Color(Lib.ReflectionValue<bool>(Module, "scanning"), Local.Generic_ENABLED,
            Lib.Kolor.Green, Local.Generic_DISABLED, Lib.Kolor.Yellow);

        public override void Ctrl(bool value)
        {
            bool scanning = Lib.ReflectionValue<bool>(Module, "scanning");
            if (scanning && !value) Module.Events["stopScan"].Invoke();
            else if (!scanning && value) Module.Events["startScan"].Invoke();
        }

        public override void Toggle()
        {
            Ctrl(!Lib.ReflectionValue<bool>(Module, "scanning"));
        }
    }

    public sealed class ProtoScannerDevice : ProtoDevice<PartModule>
    {
        private readonly Vessel vessel;

        public ProtoScannerDevice(PartModule prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule,
            Vessel v)
            : base(prefab, protoPart, protoModule)
        {
            this.vessel = v;
        }

        public override string Status => Lib.Color(Lib.Proto.GetBool(ProtoModule, "scanning"), Local.Generic_ENABLED,
            Lib.Kolor.Green, Local.Generic_DISABLED, Lib.Kolor.Yellow);

        public override void Ctrl(bool value)
        {
            bool scanning = Lib.Proto.GetBool(ProtoModule, "scanning");
            if (scanning && !value) SCANsat.StopScanner(vessel, ProtoModule, Prefab.part);
            else if (!scanning && value) SCANsat.ResumeScanner(vessel, ProtoModule, Prefab.part);
        }

        public override void Toggle()
        {
            Ctrl(!Lib.Proto.GetBool(ProtoModule, "scanning"));
        }
    }
} // KERBALISM