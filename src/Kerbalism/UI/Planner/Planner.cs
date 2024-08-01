using System;
using System.Collections.Generic;
using Kerbalism.System;
using UnityEngine;


namespace Kerbalism.Planner
{
    ///<summary> Class for the Planner used in the VAB/SPH, it is used to predict resource production/consumption and
    /// provide information on life support, radiation, comfort and other relevant factors. </summary>
    public static class Planner
    {
        #region CONSTRUCTORS_DESTRUCTORS

        ///<summary> Initializes the Planner for use </summary>
        internal static void Initialize()
        {
            // set the ui styles
            SetStyles();

            // Compute sorted body indices
            ComputeSortedBodyIndices();

            // set default body index to home
            body_index = FlightGlobals.GetHomeBodyIndex();

            // resource panels
            // - add all resources defined in the Profiles Supply configs except EC
            Profile.Profile.supplies.FindAll(k => k.resource != "ElectricCharge")
                .ForEach(k => supplies.Add(k.resource));

            // special panels
            // - stress & radiation panels require that a rule using the living_space/radiation modifier exist (current limitation)
            if (Features.Radiation)
                panel_special.Add("radiation");

            panel_environment.Add("environment");
        }

        ///<summary> Sets the styles for the panels UI </summary>
        private static void SetStyles()
        {
            // left menu style
            leftmenu_style = new GUIStyle(HighLogic.Skin.label)
            {
                richText = true
            };
            leftmenu_style.normal.textColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            leftmenu_style.fixedWidth =
                Styles.ScaleWidthFloat(
                    80.0f); // Fixed to avoid that the sun icon moves around for different planet name lengths
            leftmenu_style.stretchHeight = true;
            leftmenu_style.fontSize = Styles.ScaleInteger(10);
            leftmenu_style.alignment = TextAnchor.MiddleLeft;

            // right menu style
            rightmenu_style = new GUIStyle(leftmenu_style)
            {
                alignment = TextAnchor.MiddleRight
            };

            // quote style
            quote_style = new GUIStyle(HighLogic.Skin.label)
            {
                richText = true
            };
            quote_style.normal.textColor = Color.black;
            quote_style.stretchWidth = true;
            quote_style.stretchHeight = true;
            quote_style.fontSize = Styles.ScaleInteger(11);
            quote_style.alignment = TextAnchor.LowerCenter;

            // center icon style
            icon_style = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter
            };

            // debug header style
            devbuild_style = new GUIStyle();
            devbuild_style.normal.textColor = Color.white;
            devbuild_style.stretchHeight = true;
            devbuild_style.fontSize = Styles.ScaleInteger(12);
            devbuild_style.alignment = TextAnchor.MiddleCenter;
        }

        ///<summary>Constructed a list of CB indices that is sorted (hierarchically) by SMA</summary>
        private static void ComputeSortedBodyIndices()
        {
            void SortBodiesAndAppendIndicesToList(List<CelestialBody> bodies)
            {
                bodies.Sort((a, b) => a.orbit.semiMajorAxis.CompareTo(b.orbit.semiMajorAxis));
                foreach (var body in bodies)
                {
                    sorted_body_indices.Add(body.flightGlobalsIndex);
                    if (body.orbitingBodies.Count > 0)
                    {
                        SortBodiesAndAppendIndicesToList(new List<CelestialBody>(body.orbitingBodies));
                    }
                }
            }

            SortBodiesAndAppendIndicesToList(new List<CelestialBody>(Planetarium.fetch.Sun.orbitingBodies));
        }

        #endregion

        #region EVENTS

        ///<summary> Method called when the vessel in the editor has been modified </summary>
        internal static void EditorShipModifiedEvent(ShipConstruct sc) => RefreshPlanner();

        #endregion

        #region METHODS

        ///<summary> Call this to trigger a planner update</summary>
        internal static void RefreshPlanner() => update_counter = 0;

        ///<summary> Run simulators and update the planner UI sub-panels </summary>
        internal static void Update()
        {
            // get vessel crew manifest
            VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
            if (manifest == null)
                return;

            // check for number of crew change
            if (vessel_analyzer.crew_count != manifest.CrewCount)
                enforceUpdate = true;

            // only update when we need to, repeat update a number of times to allow the simulators to catch up
            if (!enforceUpdate && update_counter++ > 3)
                return;

            // clear the panel
            panel.Clear();

            // if there is something in the editor
            if (EditorLogic.RootPart != null)
            {
                // get parts recursively
                List<Part> parts = Lib.GetPartsRecursively(EditorLogic.RootPart);

                // analyze using the settings from the panels user input
                env_analyzer.Analyze(FlightGlobals.Bodies[body_index], altitude_mults[situation_index], sunlight);
                vessel_analyzer.Analyze(parts, resource_sim, env_analyzer);
                resource_sim.Analyze(parts, env_analyzer, vessel_analyzer);

                // add ec panel
                AddSubPanelEC(panel);

                // get vessel resources
                panel_resource.Clear();
                foreach (string res in supplies)
                    if (resource_sim.Resource(res).capacity > 0.0)
                        panel_resource.Add(res);

                // reset current panel if necessary
                if (resource_index >= panel_resource.Count) resource_index = 0;

                // add resource panel
                if (panel_resource.Count > 0)
                    AddSubPanelResource(panel, panel_resource[resource_index]);

                // add environment panel
                switch (panel_environment[environment_index])
                {
                    case "environment":
                        AddSubPanelEnvironment(panel);
                        break;
                }
            }

            enforceUpdate = false;
        }

        ///<summary> Planner panel UI width </summary>
        internal static float Width()
        {
            return Styles.ScaleWidthFloat(280.0f);
        }

        ///<summary> Planner panel UI height </summary>
        internal static float Height()
        {
            if (EditorLogic.RootPart != null)
                return Styles.ScaleFloat(Lib.IsDevBuild ? 45.0f : 30.0f) +
                       panel.Height(); // header + ui content + dev build header if present
            else
                return Styles.ScaleFloat(66.0f); // quote-only
        }

        ///<summary> Render planner UI panel </summary>
        internal static void Render()
        {
            // if there is something in the editor
            if (EditorLogic.RootPart != null)
            {
                if (Lib.IsDevBuild)
                {
                    GUILayout.BeginHorizontal(Styles.title_container);
                    GUILayout.Label(new GUIContent("KERBALISM DEV BUILD " + Lib.KerbalismDevBuild), devbuild_style);
                    GUILayout.EndHorizontal();
                }

                // start header
                GUILayout.BeginHorizontal(Styles.title_container);

                // body selector
                GUILayout.Label(new GUIContent(FlightGlobals.Bodies[body_index].name, Local.Planner_Targetbody),
                    leftmenu_style); //"Target body"
                if (Lib.IsClicked())
                {
                    var sorted_index = sorted_body_indices.IndexOf(body_index);
                    body_index = sorted_body_indices[(sorted_index + 1) % sorted_body_indices.Count];
                    enforceUpdate = true;
                }
                else if (Lib.IsClicked(1))
                {
                    var sorted_index = sorted_body_indices.IndexOf(body_index);
                    body_index = sorted_body_indices[(sorted_index - 1) % sorted_body_indices.Count];
                    enforceUpdate = true;
                }

                // sunlight selector
                switch (sunlight)
                {
                    case SunlightState.SunlightNominal:
                        GUILayout.Label(new GUIContent(Textures.sun_white, Local.Planner_SunlightNominal), icon_style);
                        break; //"In sunlight\n<b>Nominal</b> solar panel output"
                    case SunlightState.SunlightSimulated:
                        GUILayout.Label(new GUIContent(Textures.solar_panel, Local.Planner_SunlightSimulated),
                            icon_style);
                        break; //"In sunlight\n<b>Estimated</b> solar panel output\n<i>Sunlight direction : look at the shadows !</i>"
                    case SunlightState.Shadow:
                        GUILayout.Label(new GUIContent(Textures.sun_black, Local.Planner_Shadow), icon_style);
                        break; //"In shadow"
                }

                if (Lib.IsClicked())
                {
                    sunlight = (SunlightState) (((int) sunlight + 1) % Enum.GetValues(typeof(SunlightState)).Length);
                    enforceUpdate = true;
                }

                // situation selector
                GUILayout.Label(new GUIContent(situations[situation_index], Local.Planner_Targetsituation),
                    rightmenu_style); //"Target situation"
                if (Lib.IsClicked())
                {
                    situation_index = (situation_index + 1) % situations.Length;
                    enforceUpdate = true;
                }
                else if (Lib.IsClicked(1))
                {
                    situation_index = (situation_index == 0 ? situations.Length : situation_index) - 1;
                    enforceUpdate = true;
                }

                // end header
                GUILayout.EndHorizontal();

                // render panel
                panel.Render();
            }
            // if there is nothing in the editor
            else
            {
                // render quote
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.Label("<i>" + Local.Planner_RenderQuote + "</i>",
                    quote_style); //In preparing for space, I have always found that\nplans are useless but planning is indispensable.\nWernher von Kerman
                GUILayout.EndHorizontal();
                GUILayout.Space(Styles.ScaleFloat(10.0f));
            }
        }

        ///<summary> Add environment sub-panel, including tooltips </summary>
        private static void AddSubPanelEnvironment(Panel p)
        {
            string flux_tooltip = Lib.BuildString
            (
                "<align=left />" +
                String.Format("<b>{0,-14}\t{1,-15}\t{2}</b>\n", Local.Planner_Source, Local.Planner_Flux,
                    Local.Planner_Temp), //"Source""Flux""Temp"
                String.Format("{0,-14}\t{1,-15}\t{2}\n", Local.Planner_solar,
                    env_analyzer.solar_flux > 0.0 ? Lib.HumanReadableFlux(env_analyzer.solar_flux) : Local.Generic_NONE,
                    Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env_analyzer.solar_flux))), //"solar""none"
                String.Format("{0,-14}\t{1,-15}\t{2}\n", Local.Planner_albedo,
                    env_analyzer.albedo_flux > 0.0
                        ? Lib.HumanReadableFlux(env_analyzer.albedo_flux)
                        : Local.Generic_NONE,
                    Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env_analyzer.albedo_flux))), //"albedo""none"
                String.Format("{0,-14}\t{1,-15}\t{2}\n", Local.Planner_body,
                    env_analyzer.body_flux > 0.0 ? Lib.HumanReadableFlux(env_analyzer.body_flux) : Local.Generic_NONE,
                    Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env_analyzer.body_flux))), //"body""none"
                String.Format("{0,-14}\t{1,-15}\t{2}\n", Local.Planner_background,
                    Lib.HumanReadableFlux(Sim.BackgroundFlux()),
                    Lib.HumanReadableTemp(Sim.BlackBodyTemperature(Sim.BackgroundFlux()))), //"background"
                String.Format("{0,-14}\t\t{1,-15}\t{2}", Local.Planner_total,
                    Lib.HumanReadableFlux(env_analyzer.total_flux),
                    Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env_analyzer.total_flux))) //"total"
            );
            string atmosphere_tooltip = Lib.BuildString
            (
                "<align=left />",
                String.Format("{0,-14}\t<b>{1}</b>\n", Local.BodyInfo_breathable,
                    Sim.Breathable(env_analyzer.body)
                        ? Local.BodyInfo_breathable_yes
                        : Local.BodyInfo_breathable_no), //"breathable""yes""no"
                String.Format("{0,-14}\t<b>{1}</b>\n", Local.Planner_pressure,
                    Lib.HumanReadablePressure(env_analyzer.body.atmospherePressureSeaLevel)), //"pressure"
                String.Format("{0,-14}\t<b>{1}</b>\n", Local.BodyInfo_lightabsorption,
                    Lib.HumanReadablePerc(1.0 - env_analyzer.atmo_factor)), //"light absorption"
                String.Format("{0,-14}\t<b>{1}</b>", Local.BodyInfo_gammaabsorption,
                    Lib.HumanReadablePerc(1.0 - Sim.GammaTransparency(env_analyzer.body, 0.0))) //"gamma absorption"
            );
            string shadowtime_str = Lib.HumanReadableDuration(env_analyzer.shadow_period) + " (" +
                                    (env_analyzer.shadow_time * 100.0).ToString("F0") + "%)";

            p.AddSection(Local.TELEMETRY_ENVIRONMENT, string.Empty, //"ENVIRONMENT"
                () =>
                {
                    p.Prev(ref environment_index, panel_environment.Count);
                    enforceUpdate = true;
                },
                () =>
                {
                    p.Next(ref environment_index, panel_environment.Count);
                    enforceUpdate = true;
                });
            p.AddContent(Local.Planner_temperature, Lib.HumanReadableTemp(env_analyzer.temperature),
                env_analyzer.body.atmosphere && env_analyzer.landed
                    ? Local.Planner_atmospheric
                    : flux_tooltip); //"temperature""atmospheric"
            p.AddContent(Local.Planner_atmosphere,
                env_analyzer.body.atmosphere ? Local.Planner_atmosphere_yes : Local.Planner_atmosphere_no,
                atmosphere_tooltip); //"atmosphere""yes""no"
            p.AddContent(Local.Planner_shadowtime, shadowtime_str,
                Local.Planner_shadowtime_desc); //"shadow time""the time in shadow\nduring the orbit"
        }

        ///<summary> Add electric charge sub-panel, including tooltips </summary>
        private static void AddSubPanelEC(Panel p)
        {
            // get simulated resource
            SimulatedResource res = resource_sim.Resource("ElectricCharge");

            // create tooltip
            string tooltip = res.Tooltip();

            // render the panel section
            p.AddSection(Local.Planner_ELECTRICCHARGE); //"ELECTRIC CHARGE"
            p.AddContent(Local.Planner_storage, Lib.HumanOrSIAmount(res.storage, Lib.ECResID), tooltip); //"storage"
            p.AddContent(Local.Planner_consumed, Lib.HumanOrSIRate(res.consumed, Lib.ECResID), tooltip); //"consumed"
            p.AddContent(Local.Planner_produced, Lib.HumanOrSIRate(res.produced, Lib.ECResID), tooltip); //"produced"
            p.AddContent(Local.Planner_duration, Lib.HumanReadableDuration(res.Lifetime())); //"duration"
        }

        ///<summary> Add supply resource sub-panel, including tooltips </summary>
        ///<remarks>
        /// does not include electric charge
        /// does not include special resources like waste atmosphere
        /// restricted to resources that are configured explicitly in the profile as supplies
        ///</remarks>
        private static void AddSubPanelResource(Panel p, string res_name)
        {
            // get simulated resource
            SimulatedResource res = resource_sim.Resource(res_name);

            // create tooltip
            string tooltip = res.Tooltip();

            var resource = PartResourceLibrary.Instance.resourceDefinitions[res_name];

            // render the panel section
            p.AddSection(Lib.SpacesOnCaps(resource.displayName).ToUpper(), string.Empty,
                () =>
                {
                    p.Prev(ref resource_index, panel_resource.Count);
                    enforceUpdate = true;
                },
                () =>
                {
                    p.Next(ref resource_index, panel_resource.Count);
                    enforceUpdate = true;
                });
            p.AddContent(Local.Planner_storage, Lib.HumanOrSIAmount(res.storage, resource.id), tooltip); //"storage"
            p.AddContent(Local.Planner_consumed, Lib.HumanOrSIRate(res.consumed, resource.id), tooltip); //"consumed"
            p.AddContent(Local.Planner_produced, Lib.HumanOrSIRate(res.produced, resource.id), tooltip); //"produced"
            p.AddContent(Local.Planner_duration, Lib.HumanReadableDuration(res.Lifetime())); //"duration"
        }

        #endregion

        #region FIELDS_PROPERTIES

        // store situations and altitude multipliers
        private static readonly string[] situations = {"Landed", "Low Orbit", "Orbit", "High Orbit"};
        private static readonly double[] altitude_mults = {0.0, 0.33, 1.0, 3.0};

        // styles
        private static GUIStyle devbuild_style;
        private static GUIStyle leftmenu_style;
        private static GUIStyle rightmenu_style;
        private static GUIStyle quote_style;
        private static GUIStyle icon_style;

        // analyzers
        private static ResourceSimulator resource_sim = new ResourceSimulator();
        private static EnvironmentAnalyzer env_analyzer = new EnvironmentAnalyzer();
        private static VesselAnalyzer vessel_analyzer = new VesselAnalyzer();

        // panel arrays
        private static List<string> supplies = new List<string>();
        private static List<string> panel_resource = new List<string>();
        private static List<string> panel_special = new List<string>();
        private static List<string> panel_environment = new List<string>();

        // body/situation/sunlight indexes
        private static int body_index;
        private static List<int> sorted_body_indices = new List<int>();
        private static int situation_index = 2; // orbit

        public enum SunlightState
        {
            SunlightNominal = 0,
            SunlightSimulated = 1,
            Shadow = 2
        }

        private static SunlightState sunlight = SunlightState.SunlightSimulated;
        public static SunlightState Sunlight => sunlight;

        // panel indexes
        private static int resource_index;
        private static int environment_index;

        // panel ui
        private static Panel panel = new Panel();
        private static bool enforceUpdate = false;
        private static int update_counter = 0;

        #endregion
    }
} // KERBALISM
