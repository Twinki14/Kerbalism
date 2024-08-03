namespace Kerbalism.Automation.Devices
{
    public sealed class AntennaRTDevice : LoadedDevice<PartModule>
    {
        public AntennaRTDevice(PartModule module) : base(module)
        {
        }

        public override string Name => "antenna";

        public override string Status
        {
            get
            {
                return Lib.ReflectionValue<bool>(Module, "IsRTActive")
                    ? Lib.Color(Local.Generic_ACTIVE, Lib.Kolor.Green)
                    : Lib.Color(Local.Generic_INACTIVE, Lib.Kolor.Yellow);
            }
        }

        public override void Ctrl(bool value) => Lib.ReflectionValue(Module, "IsRTActive", value);

        public override void Toggle() => Ctrl(!Lib.ReflectionValue<bool>(Module, "IsRTActive"));
    }

    public sealed class ProtoAntennaRTDevice : ProtoDevice<PartModule>
    {
        public ProtoAntennaRTDevice(PartModule prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
            : base(prefab, protoPart, protoModule)
        {
        }

        public override string Name => "antenna";

        public override string Status
        {
            get
            {
                return Lib.Proto.GetBool(ProtoModule, "IsRTActive")
                    ? Lib.Color(Local.Generic_ACTIVE, Lib.Kolor.Green)
                    : Lib.Color(Local.Generic_INACTIVE, Lib.Kolor.Yellow);
            }
        }

        public override void Ctrl(bool value) => Lib.Proto.Set(ProtoModule, "IsRTActive", value);

        public override void Toggle() => Ctrl(!Lib.Proto.GetBool(ProtoModule, "IsRTActive"));
    }
}