namespace Kerbalism.Automation.Devices
{
    public sealed class LightDevice : LoadedDevice<ModuleLight>
    {
        public LightDevice(ModuleLight module) : base(module)
        {
        }

        public override string Name => "light";

        public override string Status => Lib.Color(Module.isOn, Local.Generic_ON, Lib.Kolor.Green, Local.Generic_OFF,
            Lib.Kolor.Yellow);

        public override void Ctrl(bool value)
        {
            if (value) Module.LightsOn();
            else Module.LightsOff();
        }

        public override void Toggle()
        {
            Ctrl(!Module.isOn);
        }
    }


    public sealed class ProtoLightDevice : ProtoDevice<ModuleLight>
    {
        public ProtoLightDevice(ModuleLight prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
            : base(prefab, protoPart, protoModule)
        {
        }

        public override string Name => "light";

        public override string Status => Lib.Color(Lib.Proto.GetBool(ProtoModule, "isOn"), Local.Generic_ON,
            Lib.Kolor.Green, Local.Generic_OFF, Lib.Kolor.Yellow);

        public override void Ctrl(bool value)
        {
            Lib.Proto.Set(ProtoModule, "isOn", value);
        }

        public override void Toggle()
        {
            Ctrl(!Lib.Proto.GetBool(ProtoModule, "isOn"));
        }
    }
} // KERBALISM