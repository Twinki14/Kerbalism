using System;
using Kerbalism.Database;
using UnityEngine;

namespace Kerbalism.Automation
{
    public abstract class Device
    {
        public class DeviceIcon
        {
            public readonly Texture2D Texture;
            public readonly string Tooltip;
            public readonly Action OnClick;

            public DeviceIcon(Texture2D texture, string tooltip = "", Action onClick = null)
            {
                Texture = texture;
                Tooltip = tooltip;
                OnClick = onClick;
            }
        }

        protected Device()
        {
            DeviceType = GetType().Name;
        }

        // note 1 : the ID must be unique and always the same (persistence), so the Name property must always be the
        // same, and be unique in case multiple modules of the same type exists on the part.
        // note 2 : dynamically generate the id when first requested.
        // can't do it in the base ctor because the PartId and Name may be overloaded.
        public uint Id
        {
            get
            {
                if (_id == uint.MaxValue)
                {
                    _id = PartId + (uint) Math.Abs(Name.GetHashCode());
                }

                return _id;
            }
        }

        private uint _id = uint.MaxValue; // let's just hope nothing will ever have that id

        public string DeviceType { get; private set; }

        // return device name, must be static and unique in case several modules of the same type are on the part
        public abstract string Name { get; }

        // the name that will be displayed. can be overloaded in case some dynamic text is added (see experiments)
        public virtual string DisplayName => Name;

        // return part id
        public abstract uint PartId { get; }

        // return part name
        protected abstract string PartName { get; }

        // return short device status string
        public abstract string Status { get; }

        // return tooltip string
        public virtual string Tooltip => Lib.BuildString(Lib.Bold(DisplayName), "\non ", PartName);

        // return icon/button
        public virtual DeviceIcon Icon => null;

        // control the device using a value
        public abstract void Ctrl(bool value);

        // toggle the device state
        public abstract void Toggle();

        public virtual bool IsVisible => true;

        public virtual void OnUpdate()
        {
        }
    }

    public abstract class LoadedDevice<T> : Device where T : PartModule
    {
        protected readonly T Module;

        protected LoadedDevice(T module)
        {
            Module = module;
        }

        protected override string PartName => Module.part.partInfo.title;
        public override string Name => Module is IModuleInfo info ? info.GetModuleTitle() : Module.GUIName;
        public override uint PartId => Module.part.flightID;
    }

    public abstract class ProtoDevice<T> : Device where T : PartModule
    {
        protected readonly T Prefab;
        protected readonly ProtoPartSnapshot ProtoPart;
        protected readonly ProtoPartModuleSnapshot ProtoModule;

        protected ProtoDevice(T prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
        {
            Prefab = prefab;
            ProtoPart = protoPart;
            ProtoModule = protoModule;
        }

        protected override string PartName => Prefab.part.partInfo.title;
        public override string Name => Prefab is IModuleInfo info ? info.GetModuleTitle() : Prefab.GUIName;
        public override uint PartId => ProtoPart.flightID;
    }

    public abstract class VesselDevice : Device
    {
        protected readonly VesselData VesselData;

        protected VesselDevice(VesselData vd)
        {
            VesselData = vd;
        }

        public override uint PartId => 0u;
        protected override string PartName => string.Empty;
        public override string Tooltip => Lib.Bold(DisplayName);
    }
}
