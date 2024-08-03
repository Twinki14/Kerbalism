using System.Collections.Generic;
using System.Linq;
using Kerbalism.Automation.Devices;
using Kerbalism.Automation.VesselDevices;
using Kerbalism.Database;
using Kerbalism.Modules;
using Kerbalism.System;

namespace Kerbalism.Automation
{
    public enum ScriptType
    {
        PowerLow = 1, // called when ec level goes below 15%
        PowerHigh = 2, // called when ec level goes above 15%
        Sunlight = 3, // called when sun rise
        Shadow = 4, // called when sun set
        Unlinked = 5, // called when signal is lost
        Linked = 6, // called when signal is regained
        DriveFull = 7, // called when storage capacity goes below 15%
        DriveEmpty = 8, // called when storage capacity goes above 30%
        Landed = 9, // called on landing
        Atmo = 10, // called on entering atmosphere
        Space = 11, // called on reaching space
        RadLow = 12, // called when radiation goes below 0.05 rad/h
        RadHigh = 13, // called when radiation goes above 0.05 rad/h
        EvaOut = 14, // called when going out on eva
        EvaIn = 15, // called when coming back from eva
        Action1 = 16, // called when pressing 1
        Action2 = 17, // called when pressing 2
        Action3 = 18, // called when pressing 3
        Action4 = 19, // called when pressing 4
        Action5 = 20, // called when pressing 5
        Last = 21
    }

    public sealed class Computer
    {
        private readonly Dictionary<ScriptType, Script> _scripts;

        public Computer(ConfigNode node)
        {
            _scripts = new Dictionary<ScriptType, Script>();

            if (node == null)
            {
                return;
            }

            // load scripts
            foreach (var scriptNode in node.GetNode("scripts").GetNodes())
            {
                _scripts.Add((ScriptType) Lib.Parse.ToUInt(scriptNode.name), new Script(scriptNode));
            }
        }

        public void Save(ConfigNode node)
        {
            // save scripts
            var scriptsNode = node.AddNode("scripts");
            foreach (var p in _scripts.Where(p => p.Value.States.Count != 0))
            {
                p.Value.Save(scriptsNode.AddNode(((uint) p.Key).ToString()));
            }
        }

        // get a script
        public Script Get(ScriptType type)
        {
            if (!_scripts.ContainsKey(type))
            {
                _scripts.Add(type, new Script());
            }

            return _scripts[type];
        }

        // execute a script
        public void Execute(Vessel v, ScriptType type)
        {
            // do nothing if there is no EC left on the vessel
            var ec = ResourceCache.GetResource(v, "ElectricCharge");

            if (ec.Amount <= double.Epsilon)
            {
                return;
            }

            // get the script
            if (!_scripts.TryGetValue(type, out var script))
            {
                return;
            }

            // execute the script
            script.Execute(GetModuleDevices(v));

            // show message to the user
            // - unless the script is empty (can happen when being edited)
            if (script.States.Count > 0 && v.KerbalismData().cfg_script)
            {
                Message.Post(Lib.BuildString(Local.UI_scriptvessel, " <b>", v.vesselName, "</b>"));
            }
        }

        // call scripts automatically when conditions are met
        public void Automate(Vessel v, VesselData vd, VesselResources resources)
        {
            // do nothing if automation is disabled
            if (!Features.Automation)
            {
                return;
            }

            // get current states
            var ec = resources.GetResource(v, "ElectricCharge");
            var sunlight = !vd.EnvInFullShadow;
            var powerLow = ec.Level < 0.2;
            var powerHigh = ec.Level > 0.8;
            var radiationLow = vd.EnvRadiation < 0.000005552; //< 0.02 rad/h
            var radiationHigh = vd.EnvRadiation > 0.00001388; //< 0.05 rad/h
            var signal = vd.Connection.linked;
            var driveFull = vd.DrivesFreeSpace < double.MaxValue && (vd.DrivesFreeSpace / vd.DrivesCapacity < 0.15);
            var driveEmpty = vd.DrivesFreeSpace >= double.MaxValue || (vd.DrivesFreeSpace / vd.DrivesCapacity > 0.9);

            // get current situation
            var landed = false;
            var atmo = false;
            var space = false;

            switch (v.situation)
            {
                case Vessel.Situations.LANDED:
                case Vessel.Situations.SPLASHED:
                    landed = true;
                    break;

                case Vessel.Situations.FLYING:
                    atmo = true;
                    break;

                case Vessel.Situations.SUB_ORBITAL:
                case Vessel.Situations.ORBITING:
                case Vessel.Situations.ESCAPING:
                    space = true;
                    break;
            }

            // compile list of scripts that need to be called
            var toExec = new List<Script>();
            foreach (var p in _scripts)
            {
                var type = p.Key;
                var script = p.Value;
                if (script.States.Count == 0) continue; //< skip empty scripts (may happen during editing)

                switch (type)
                {
                    case ScriptType.Landed:
                        if (landed && script.Prev == "0") toExec.Add(script);
                        script.Prev = landed ? "1" : "0";
                        break;

                    case ScriptType.Atmo:
                        if (atmo && script.Prev == "0") toExec.Add(script);
                        script.Prev = atmo ? "1" : "0";
                        break;

                    case ScriptType.Space:
                        if (space && script.Prev == "0") toExec.Add(script);
                        script.Prev = space ? "1" : "0";
                        break;

                    case ScriptType.Sunlight:
                        if (sunlight && script.Prev == "0") toExec.Add(script);
                        script.Prev = sunlight ? "1" : "0";
                        break;

                    case ScriptType.Shadow:
                        if (!sunlight && script.Prev == "0") toExec.Add(script);
                        script.Prev = !sunlight ? "1" : "0";
                        break;

                    case ScriptType.PowerHigh:
                        if (powerHigh && script.Prev == "0") toExec.Add(script);
                        script.Prev = powerHigh ? "1" : "0";
                        break;

                    case ScriptType.PowerLow:
                        if (powerLow && script.Prev == "0") toExec.Add(script);
                        script.Prev = powerLow ? "1" : "0";
                        break;

                    case ScriptType.RadLow:
                        if (radiationLow && script.Prev == "0") toExec.Add(script);
                        script.Prev = radiationLow ? "1" : "0";
                        break;

                    case ScriptType.RadHigh:
                        if (radiationHigh && script.Prev == "0") toExec.Add(script);
                        script.Prev = radiationHigh ? "1" : "0";
                        break;
                    case ScriptType.Linked:
                        if (signal && script.Prev == "0") toExec.Add(script);
                        script.Prev = signal ? "1" : "0";
                        break;

                    case ScriptType.Unlinked:
                        if (!signal && script.Prev == "0") toExec.Add(script);
                        script.Prev = !signal ? "1" : "0";
                        break;

                    case ScriptType.DriveFull:
                        if (driveFull && script.Prev == "0") toExec.Add(script);
                        script.Prev = driveFull ? "1" : "0";
                        break;

                    case ScriptType.DriveEmpty:
                        if (driveEmpty && script.Prev == "0") toExec.Add(script);
                        script.Prev = driveEmpty ? "1" : "0";
                        break;
                }
            }

            // if there are scripts to call
            if (toExec.Count <= 0)
            {
                return;
            }

            // get list of devices
            // - we avoid creating it when there are no scripts to be executed, making its overall cost trivial
            var devices = GetModuleDevices(v);

            // execute all scripts
            foreach (var script in toExec)
            {
                script.Execute(devices);
            }

            // show message to the user
            if (v.KerbalismData().cfg_script)
            {
                Message.Post(Lib.BuildString("Script called on vessel <b>", v.vesselName, "</b>"));
            }
        }

        // return set of devices on a vessel
        // - the list is only valid for a single simulation step
        public static List<Device> GetModuleDevices(Vessel v)
        {
            var moduleDevices = Cache.VesselObjectsCache<List<Device>>(v, "computer");
            if (moduleDevices != null)
                return moduleDevices;

            moduleDevices = new List<Device>();

            // store device being added
            Device device;

            // loaded vessel
            if (v.loaded)
            {
                foreach (var m in Lib.FindModules<PartModule>(v))
                {
                    switch (m.moduleName)
                    {
                        case "Laboratory":
                            device = new LaboratoryDevice(m as Laboratory);
                            break;
                        case "Experiment":
                            device = new ExperimentDevice(m as Experiment);
                            break;
                        case "SolarPanelFixer":
                            device = new PanelDevice(m as SolarPanelFixer);
                            break;
                        case "ModuleLight":
                            device = new LightDevice(m as ModuleLight);
                            break;
                        case "ModuleColoredLensLight":
                            device = new LightDevice(m as ModuleLight);
                            break;
                        case "ModuleMultiPointSurfaceLight":
                            device = new LightDevice(m as ModuleLight);
                            break;
                        case "SCANsat":
                            device = new ScannerDevice(m);
                            break;
                        case "ModuleSCANresourceScanner":
                            device = new ScannerDevice(m);
                            break;
                        case "ModuleDataTransmitter":
                        case "ModuleDataTransmitterFeedeable":
                            device = new AntennaDevice(m as ModuleDataTransmitter);
                            break;
                        case "ModuleRTAntenna":
                        case "ModuleRTAntennaPassive":
                            device = new AntennaRTDevice(m);
                            break;
                        case "KerbalismSentinel":
                            device = new SentinelDevice(m as KerbalismSentinel);
                            break;
                        default: continue;
                    }

                    // add the device
                    moduleDevices.Add(device);
                }
            }
            // unloaded vessel
            else
            {
                // store data required to support multiple modules of same type in a part
                var pd = new Dictionary<string, Lib.Module_prefab_data>();

                // for each part
                foreach (var p in v.protoVessel.protoPartSnapshots)
                {
                    // get part prefab (required for module properties)
                    var partPrefab = PartLoader.getPartInfoByName(p.partName).partPrefab;

                    // get all module prefabs
                    var modulePrefabs = partPrefab.FindModulesImplementing<PartModule>();

                    // clear module indexes
                    pd.Clear();

                    // for each module
                    foreach (var m in p.modules)
                    {
                        // get the module prefab
                        // if the prefab doesn't contain this module, skip it
                        var modulePrefab = Lib.ModulePrefab(modulePrefabs, m.moduleName, pd);
                        if (!modulePrefab) continue;

                        // if the module is disabled, skip it
                        // note: this must be done after ModulePrefab is called, so that indexes are right
                        if (!Lib.Proto.GetBool(m, "isEnabled")) continue;

                        // depending on module name
                        switch (m.moduleName)
                        {
                            case "Laboratory":
                                device = new ProtoLaboratoryDevice(modulePrefab as Laboratory, p, m);
                                break;
                            case "Experiment":
                                device = new ProtoExperimentDevice(modulePrefab as Experiment, p, m, v);
                                break;
                            case "SolarPanelFixer":
                                device = new ProtoPanelDevice(modulePrefab as SolarPanelFixer, p, m);
                                break;
                            case "ModuleLight":
                            case "ModuleColoredLensLight":
                            case "ModuleMultiPointSurfaceLight":
                                device = new ProtoLightDevice(modulePrefab as ModuleLight, p, m);
                                break;
                            case "SCANsat":
                                device = new ProtoScannerDevice(modulePrefab, p, m, v);
                                break;
                            case "ModuleSCANresourceScanner":
                                device = new ProtoScannerDevice(modulePrefab, p, m, v);
                                break;
                            case "ModuleDataTransmitter":
                            case "ModuleDataTransmitterFeedeable":
                                device = new ProtoAntennaDevice(modulePrefab as ModuleDataTransmitter, p, m);
                                break;
                            case "ModuleRTAntenna":
                            case "ModuleRTAntennaPassive":
                                device = new ProtoAntennaRTDevice(modulePrefab, p, m);
                                break;
                            case "KerbalismSentinel":
                                device = new ProtoSentinelDevice(modulePrefab as KerbalismSentinel, p, m, v);
                                break;
                            default: continue;
                        }

                        // add the device
                        moduleDevices.Add(device);
                    }
                }
            }

            // return all found module devices sorted by type, then by name
            // in reverse (the list will be presented from end to start in the UI)
            moduleDevices.Sort((b, a) =>
            {
                var xdiff = a.DeviceType.CompareTo(b.DeviceType);

                return xdiff != 0 ? xdiff : a.Name.CompareTo(b.Name);
            });

            // now add vessel wide devices to the end of the list
            var vd = v.KerbalismData();

            moduleDevices.Add(new VesselDeviceTransmit(v, vd)); // vessel wide transmission toggle

            Cache.SetVesselObjectsCache(v, "computer", moduleDevices);
            return moduleDevices;
        }
    }
}
