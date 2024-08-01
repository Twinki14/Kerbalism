using System;
using System.Reflection;
using System.Collections.Generic;
using Kerbalism.Modules;
using Kerbalism.System;
using ModuleWheels;

namespace Kerbalism.Planner
{
    ///<summary> Planners simulator for resources contained, produced and consumed within the vessel </summary>
    public class ResourceSimulator
    {
        private class PlannerDelegate
        {
            internal MethodInfo methodInfo = null;
            internal IKerbalismModule module = null;

            public PlannerDelegate(IKerbalismModule module)
            {
                this.module = module;
            }

            public PlannerDelegate(MethodInfo methodInfo)
            {
                this.methodInfo = methodInfo;
            }

            internal string Invoke(PartModule m, List<KeyValuePair<string, double>> resourcesList, CelestialBody body,
                Dictionary<string, double> environment)
            {
                IKerbalismModule km = m as IKerbalismModule;
                if (km != null)
                {
                    return km.PlannerUpdate(resourcesList, body, environment);
                }

                var result = methodInfo.Invoke(m, new object[] {resourcesList, body, environment});
                if (result != null) return result.ToString();
                return "unknown";
            }
        }

        private static readonly Dictionary<string, PlannerDelegate> apiDelegates =
            new Dictionary<string, PlannerDelegate>();

        private static readonly List<string> unsupportedModules = new List<string>();

        private static Type[] plannerMethodSignature =
            {typeof(List<KeyValuePair<string, double>>), typeof(CelestialBody), typeof(Dictionary<string, double>)};

        /// <summary>
        /// run simulator to get statistics a fraction of a second after the vessel would spawn
        /// in the configured environment (celestial body, orbit height and presence of sunlight)
        /// </summary>
        public void Analyze(List<Part> parts, EnvironmentAnalyzer env, VesselAnalyzer va)
        {
            // reach steady state, so all initial resources like WasteAtmosphere are produced
            // it is assumed that one cycle is needed to produce things that don't need inputs
            // another cycle is needed for processes to pick that up
            // another cycle may be needed for results of those processes to be picked up
            // two additional cycles are for having some margin
            for (int i = 0; i < 5; i++)
            {
                RunSimulator(parts, env, va);
            }

            // Do the actual run people will see from the simulator UI
            foreach (SimulatedResource r in resources.Values)
            {
                r.ResetSimulatorDisplayValues();
            }

            RunSimulator(parts, env, va);
        }

        /// <summary>run a single timestamp of the simulator</summary>
        private void RunSimulator(List<Part> parts, EnvironmentAnalyzer env, VesselAnalyzer va)
        {
            // clear previous resource state
            resources.Clear();

            // get amount and capacity from parts
            foreach (Part p in parts)
            {
                for (int i = 0; i < p.Resources.Count; ++i)
                {
                    Process_part(p, p.Resources[i].resourceName);
#if DEBUG_RESOURCES
					p.Resources[i].isVisible = true;
					p.Resources[i].isTweakable = true;
#endif
                }
            }

            // process all modules
            foreach (Part p in parts)
            {
                // get planner controller in the part
                PlannerController ctrl = p.FindModuleImplementing<PlannerController>();

                // ignore all modules in the part if specified in controller
                if (ctrl != null && !ctrl.considered)
                    continue;

                // for each module
                foreach (PartModule m in p.Modules)
                {
                    // skip disabled modules
                    // rationale: the Selector disable non-selected modules in this way
                    if (!m.isEnabled)
                        continue;

                    if (IsModuleKerbalismAware(m))
                    {
                        Process_apiModule(m, env, va);
                    }
                    else
                    {
                        switch (m.moduleName)
                        {
                            case "Laboratory":
                                Process_laboratory(m as Laboratory);
                                break;
                            case "Experiment":
                                Process_experiment(m as Experiment);
                                break;
                            case "ModuleCommand":
                                Process_command(m as ModuleCommand);
                                break;
                            case "ModuleScienceConverter":
                                Process_stocklab(m as ModuleScienceConverter);
                                break;
                            case "ModuleActiveRadiator":
                                Process_radiator(m as ModuleActiveRadiator);
                                break;
                            case "ModuleWheelMotor":
                                Process_wheel_motor(m as ModuleWheelMotor);
                                break;
                            case "ModuleWheelMotorSteering":
                                Process_wheel_steering(m as ModuleWheelMotorSteering);
                                break;
                            case "ModuleLight":
                            case "ModuleColoredLensLight":
                            case "ModuleMultiPointSurfaceLight":
                                Process_light(m as ModuleLight);
                                Process_light(m as ModuleLight);
                                Process_light(m as ModuleLight);
                                break;
                            case "KerbalismScansat":
                                Process_scanner(m as KerbalismScansat);
                                break;
                            case "ModuleRTAntennaPassive":
                                Process_rtantenna(m);
                                break;
                            //case "ModuleRTAntenna":
                            case "AntennaDataTransmitterRemoteTech":
                                Process_rtantenna_transmitter(m as AntennaDataTransmitterRemoteTech);
                                break;
                            case "ModuleDataTransmitter":
                            case "ModuleDataTransmitterFeedeable": // NearFutureExploration derivative
                                Process_datatransmitter(m as ModuleDataTransmitter);
                                break;
                            case "ModuleEngines":
                                Process_engines(m as ModuleEngines);
                                break;
                            case "ModuleEnginesFX":
                                Process_enginesfx(m as ModuleEnginesFX);
                                break;
                            case "ModuleRCS":
                                Process_rcs(m as ModuleRCS);
                                break;
                            case "ModuleRCSFX":
                                Process_rcsfx(m as ModuleRCSFX);
                                break;
                            case "SolarPanelFixer":
                                Process_solarPanel(m as SolarPanelFixer, env);
                                break;
                        }
                    }
                }
            }

            // execute all possible recipes
            bool executing = true;
            while (executing)
            {
                executing = false;
                for (int i = 0; i < recipes.Count; ++i)
                {
                    SimulatedRecipe recipe = recipes[i];
                    if (recipe.left > double.Epsilon)
                    {
                        executing |= recipe.Execute(this);
                    }
                }
            }

            recipes.Clear();

            // clamp all resources
            foreach (KeyValuePair<string, SimulatedResource> pair in resources)
                pair.Value.Clamp();
        }

        private void Process_apiModule(PartModule m, EnvironmentAnalyzer env, VesselAnalyzer va)
        {
            List<KeyValuePair<string, double>> resourcesList = new List<KeyValuePair<string, double>>();

            Dictionary<string, double> environment = new Dictionary<string, double>();
            environment["altitude"] = env.altitude;
            environment["orbital_period"] = env.orbital_period;
            environment["shadow_period"] = env.shadow_period;
            environment["shadow_time"] = env.shadow_time;
            environment["albedo_flux"] = env.albedo_flux;
            environment["solar_flux"] = env.solar_flux;
            environment["sun_dist"] = env.sun_dist;
            environment["temperature"] = env.temperature;
            environment["total_flux"] = env.total_flux;
            environment["temperature"] = env.temperature;
            environment["sunlight"] = Planner.Sunlight == Planner.SunlightState.Shadow ? 0 : 1;

            Lib.Log("resource count before call " + resourcesList.Count);
            string title;
            IKerbalismModule km = m as IKerbalismModule;
            if (km != null)
                title = km.PlannerUpdate(resourcesList, env.body, environment);
            else
                title = apiDelegates[m.moduleName].Invoke(m, resourcesList, env.body, environment);
            Lib.Log("resource count after call " + resourcesList.Count);

            foreach (var p in resourcesList)
            {
                var res = Resource(p.Key);
                if (p.Value >= 0)
                    res.Produce(p.Value, title);
                else
                    res.Consume(-p.Value, title);
            }
        }

        private bool IsModuleKerbalismAware(PartModule m)
        {
            if (m is IKerbalismModule) return true;

            if (apiDelegates.ContainsKey(m.moduleName)) return true;
            if (unsupportedModules.Contains(m.moduleName)) return false;

            MethodInfo methodInfo = m.GetType().GetMethod("PlannerUpdate", plannerMethodSignature);
            if (methodInfo == null)
            {
                unsupportedModules.Add(m.moduleName);
                return false;
            }

            apiDelegates[m.moduleName] = new PlannerDelegate(methodInfo);
            return true;
        }

        /// <summary>obtain information on resource metrics for any resource contained within simulated vessel</summary>
        public SimulatedResource Resource(string name)
        {
            SimulatedResource res;
            if (!resources.TryGetValue(name, out res))
            {
                res = new SimulatedResource(name);
                resources.Add(name, res);
            }

            return res;
        }

        /// <summary>transfer per-part resources to the simulator</summary>
        void Process_part(Part p, string res_name)
        {
            SimulatedResourceView res = Resource(res_name).GetSimulatedResourceView(p);
            res.AddPartResources(p);
        }

        void Process_laboratory(Laboratory lab)
        {
            // note: we are not checking if there is a scientist in the part
            if (lab.running)
            {
                Resource("ElectricCharge").Consume(lab.ec_rate, "laboratory");
            }
        }


        void Process_experiment(Experiment exp)
        {
            if (exp.Running)
            {
                Resource("ElectricCharge")
                    .Consume(exp.ec_rate, exp.ExpInfo.SampleMass == 0.0 ? "sensor" : "experiment");
            }
        }


        void Process_command(ModuleCommand command)
        {
            foreach (ModuleResource res in command.resHandler.inputResources)
            {
                Resource(res.name).Consume(res.rate, "command");
            }
        }


        void Process_stocklab(ModuleScienceConverter lab)
        {
            Resource("ElectricCharge").Consume(lab.powerRequirement, "lab");
        }


        void Process_radiator(ModuleActiveRadiator radiator)
        {
            // note: IsCooling is not valid in the editor, for deployable radiators,
            // we will have to check if the related deploy module is deployed
            // we use PlannerController instead
            foreach (ModuleResource res in radiator.resHandler.inputResources)
            {
                Resource(res.name).Consume(res.rate, "radiator");
            }
        }


        void Process_wheel_motor(ModuleWheelMotor motor)
        {
            foreach (ModuleResource res in motor.resHandler.inputResources)
            {
                Resource(res.name).Consume(res.rate, "wheel");
            }
        }


        void Process_wheel_steering(ModuleWheelMotorSteering steering)
        {
            foreach (ModuleResource res in steering.resHandler.inputResources)
            {
                Resource(res.name).Consume(res.rate, "wheel");
            }
        }


        void Process_light(ModuleLight light)
        {
            if (light.useResources && light.isOn)
            {
                Resource("ElectricCharge").Consume(light.resourceAmount, "light");
            }
        }


        void Process_scanner(KerbalismScansat m)
        {
            Resource("ElectricCharge").Consume(m.ec_rate, "scanner");
        }

        void Process_rtantenna(PartModule m)
        {
            Resource("ElectricCharge").Consume(0.0005, "communications (control)"); // 3km range needs approx 0.5 Watt
        }

        // following the naming conventions that I see here.
        void Process_rtantenna_transmitter(AntennaDataTransmitterRemoteTech adt)
        {
            Resource("ElectricCharge").Consume(adt.energyCost, "communications (idle)");
            Resource("ElectricCharge").Consume(adt.packetResourceCost * adt.packetSize / adt.packetInterval,
                "communications (transmitting)");
        }

        void Process_datatransmitter(ModuleDataTransmitter mdt)
        {
            switch (mdt.antennaType)
            {
                case AntennaType.INTERNAL:
                    Resource("ElectricCharge")
                        .Consume(mdt.DataResourceCost * mdt.DataRate * Settings.TransmitterPassiveEcFactor,
                            "communications (idle)");
                    break;
                default:
                    Resource("ElectricCharge")
                        .Consume(mdt.DataResourceCost * mdt.DataRate * Settings.TransmitterActiveEcFactor,
                            "communications (transmitting)");
                    break;
            }
        }

        void Process_engines(ModuleEngines me)
        {
            // calculate thrust fuel flow
            double thrust_flow = me.maxFuelFlow * 1e3 * me.thrustPercentage;

            // search fuel types
            foreach (Propellant fuel in me.propellants)
            {
                switch (fuel.name)
                {
                    case "ElectricCharge": // mainly used for Ion Engines
                        Resource("ElectricCharge").Consume(thrust_flow * fuel.ratio, "engines");
                        break;
                    case "LqdHydrogen": // added for cryotanks and any other supported mod that uses Liquid Hydrogen
                        Resource("LqdHydrogen").Consume(thrust_flow * fuel.ratio, "engines");
                        break;
                }
            }
        }

        void Process_enginesfx(ModuleEnginesFX mefx)
        {
            // calculate thrust fuel flow
            double thrust_flow = mefx.maxFuelFlow * 1e3 * mefx.thrustPercentage;

            // search fuel types
            foreach (Propellant fuel in mefx.propellants)
            {
                switch (fuel.name)
                {
                    case "ElectricCharge": // mainly used for Ion Engines
                        Resource("ElectricCharge").Consume(thrust_flow * fuel.ratio, "engines");
                        break;
                    case "LqdHydrogen": // added for cryotanks and any other supported mod that uses Liquid Hydrogen
                        Resource("LqdHydrogen").Consume(thrust_flow * fuel.ratio, "engines");
                        break;
                }
            }
        }

        void Process_rcs(ModuleRCS mr)
        {
            // calculate thrust fuel flow
            double thrust_flow = mr.maxFuelFlow * 1e3 * mr.thrustPercentage * mr.thrusterPower;

            // search fuel types
            foreach (Propellant fuel in mr.propellants)
            {
                switch (fuel.name)
                {
                    case "ElectricCharge": // mainly used for Ion RCS
                        Resource("ElectricCharge").Consume(thrust_flow * fuel.ratio, "rcs");
                        break;
                    case "LqdHydrogen": // added for cryotanks and any other supported mod that uses Liquid Hydrogen
                        Resource("LqdHydrogen").Consume(thrust_flow * fuel.ratio, "rcs");
                        break;
                }
            }
        }

        void Process_rcsfx(ModuleRCSFX mrfx)
        {
            // calculate thrust fuel flow
            double thrust_flow = mrfx.maxFuelFlow * 1e3 * mrfx.thrustPercentage * mrfx.thrusterPower;

            // search fuel types
            foreach (Propellant fuel in mrfx.propellants)
            {
                switch (fuel.name)
                {
                    case "ElectricCharge": // mainly used for Ion RCS
                        Resource("ElectricCharge").Consume(thrust_flow * fuel.ratio, "rcs");
                        break;
                    case "LqdHydrogen": // added for cryotanks and any other supported mod that uses Liquid Hydrogen
                        Resource("LqdHydrogen").Consume(thrust_flow * fuel.ratio, "rcs");
                        break;
                }
            }
        }

        void Process_solarPanel(SolarPanelFixer spf, EnvironmentAnalyzer env)
        {
            if (spf.part.editorStarted && spf.isInitialized && spf.isEnabled && spf.editorEnabled)
            {
                double editorOutput = 0.0;
                switch (Planner.Sunlight)
                {
                    case Planner.SunlightState.SunlightNominal:
                        editorOutput = spf.nominalRate * (env.solar_flux / Sim.SolarFluxAtHome);
                        if (editorOutput > 0.0)
                            Resource("ElectricCharge").Produce(editorOutput, "solar panel (nominal)");
                        break;
                    case Planner.SunlightState.SunlightSimulated:
                        // create a sun direction according to the shadows direction in the VAB / SPH
                        Vector3d sunDir = EditorDriver.editorFacility == EditorFacility.VAB
                            ? new Vector3d(1.0, 1.0, 0.0).normalized
                            : new Vector3d(0.0, 1.0, -1.0).normalized;
                        string occludingPart = null;
                        double effiencyFactor = spf.SolarPanel.GetCosineFactor(sunDir, true) *
                                                spf.SolarPanel.GetOccludedFactor(sunDir, out occludingPart, true);
                        double distanceFactor = env.solar_flux / Sim.SolarFluxAtHome;
                        editorOutput = spf.nominalRate * effiencyFactor * distanceFactor;
                        if (editorOutput > 0.0)
                            Resource("ElectricCharge").Produce(editorOutput, "solar panel (estimated)");
                        break;
                }
            }
        }

        Dictionary<string, SimulatedResource> resources = new Dictionary<string, SimulatedResource>();
        List<SimulatedRecipe> recipes = new List<SimulatedRecipe>();
    }
} // KERBALISM
