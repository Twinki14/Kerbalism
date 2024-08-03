using Kerbalism.Modules;

namespace Kerbalism.Automation.Devices
{
    public sealed class LaboratoryDevice : LoadedDevice<Laboratory>
    {
        public LaboratoryDevice(Laboratory module) : base(module)
        {
        }

        public override string Status => Lib.Color(Module.running, Local.Generic_ACTIVE, Lib.Kolor.Green,
            Local.Generic_DISABLED, Lib.Kolor.Yellow);

        public override void Ctrl(bool value)
        {
            if (Module.running != value) Module.Toggle();
        }

        public override void Toggle()
        {
            Ctrl(!Module.running);
        }
    }


    public sealed class ProtoLaboratoryDevice : ProtoDevice<Laboratory>
    {
        public ProtoLaboratoryDevice(Laboratory prefab, ProtoPartSnapshot protoPart,
            ProtoPartModuleSnapshot protoModule)
            : base(prefab, protoPart, protoModule)
        {
        }

        public override string Status => Lib.Color(Lib.Proto.GetBool(ProtoModule, "running"), Local.Generic_ACTIVE,
            Lib.Kolor.Green, Local.Generic_DISABLED, Lib.Kolor.Yellow);

        public override void Ctrl(bool value)
        {
            Lib.Proto.Set(ProtoModule, "running", value);
        }

        public override void Toggle()
        {
            Ctrl(!Lib.Proto.GetBool(ProtoModule, "running"));
        }
    }
} // KERBALISM