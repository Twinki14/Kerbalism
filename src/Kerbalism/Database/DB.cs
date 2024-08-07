using System;
using System.Collections.Generic;
using System.Linq;
using Kerbalism.Science;

namespace Kerbalism.Database
{
    public static class DB
    {
        public static int uid; // savegame unique id
        public static LandmarkData landmarks; // store landmark data
        public static UIData ui; // store ui data

        private static Version version; // savegame version
        private static Dictionary<Guid, VesselData> vessels = new Dictionary<Guid, VesselData>(); // store data per-vessel

        public static void Load(ConfigNode node)
        {
            // get version (or use current one for new savegames)
            var versionStr = Lib.ConfigValue(node, "version", Lib.KerbalismVersion.ToString());

            // sanitize old saves (pre 3.1) format (X.X.X.X) to new format (X.X)
            if (versionStr.Split('.').Length > 2)
            {
                versionStr = versionStr.Split('.')[0] + "." + versionStr.Split('.')[1];
            }

            version = new Version(versionStr);

            // if this is an unsupported version, print warning
            if (version <= new Version(1, 2))
            {
                Lib.Log("loading save from unsupported version " + version);
            }

            // get unique id (or generate one for new savegames)
            uid = Lib.ConfigValue(node, "uid", Lib.RandomInt(int.MaxValue));

            // load the science database, has to be before vessels are loaded
            ScienceDB.Load(node);

            UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Load.Vessels");
            vessels.Clear();

            // flightstate will be null when first creating the game
            if (HighLogic.CurrentGame.flightState != null)
            {
                var vesselsNode = node.GetNode("vessels2") ?? new ConfigNode();

                // HighLogic.CurrentGame.flightState.protoVessels is what is used by KSP to persist vessels
                // It is always available and synchronized in OnLoad, no matter the scene, excepted on the first OnLoad in a new game
                foreach (var pv in HighLogic.CurrentGame.flightState.protoVessels)
                {
                    if (pv.vesselID == Guid.Empty)
                    {
                        // It seems flags are saved with an empty GUID. skip them.
                        Lib.LogDebug("Skipping VesselData load for vessel with empty GUID :" + pv.vesselName);
                        continue;
                    }

                    var vd = new VesselData(pv, vesselsNode.GetNode(pv.vesselID.ToString()));
                    vessels.Add(pv.vesselID, vd);
                    Lib.LogDebug("VesselData loaded for vessel " + pv.vesselName);
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();

            // for compatibility with old saves, convert drives data (it's now saved in PartData)
            if (node.HasNode("drives"))
            {
                var allParts = new Dictionary<uint, PartData>();
                foreach (var partData in vessels.Values.SelectMany(vesselData => vesselData.PartDatas))
                {
                    // we had a case of someone having a save with multiple parts having the same flightID
                    // 5 duplicates, all were asteroids.
                    allParts.TryAdd(partData.FlightId, partData);
                }

                foreach (var drive_node in node.GetNode("drives").GetNodes())
                {
                    var driveId = Lib.Parse.ToUInt(drive_node.name);
                    if (allParts.TryGetValue(driveId, out var part))
                    {
                        part.Drive = new Drive(drive_node);
                    }
                }
            }

            // load landmark data
            if (node.HasNode("landmarks"))
            {
                landmarks = new LandmarkData(node.GetNode("landmarks"));
            }
            else
            {
                landmarks = new LandmarkData();
            }

            // load ui data
            if (node.HasNode("ui"))
            {
                ui = new UIData(node.GetNode("ui"));
            }
            else
            {
                ui = new UIData();
            }

            // if an old savegame was imported, log some debug info
            if (version != Lib.KerbalismVersion)
            {
                Lib.Log("savegame converted from version " + version + " to " + Lib.KerbalismVersion);
            }
        }

        public static void Save(ConfigNode node)
        {
            // save version
            node.AddValue("version", Lib.KerbalismVersion.ToString());

            // save unique id
            node.AddValue("uid", uid);

            // only persist vessels that exists in KSP own vessel persistence
            // this prevent creating junk data without going into the mess of using gameevents
            UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.DB.Save.Vessels");
            var vesselsNode = node.AddNode("vessels2");
            foreach (var pv in HighLogic.CurrentGame.flightState.protoVessels)
            {
                if (pv.vesselID == Guid.Empty)
                {
                    // It seems flags are saved with an empty GUID. skip them.
                    Lib.LogDebug("Skipping VesselData save for vessel with empty GUID :" + pv.vesselName);
                    continue;
                }

                var vd = pv.KerbalismData();
                var vesselNode = vesselsNode.AddNode(pv.vesselID.ToString());
                vd.Save(vesselNode);
            }

            UnityEngine.Profiling.Profiler.EndSample();

            // save the science database
            ScienceDB.Save(node);

            // save landmark data
            landmarks.Save(node.AddNode("landmarks"));

            // save ui data
            ui.Save(node.AddNode("ui"));
        }

        public static VesselData KerbalismData(this Vessel vessel)
        {
            if (vessels.TryGetValue(vessel.id, out var vd))
            {
                return vd;
            }

            Lib.LogDebug("Creating Vesseldata for new vessel " + vessel.vesselName);
            vd = new VesselData(vessel);
            vessels.Add(vessel.id, vd);

            return vd;
        }

        public static VesselData KerbalismData(this ProtoVessel protoVessel)
        {
            if (vessels.TryGetValue(protoVessel.vesselID, out var vd))
            {
                return vd;
            }

            Lib.Log(
                "VesselData for protovessel " + protoVessel.vesselName + ", ID=" + protoVessel.vesselID +
                " doesn't exist !", Lib.LogLevel.Warning);
            vd = new VesselData(protoVessel, null);
            vessels.Add(protoVessel.vesselID, vd);

            return vd;
        }

        /// <summary>shortcut for VesselData.IsValid. False in the following cases : asteroid, debris, flag, deployed ground part, dead eva, rescue</summary>
        public static bool KerbalismIsValid(this Vessel vessel)
        {
            return KerbalismData(vessel).IsSimulated;
        }

        public static Dictionary<Guid, VesselData>.ValueCollection VesselDatas => vessels.Values;

        public static string To_safe_key(string key) => key.Replace(" ", "___");

        public static string From_safe_key(string key) => key.Replace("___", " ");

        #region VESSELDATA METHODS

        public static bool TryGetVesselDataTemp(this Vessel vessel, out VesselData vesselData)
        {
            if (vessels.TryGetValue(vessel.id, out vesselData))
            {
                return true;
            }

            Lib.LogStack($"Could not get VesselData for vessel {vessel.vesselName}", Lib.LogLevel.Error);
            return false;
        }

        /// <summary>
        /// Get the VesselData for this vessel, if it exists. Typically, you will need this in a Foreach on FlightGlobals.Vessels
        /// </summary>
        public static bool TryGetVesselData(this Vessel vessel, out VesselData vesselData)
        {
            return vessels.TryGetValue(vessel.id, out vesselData);
        }

        #endregion
    }
}
