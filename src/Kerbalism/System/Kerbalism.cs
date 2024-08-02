using System;
using System.Collections.Generic;
using Kerbalism.Automation;
using Kerbalism.Comms;
using Kerbalism.Database;
using Kerbalism.External;
using Kerbalism.Modules;
using Kerbalism.Science;
using Kerbalism.Utility;
using KSP.UI.Screens;
using UnityEngine;
using LineRenderer = Kerbalism.Renderer.LineRenderer;

namespace Kerbalism.System
{
    /// <summary>
    /// Main initialization class : for everything that isn't save-game dependant.
    /// For save-dependant things, or things that require the game to be loaded do it in Kerbalism.OnLoad()
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class KerbalismCoreSystems : MonoBehaviour
    {
        public void Start()
        {
            // reset the save game initialized flag
            Kerbalism.IsSaveGameInitDone = false;

            // things in here will be only called once per KSP launch, after loading
            // nearly everything is available at this point, including the Kopernicus patched bodies.
            if (!Kerbalism.IsCoreMainMenuInitDone)
            {
                Kerbalism.IsCoreMainMenuInitDone = true;
            }

            // things in here will be called every the player goes to the main menu
            RemoteTech.EnableInSPC(); // allow RemoteTech Core to run in the Space Center
        }
    }

    [KSPScenario(ScenarioCreationOptions.AddToAllGames,
        new[] {GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR})]
    public sealed class Kerbalism : ScenarioModule
    {
        #region declarations

        /// <summary> global access </summary>
        public static Kerbalism Fetch { get; private set; } = null;

        /// <summary> Is the one-time main menu init done. Becomes true after loading, when the the main menu is shown, and never becomes false again</summary>
        public static bool IsCoreMainMenuInitDone { get; set; } = false;

        /// <summary> Is the one-time on game load init done. Becomes true after the first OnLoad() of a game, and never becomes false again</summary>
        public static bool IsCoreGameInitDone { get; set; } = false;

        /// <summary> Is the savegame (or new game) first load done. Becomes true after the first OnLoad(), and false when returning to the main menu</summary>
        public static bool IsSaveGameInitDone { get; set; } = false;

        // used to setup KSP callbacks
        public static Callbacks Callbacks { get; private set; }

        // the rendering script attached to map camera
        static MapCameraScript map_camera_script;

        // store time until last update for unloaded vessels
        // note: not using reference_wrapper<T> to increase readability
        sealed class Unloaded_data
        {
            public double time;
        }; //< reference wrapper

        static Dictionary<Guid, Unloaded_data> unloaded = new Dictionary<Guid, Unloaded_data>();

        // equivalent to TimeWarp.fixedDeltaTime
        // note: stored here to avoid converting it to double every time
        public static double elapsed_s;

        // number of steps from last warp blending
        private static uint warp_blending;

        /// <summary>Are we in an intermediary timewarp speed ?</summary>
        public static bool WarpBlending => warp_blending > 2u;

        // last savegame unique id
        static int savegame_uid;

        /// <summary> real time of last game loaded event </summary>
        public static float gameLoadTime = 0.0f;

        public static bool SerenityEnabled { get; private set; }

        private static bool didSanityCheck = false;

        #endregion

        #region initialization & save/load

        //  constructor
        public Kerbalism()
        {
            // enable global access
            Fetch = this;

            SerenityEnabled = Expansions.ExpansionsLoader.IsExpansionInstalled("Serenity");
        }

        private void OnDestroy()
        {
            Fetch = null;
        }

        public override void OnLoad(ConfigNode node)
        {
            // everything in there will be called only one time : the first time a game is loaded from the main menu
            if (!IsCoreGameInitDone)
            {
                try
                {
                    // core game systems
                    Sim.Init(); // find suns (Kopernicus support)
                    Radiation.Init(); // create the radiation fields
                    ScienceDB.Init(); // build the science database (needs Sim.Init() and Radiation.Init() first)
                    Science.Science.Init(); // register the science hijacker

                    // static graphic components
                    LineRenderer.Init();
                    ParticleRenderer.Init();
                    Highlighter.Init();

                    // UI
                    Textures.Init(); // set up the icon textures
                    UI.Init(); // message system, main gui, launcher
                    global::Kerbalism.KsmGui.KsmGuiMasterController.Init(); // setup the new gui framework

                    // part prefabs hacks
                    Profile.Profile.SetupPods(); // add supply resources to pods
                    Misc.PartPrefabsTweaks(); // part prefabs tweaks, must be called after ScienceDB.Init()

                    // Create KsmGui windows
                    new ScienceArchiveWindow();

                    // GameEvents callbacks
                    Callbacks = new Callbacks();
                }
                catch (Exception e)
                {
                    string fatalError = SanityCheck(true);
                    if (fatalError == null)
                        fatalError = string.Empty;
                    else
                        fatalError += "\n\n";

                    fatalError += "FATAL ERROR : Kerbalism core init has failed :" + "\n" + e.ToString();
                    Lib.Log(fatalError, Lib.LogLevel.Error);
                    LoadFailedPopup(fatalError);
                }

                IsCoreGameInitDone = true;
            }

            // everything in there will be called every time a savegame (or a new game) is loaded from the main menu
            if (!IsSaveGameInitDone)
            {
                try
                {
                    Cache.Init();
                    global::Kerbalism.ResourceCache.Init();

                    BackgroundResources.DisableBackgroundResources();
                }
                catch (Exception e)
                {
                    string fatalError = "FATAL ERROR : Kerbalism save game init has failed :" + "\n" + e.ToString();
                    Lib.Log(fatalError, Lib.LogLevel.Error);
                    LoadFailedPopup(fatalError);
                }

                IsSaveGameInitDone = true;

                Message.Clear();
            }

            // eveything else will be called on every OnLoad() call :
            // - save/load
            // - every scene change
            // - in various semi-random situations (thanks KSP)

            // Fix for background IMGUI textures being dropped on scene changes since KSP 1.8
            Styles.ReloadBackgroundStyles();

            // always clear the caches
            Cache.Clear();
            global::Kerbalism.ResourceCache.Clear();

            // deserialize our database
            try
            {
                UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Load");
                DB.Load(node);
                UnityEngine.Profiling.Profiler.EndSample();
            }
            catch (Exception e)
            {
                string fatalError = "FATAL ERROR : Kerbalism save game load has failed :" + "\n" + e.ToString();
                Lib.Log(fatalError, Lib.LogLevel.Error);
                LoadFailedPopup(fatalError);
            }

            // detect if this is a different savegame
            if (DB.uid != savegame_uid)
            {
                // clear caches
                Message.all_logs.Clear();

                // sync main window pos from db
                UI.Sync();

                // remember savegame id
                savegame_uid = DB.uid;
            }

            Kerbalism.gameLoadTime = Time.time;
        }

        public override void OnSave(ConfigNode node)
        {
            if (!enabled) return;

            // serialize data
            UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Save");
            DB.Save(node);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void LoadFailedPopup(string error)
        {
            string popupMsg = "Kerbalism has encountered an unrecoverable error and KSP must be closed\n\n";
            popupMsg +=
                "If you can't fix it, ask for help in the <b>kerbalism discord</b> or at the KSP forums thread\n\n";
            popupMsg +=
                "Please provide a screenshot of this message, and your ksp.log file found in your KSP install folder\n\n";
            popupMsg += error;

            Lib.Popup("Kerbalism fatal error", popupMsg, 600f);
        }

        #endregion

        #region fixedupdate

        void FixedUpdate()
        {
            // remove control locks in any case
            Misc.ClearLocks();

            // do nothing if paused
            if (Lib.IsPaused())
                return;

            // convert elapsed time to double only once
            double fixedDeltaTime = TimeWarp.fixedDeltaTime;

            // and detect warp blending
            if (Math.Abs(fixedDeltaTime - elapsed_s) < 0.001)
                warp_blending = 0;
            else
                ++warp_blending;

            // update elapsed time
            elapsed_s = fixedDeltaTime;

            // store info for oldest unloaded vessel
            double last_time = 0.0;
            Guid last_id = Guid.Empty;
            Vessel last_v = null;
            VesselData last_vd = null;
            VesselResources last_resources = null;

            // credit science at regular interval
            ScienceDB.CreditScienceBuffers(elapsed_s);

            foreach (VesselData vd in DB.VesselDatas)
            {
                vd.EarlyUpdate();
            }

            // for each vessel
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                // get vessel data
                VesselData vd = v.KerbalismData();

                // update the vessel data validity
                vd.Update(v);

                // do nothing else for invalid vessels
                if (!vd.IsSimulated)
                    continue;

                // get resource cache
                VesselResources resources = global::Kerbalism.ResourceCache.Get(v);

                // if loaded
                if (v.loaded)
                {
                    //UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.VesselDataEval");
                    // update the vessel info
                    vd.Evaluate(false, elapsed_s);
                    //UnityEngine.Profiling.Profiler.EndSample();

                    // get most used resource
                    ResourceInfo ec = resources.GetResource(v, "ElectricCharge");

                    UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Radiation");
                    // show belt warnings
                    Radiation.BeltWarnings(v, vd);
                    UnityEngine.Profiling.Profiler.EndSample();

                    UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Comms");
                    CommsMessages.Update(v, vd, elapsed_s);
                    UnityEngine.Profiling.Profiler.EndSample();

                    UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Science");
                    // transmit science data
                    Science.Science.Update(v, vd, ec, elapsed_s);
                    UnityEngine.Profiling.Profiler.EndSample();

                    UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Profile");
                    // apply rules
                    Profile.Profile.Execute(v, vd, resources, elapsed_s);
                    UnityEngine.Profiling.Profiler.EndSample();

                    UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Profile");
                    // part module resource updates
                    vd.ResourceUpdate(resources, elapsed_s);
                    UnityEngine.Profiling.Profiler.EndSample();

                    UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Loaded.Resource");
                    // apply deferred requests
                    resources.Sync(v, vd, elapsed_s);
                    UnityEngine.Profiling.Profiler.EndSample();

                    // call automation scripts
                    vd.computer.Automate(v, vd, resources);

                    // remove from unloaded data container
                    unloaded.Remove(vd.VesselId);
                }
                // if unloaded
                else
                {
                    // get unloaded data, or create an empty one
                    Unloaded_data ud;
                    if (!unloaded.TryGetValue(vd.VesselId, out ud))
                    {
                        ud = new Unloaded_data();
                        unloaded.Add(vd.VesselId, ud);
                    }

                    // accumulate time
                    ud.time += elapsed_s;

                    // maintain oldest entry
                    if (ud.time > last_time)
                    {
                        last_time = ud.time;
                        last_v = v;
                        last_vd = vd;
                        last_resources = resources;
                    }
                }
            }

            // at most one vessel gets background processing per physics tick :
            // if there is a vessel that is not the currently loaded vessel, then
            // we will update the vessel whose most recent background update is the oldest
            if (last_v != null)
            {
                //UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.VesselDataEval");
                // update the vessel info (high timewarp speeds reevaluation)
                last_vd.Evaluate(false, last_time);
                //UnityEngine.Profiling.Profiler.EndSample();

                // get most used resource
                ResourceInfo last_ec = last_resources.GetResource(last_v, "ElectricCharge");

                UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Radiation");
                // show belt warnings
                Radiation.BeltWarnings(last_v, last_vd);

                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Comms");
                CommsMessages.Update(last_v, last_vd, last_time);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Profile");
                // apply rules
                Profile.Profile.Execute(last_v, last_vd, last_resources, last_time);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Background");
                // simulate modules in background
                Background.Update(last_v, last_vd, last_resources, last_time);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Science");
                // transmit science	data
                Science.Science.Update(last_v, last_vd, last_ec, last_time);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.FixedUpdate.Unloaded.Resource");
                // apply deferred requests
                last_resources.Sync(last_v, last_vd, last_time);
                UnityEngine.Profiling.Profiler.EndSample();

                // call automation scripts
                last_vd.computer.Automate(last_v, last_vd, last_resources);

                // remove from unloaded data container
                unloaded.Remove(last_vd.VesselId);
            }
        }

        #endregion

        #region Update and GUI

        void Update()
        {
            if (!didSanityCheck)
                SanityCheck();

            // attach map renderer to planetarium camera once
            if (MapView.MapIsEnabled && map_camera_script == null)
                map_camera_script = PlanetariumCamera.Camera.gameObject.AddComponent<MapCameraScript>();

            // process keyboard input
            Misc.KeyboardInput();

            // add description to techs
            Misc.TechDescriptions();

            // set part highlight colors
            Highlighter.Update();

            // prepare gui content
            UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.UI.Update");
            UI.Update(Callbacks.visible);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        void OnGUI()
        {
            UI.On_gui(Callbacks.visible);
        }

        #endregion

        private string SanityCheck(bool forced = false)
        {
            // fix PostScreenMessage() not being available for a few updates after scene load since KSP 1.8
            if (!forced)
            {
                if (ScreenMessages.PostScreenMessage("") == null)
                {
                    didSanityCheck = false;
                    return string.Empty;
                }
                else
                {
                    didSanityCheck = true;
                }
            }

            bool harmonyFound = false;
            foreach (var a in AssemblyLoader.loadedAssemblies)
            {
                if (a.name.ToLower().Contains("harmony"))
                    harmonyFound = true;
            }

            if (!harmonyFound)
            {
                string result =
                    "<color=#FF4500><b>HarmonyKSP isn't installed</b></color>\nThis is a required dependency for Kerbalism!";
                DisplayWarning(result);
                enabled = false;
                return result;
            }

            if (!Settings.loaded)
            {
                string result =
                    "<color=#FF4500><b>No Kerbalism configuration found</b></color>\nCheck that you have installed KerbalismConfig (or any other Kerbalism config pack).";
                DisplayWarning(result);
                enabled = false;
                return result;
            }

            List<string> incompatibleMods = Settings.IncompatibleMods();
            List<string> warningMods = Settings.WarningMods();

            List<string> incompatibleModsFound = new List<string>();
            List<string> warningModsFound = new List<string>();

            foreach (var a in AssemblyLoader.loadedAssemblies)
            {
                if (incompatibleMods.Contains(a.name.ToLower())) incompatibleModsFound.Add(a.name);
                if (warningMods.Contains(a.name.ToLower())) warningModsFound.Add(a.name);
            }

            string msg = string.Empty;

            var configNodes = GameDatabase.Instance.GetConfigs("Kerbalism");
            if (configNodes.Length > 1)
            {
                msg +=
                    "<color=#FF4500>Multiple configurations detected</color>\nHint: delete KerbalismConfig if you are using a custom config pack.\n\n";
            }

            if (incompatibleModsFound.Count > 0)
            {
                msg += "<color=#FF4500>Mods with known incompatibilities found:</color>\n";
                foreach (var m in incompatibleModsFound) msg += "- " + m + "\n";
                msg += "Kerbalism will not run properly with these mods. Please remove them.\n\n";
            }

            if (warningModsFound.Count > 0)
            {
                msg += "<color=#FF4500>Mods with limited compatibility found:</color>\n";
                foreach (var m in warningModsFound) msg += "- " + m + "\n";
                msg += "You might have problems with these mods. Please consult the FAQ on on kerbalism.github.io\n\n";
            }

            DisplayWarning(msg);
            return msg;
        }

        private static void DisplayWarning(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            msg = "<b>KERBALISM WARNING</b>\n\n" + msg;
            ScreenMessage sm = new ScreenMessage(msg, 20, ScreenMessageStyle.UPPER_CENTER);
            sm.color = Color.cyan;

            ScreenMessages.PostScreenMessage(sm);
            ScreenMessages.PostScreenMessage(msg, true);
            Lib.Log("Sanity check: " + msg);
        }
    }

    public sealed class MapCameraScript : MonoBehaviour
    {
        void OnPostRender()
        {
            // do nothing when not in map view
            // - avoid weird situation when in some user installation MapIsEnabled is true in the space center
            if (!MapView.MapIsEnabled || HighLogic.LoadedScene == GameScenes.SPACECENTER)
                return;

            // commit all geometry
            Radiation.Render();

            // render all committed geometry
            LineRenderer.Render();
            ParticleRenderer.Render();
        }
    }

    // misc functions
    public static class Misc
    {
        public static void ClearLocks()
        {
            // remove control locks
            InputLockManager.RemoveControlLock("eva_dead_lock");
            InputLockManager.RemoveControlLock("no_signal_lock");
        }

        public static void TechDescriptions()
        {
            var rnd = RDController.Instance;
            if (rnd == null)
                return;
            var selected = RDController.Instance.node_selected;
            if (selected == null)
                return;
            var techID = selected.tech.techID;
            if (rnd.node_description.text.IndexOf("<i></i>\n", StringComparison.Ordinal) ==
                -1) //< check for state in the string
            {
                rnd.node_description.text += "<i></i>\n"; //< store state in the string

                // collect unique configure-related unlocks
                HashSet<string> labels = new HashSet<string>();
                foreach (AvailablePart p in PartLoader.LoadedPartsList)
                {
                    // workaround for FindModulesImplementing nullrefs in 1.8 when called on the strange kerbalEVA_RD_Exp prefab
                    // due to the (private) cachedModuleLists being null on it
                    if (p.partPrefab.Modules.Count == 0)
                        continue;

                    foreach (Configure cfg in p.partPrefab.FindModulesImplementing<Configure>())
                    {
                        foreach (ConfigureSetup setup in cfg.Setups())
                        {
                            if (setup.tech == selected.tech.techID)
                            {
                                labels.Add(Lib.BuildString(setup.name, " to ", cfg.title));
                            }
                        }
                    }
                }

                // add unique configure-related unlocks
                // avoid printing text over the "available parts" section
                int i = 0;
                foreach (string label in labels)
                {
                    rnd.node_description.text += Lib.BuildString("\n• <color=#00ffff>", label, "</color>");
                    i++;
                    if (i >= 5 && labels.Count > i + 1)
                    {
                        rnd.node_description.text += Lib.BuildString("\n• <color=#00ffff>(+",
                            (labels.Count - i).ToString(), " more)</color>");
                        break;
                    }
                }
            }
        }

        public static void PartPrefabsTweaks()
        {
            foreach (AvailablePart ap in PartLoader.LoadedPartsList)
            {
                // recompile some part infos (this is normally done by KSP on loading, after each part prefab is compiled)
                // This is needed because :
                // - We can't check interdependent modules when OnLoad() is called, since the other modules may not be loaded yet
                // - The science DB needs the system/bodies to be instantiated, which is done after the part compilation
                bool partNeedsInfoRecompile = false;

                foreach (PartModule module in ap.partPrefab.Modules)
                {
                    // we want to remove the editor part tooltip module infos widgets that are switchable trough the configure module
                    // because the clutter the UI quite a bit. To do so, every module that implements IConfigurable is made to return
                    // an empty string in their GetInfo() if the IConfigurable.ModuleIsConfigured() is ever called on them.
                    if (module is Configure configure)
                    {
                        List<IConfigurable> configurables = configure.GetIConfigurableModules();

                        if (configurables.Count > 0)
                            partNeedsInfoRecompile = true;

                        foreach (IConfigurable configurable in configurables)
                            configurable.ModuleIsConfigured();
                    }
                    // note that the experiment modules on the prefab gets initialized from the scienceDB init, which also do
                    // a LoadedPartsList loop to get the scienceDB module infos. So this has to be called after the scienceDB init.
                    else if (module is Experiment)
                    {
                        partNeedsInfoRecompile = true;
                    }
                }

                // for some reason this crashes on the EVA kerbals parts
                if (partNeedsInfoRecompile && !ap.name.StartsWith("kerbalEVA"))
                {
                    ap.moduleInfos.Clear();
                    ap.resourceInfos.Clear();
                    try
                    {
                        Lib.ReflectionCall(PartLoader.Instance, "CompilePartInfo",
                            new Type[] {typeof(AvailablePart), typeof(Part)}, new object[] {ap, ap.partPrefab});
                    }
                    catch (Exception ex)
                    {
                        Lib.Log("Could not patch the moduleInfo for part " + ap.name + " - " + ex.Message + "\n" +
                                ex.StackTrace);
                    }
                }
            }
        }

        public static void KeyboardInput()
        {
            // mute/unmute messages with keyboard
            if (Input.GetKeyDown(KeyCode.Pause))
            {
                if (!Message.IsMuted())
                {
                    Message.Post(Local.Messagesmuted,
                        Local.Messagesmuted_subtext); //"Messages muted""Be careful out there"
                    Message.Mute();
                }
                else
                {
                    Message.Unmute();
                    Message.Post(Local.Messagesunmuted); //"Messages unmuted"
                }
            }

            // toggle body info window with keyboard
            if (MapView.MapIsEnabled && Input.GetKeyDown(KeyCode.B))
            {
                UI.Open(BodyInfo.Body_info);
            }

            // call action scripts
            // - avoid creating vessel data for invalid vessels
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null) return;
            VesselData vd = v.KerbalismData();
            if (!vd.IsSimulated) return;

            // call scripts with 1-5 key
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                vd.computer.Execute(v, ScriptType.action1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                vd.computer.Execute(v, ScriptType.action2);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                vd.computer.Execute(v, ScriptType.action3);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                vd.computer.Execute(v, ScriptType.action4);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                vd.computer.Execute(v, ScriptType.action5);
            }
        }

        // return true if the vessel is a rescue mission
    }
} // KERBALISM
