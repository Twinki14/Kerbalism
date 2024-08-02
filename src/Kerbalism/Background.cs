using System;
using System.Collections.Generic;
using System.Reflection;
using Kerbalism.Database;
using Kerbalism.Modules;

namespace Kerbalism
{
    public static class Background
    {
        private class BackgroundDelegate
        {
            private static Type[] signature =
            {
                typeof(Vessel), typeof(ProtoPartSnapshot), typeof(ProtoPartModuleSnapshot), typeof(PartModule),
                typeof(Part), typeof(Dictionary<string, double>), typeof(List<KeyValuePair<string, double>>),
                typeof(double)
            };

#if KSP18
			// non-generic actions are too new to be used in pre-KSP18
			internal Func<Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, Dictionary<string, double>, List<KeyValuePair<string, double>>, double, string> function;
#else
            internal MethodInfo methodInfo;
#endif
            private BackgroundDelegate(MethodInfo methodInfo)
            {
#if KSP18
				function =
 (Func<Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, Dictionary<string, double>, List<KeyValuePair<string, double>>, double, string>)Delegate.CreateDelegate(typeof(Func<Vessel, ProtoPartSnapshot, ProtoPartModuleSnapshot, PartModule, Part, Dictionary<string, double>, List<KeyValuePair<string, double>>, double, string>), methodInfo);
#else
                this.methodInfo = methodInfo;
#endif
            }

            public string invoke(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, PartModule module_prefab,
                Part part_prefab, Dictionary<string, double> availableRresources,
                List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
            {
                // TODO optimize this for performance
#if KSP18
				var result =
 function(v, p, m, module_prefab, part_prefab, availableRresources, resourceChangeRequest, elapsed_s);
				if (string.IsNullOrEmpty(result)) result = module_prefab.moduleName;
				return result;
#else
                var result = methodInfo.Invoke(null,
                    new object[]
                        {v, p, m, module_prefab, part_prefab, availableRresources, resourceChangeRequest, elapsed_s});
                if (result == null) return module_prefab.moduleName;
                return result.ToString();
#endif
            }

            public static BackgroundDelegate Instance(PartModule module_prefab)
            {
                BackgroundDelegate result = null;

                var type = module_prefab.GetType();
                supportedModules.TryGetValue(type, out result);
                if (result != null) return result;

                if (unsupportedModules.Contains(type)) return null;

                MethodInfo methodInfo = type.GetMethod("BackgroundUpdate", signature);
                if (methodInfo == null)
                {
                    unsupportedModules.Add(type);
                    return null;
                }

                result = new BackgroundDelegate(methodInfo);
                supportedModules[type] = result;
                return result;
            }

            private static readonly Dictionary<Type, BackgroundDelegate> supportedModules =
                new Dictionary<Type, BackgroundDelegate>();

            private static readonly List<Type> unsupportedModules = new List<Type>();
        }

        public enum Module_type
        {
            Experiment = 1,
            Laboratory = 5,
            Command = 6,
            StockLab = 11,
            Light = 12,
            Scanner = 13,
            Unknown = 17,
            KerbalismProcess = 20,
            SolarPanelFixer = 21,
            KerbalismSentinel = 22,

            /// <summary>Module implementing the kerbalism background API</summary>
            APIModule
        }

        public static Module_type ModuleType(string module_name)
        {
            switch (module_name)
            {
                case "Experiment": return Module_type.Experiment;
                case "Laboratory": return Module_type.Laboratory;
                case "ModuleCommand": return Module_type.Command;
                case "ModuleScienceConverter": return Module_type.StockLab;
                case "ModuleLight":
                case "ModuleColoredLensLight":
                case "ModuleMultiPointSurfaceLight": return Module_type.Light;
                case "KerbalismScansat": return Module_type.Scanner;
                case "KerbalismProcess": return Module_type.KerbalismProcess;
                case "SolarPanelFixer": return Module_type.SolarPanelFixer;
                case "KerbalismSentinel": return Module_type.KerbalismSentinel;
            }

            return Module_type.Unknown;
        }

        internal class BackgroundPM
        {
            internal ProtoPartSnapshot p;
            internal ProtoPartModuleSnapshot m;
            internal PartModule module_prefab;
            internal Part part_prefab;
            internal Module_type type;
        }

        public static void Update(Vessel v, VesselData vd, VesselResources resources, double elapsed_s)
        {
            if (!Lib.IsVessel(v))
                return;

            // get most used resource handlers
            ResourceInfo ec = resources.GetResource(v, "ElectricCharge");

            List<ResourceInfo> allResources = resources.GetAllResources(v);
            Dictionary<string, double> availableResources = new Dictionary<string, double>();
            foreach (var ri in allResources)
                availableResources[ri.ResourceName] = ri.Amount;
            List<KeyValuePair<string, double>> resourceChangeRequests = new List<KeyValuePair<string, double>>();

            foreach (var e in Background_PMs(v))
            {
                switch (e.type)
                {
                    case Module_type.Experiment:
                        (e.module_prefab as Experiment).BackgroundUpdate(v, vd, e.m, ec, resources, elapsed_s);
                        break; // experiments use the prefab as a singleton instead of a static method
                    case Module_type.Laboratory:
                        Laboratory.BackgroundUpdate(v, e.p, e.m, e.module_prefab as Laboratory, ec, elapsed_s);
                        break;
                    case Module_type.Command:
                        ProcessCommand(v, e.p, e.m, e.module_prefab as ModuleCommand, resources, elapsed_s);
                        break;
                    case Module_type.StockLab:
                        ProcessStockLab(v, e.p, e.m, e.module_prefab as ModuleScienceConverter, ec, elapsed_s);
                        break;
                    case Module_type.Light:
                        ProcessLight(v, e.p, e.m, e.module_prefab as ModuleLight, ec, elapsed_s);
                        break;
                    case Module_type.Scanner:
                        KerbalismScansat.BackgroundUpdate(v, e.p, e.m, e.module_prefab as KerbalismScansat,
                            e.part_prefab, vd, ec, elapsed_s);
                        break;
                    case Module_type.SolarPanelFixer:
                        SolarPanelFixer.BackgroundUpdate(v, e.m, e.module_prefab as SolarPanelFixer, vd, ec, elapsed_s);
                        break;
                    case Module_type.KerbalismSentinel:
                        KerbalismSentinel.BackgroundUpdate(v, e.m, e.module_prefab as KerbalismSentinel, vd, ec,
                            elapsed_s);
                        break;
                    case Module_type.APIModule:
                        ProcessApiModule(v, e.p, e.m, e.part_prefab, e.module_prefab, resources, availableResources,
                            resourceChangeRequests, elapsed_s);
                        break;
                }
            }
        }

        private static List<BackgroundPM> Background_PMs(Vessel v)
        {
            var result = Cache.VesselObjectsCache<List<BackgroundPM>>(v, "background");
            if (result != null)
                return result;

            result = new List<BackgroundPM>();

            // store data required to support multiple modules of same type in a part
            var PD = new Dictionary<string, Lib.Module_prefab_data>();

            // for each part
            foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
            {
                // get part prefab (required for module properties)
                Part part_prefab = PartLoader.getPartInfoByName(p.partName).partPrefab;

                // get all module prefabs
                var module_prefabs = part_prefab.FindModulesImplementing<PartModule>();

                // clear module indexes
                PD.Clear();

                // for each module
                foreach (ProtoPartModuleSnapshot m in p.modules)
                {
                    // TODO : this is to migrate pre-3.1 saves using WarpFixer to the new SolarPanelFixer. At some point in the future we can remove this code.
                    if (m.moduleName == "WarpFixer") MigrateWarpFixer(v, part_prefab, p, m);

                    // get the module prefab
                    // if the prefab doesn't contain this module, skip it
                    PartModule module_prefab = Lib.ModulePrefab(module_prefabs, m.moduleName, PD);
                    if (!module_prefab) continue;

                    // if the module is disabled, skip it
                    // note: this must be done after ModulePrefab is called, so that indexes are right
                    if (!Lib.Proto.GetBool(m, "isEnabled")) continue;

                    // get module type
                    // if the type is unknown, skip it
                    Module_type type = ModuleType(m.moduleName);
                    if (type == Module_type.Unknown)
                    {
                        var backgroundDelegate = BackgroundDelegate.Instance(module_prefab);
                        if (backgroundDelegate != null)
                            type = Module_type.APIModule;
                        else
                            continue;
                    }

                    var entry = new BackgroundPM();
                    entry.p = p;
                    entry.m = m;
                    entry.module_prefab = module_prefab;
                    entry.part_prefab = part_prefab;
                    entry.type = type;
                    result.Add(entry);
                }
            }

            Cache.SetVesselObjectsCache(v, "background", result);
            return result;
        }

        private static void ProcessApiModule(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m,
            Part part_prefab, PartModule module_prefab, VesselResources resources,
            Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequests,
            double elapsed_s)
        {
            resourceChangeRequests.Clear();

            try
            {
                string title = BackgroundDelegate.Instance(module_prefab).invoke(v, p, m, module_prefab, part_prefab,
                    availableResources, resourceChangeRequests, elapsed_s);

                foreach (var cr in resourceChangeRequests)
                {
                    if (cr.Value > 0)
                        resources.Produce(v, cr.Key, cr.Value * elapsed_s, ResourceBroker.GetOrCreate(title));
                    else if (cr.Value < 0)
                        resources.Consume(v, cr.Key, -cr.Value * elapsed_s, ResourceBroker.GetOrCreate(title));
                }
            }
            catch (Exception ex)
            {
                Lib.Log("BackgroundUpdate in PartModule " + module_prefab.moduleName + " excepted: " + ex.Message +
                        "\n" + ex.ToString());
            }
        }

        static void ProcessCommand(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, ModuleCommand command,
            VesselResources resources, double elapsed_s)
        {
            // do not consume if this is a MCM with no crew
            // rationale: for consistency, the game doesn't consume resources for MCM without crew in loaded vessels
            //            this make some sense: you left a vessel with some battery and nobody on board, you expect it to not consume EC
            if (command.minimumCrew == 0 || p.protoModuleCrew.Count > 0)
            {
                // for each input resource
                foreach (ModuleResource ir in command.resHandler.inputResources)
                {
                    // consume the resource
                    resources.Consume(v, ir.name, ir.rate * elapsed_s, ResourceBroker.Command);
                }
            }
        }

        static void ProcessStockLab(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m,
            ModuleScienceConverter lab, ResourceInfo ec, double elapsed_s)
        {
            // note: we are only simulating the EC consumption
            // note: there is no easy way to 'stop' the lab when there isn't enough EC

            // if active
            if (Lib.Proto.GetBool(m, "IsActivated"))
            {
                // consume ec
                ec.Consume(lab.powerRequirement * elapsed_s, ResourceBroker.ScienceLab);
            }
        }


        static void ProcessLight(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, ModuleLight light,
            ResourceInfo ec, double elapsed_s)
        {
            if (light.useResources && Lib.Proto.GetBool(m, "isOn"))
            {
                ec.Consume(light.resourceAmount * elapsed_s, ResourceBroker.Light);
            }
        }

        // TODO - Kerbalism - Forked Science
        // TODO - This can be completely removed, Forked Science is a new mod with new versioning & requirements
        // TODO - It won't be compatible with old Kerbalism saves anyway
        // -------
        // TODO : this is to migrate pre-3.1 saves using WarpFixer to the new SolarPanelFixer. At some point in the future we can remove this code.
        static void MigrateWarpFixer(Vessel v, Part prefab, ProtoPartSnapshot p, ProtoPartModuleSnapshot m)
        {
            ModuleDeployableSolarPanel panelModule = prefab.FindModuleImplementing<ModuleDeployableSolarPanel>();
            ProtoPartModuleSnapshot protoPanelModule =
                p.modules.Find(pm => pm.moduleName == "ModuleDeployableSolarPanel");

            if (panelModule == null || protoPanelModule == null)
            {
                Lib.Log("Vessel " + v.name +
                        " has solar panels that can't be converted automatically following Kerbalism 3.1 update. Load it to fix the issue.");
                return;
            }

            SolarPanelFixer.PanelState state = SolarPanelFixer.PanelState.Unknown;
            string panelStateStr = Lib.Proto.GetString(protoPanelModule, "deployState");

            if (!Enum.IsDefined(typeof(ModuleDeployablePart.DeployState), panelStateStr)) return;
            ModuleDeployablePart.DeployState panelState =
                (ModuleDeployablePart.DeployState) Enum.Parse(typeof(ModuleDeployablePart.DeployState), panelStateStr);

            if (panelState == ModuleDeployablePart.DeployState.BROKEN)
                state = SolarPanelFixer.PanelState.Broken;
            else if (!panelModule.isTracking)
            {
                state = SolarPanelFixer.PanelState.Static;
            }
            else
            {
                switch (panelState)
                {
                    case ModuleDeployablePart.DeployState.EXTENDED:
                        if (!panelModule.retractable)
                            state = SolarPanelFixer.PanelState.ExtendedFixed;
                        else
                            state = SolarPanelFixer.PanelState.Extended;
                        break;
                    case ModuleDeployablePart.DeployState.RETRACTED:
                        state = SolarPanelFixer.PanelState.Retracted;
                        break;
                    case ModuleDeployablePart.DeployState.RETRACTING:
                        state = SolarPanelFixer.PanelState.Retracting;
                        break;
                    case ModuleDeployablePart.DeployState.EXTENDING:
                        state = SolarPanelFixer.PanelState.Extending;
                        break;
                    default:
                        state = SolarPanelFixer.PanelState.Unknown;
                        break;
                }
            }

            m.moduleName = "SolarPanelFixer";
            Lib.Proto.Set(m, "state", state);
            Lib.Proto.Set(m, "persistentFactor", 0.75);
            Lib.Proto.Set(m, "launchUT", Planetarium.GetUniversalTime());
            Lib.Proto.Set(m, "nominalRate", panelModule.chargeRate);
        }
    }
} // KERBALISM
