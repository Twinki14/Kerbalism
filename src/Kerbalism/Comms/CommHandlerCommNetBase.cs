using System;
using CommNet;
using HarmonyLib;
using Kerbalism.Database;
using Kerbalism.System;
using KSP.Localization;

namespace Kerbalism.Comms
{
    public class CommHandlerCommNetBase : CommHandler
    {
        /// <summary> base data rate set in derived classes from UpdateTransmitters()</summary>
        protected double BaseRate = 0.0;

        protected override bool NetworkIsReady => CommNetNetwork.Initialized && CommNetNetwork.Instance?.CommNet != null;

        protected override void UpdateNetwork(ConnectionInfo connection)
        {
            var v = vd.Vessel;

            var vIsNull = v == null || v.connection == null;

            connection.linked = !vIsNull && connection.powered && v.connection.IsConnected;

            if (!connection.linked)
            {
                connection.strength = 0.0;
                connection.rate = 0.0;
                connection.target_name = string.Empty;
                connection.control_path.Clear();

                if (!vIsNull && v.connection.InPlasma)
                {
                    connection.Status = LinkStatus.plasma;
                }
                else
                {
                    connection.Status = LinkStatus.no_link;
                }

                return;
            }

            var firstLink = v.connection.ControlPath.First;
            connection.Status = firstLink.hopType == HopType.Home ? LinkStatus.direct_link : LinkStatus.indirect_link;
            connection.strength = firstLink.signalStrength;

            connection.rate = BaseRate * Math.Pow(firstLink.signalStrength, Sim.DataRateDampingExponent);

            connection.target_name =
                Lib.Ellipsis(Localizer.Format(v.connection.ControlPath.First.end.displayName).Replace("Kerbin", "DSN"),
                    20);

            if (connection.Status != LinkStatus.direct_link)
            {
                var firstHop = CommNodeToVessel(v.Connection.ControlPath.First.end);
                // Get rate from the firstHop, each Hop will do the same logic, then we will have the min rate for whole path
                if (firstHop == null || !firstHop.TryGetVesselData(out var vd))
                    connection.rate = 0.0;
                else
                    connection.rate = Math.Min(vd.Connection.rate, connection.rate);
            }

            connection.control_path.Clear();
            foreach (var link in v.connection.ControlPath)
            {
                var antennaPower = link.end.isHome
                    ? link.start.antennaTransmit.power + link.start.antennaRelay.power
                    : link.start.antennaTransmit.power;
                var linkDistance = (link.start.position - link.end.position).magnitude;
                var linkMaxDistance = Math.Sqrt(antennaPower * link.end.antennaRelay.power);
                var signalStrength = 1 - (linkDistance / linkMaxDistance);
                signalStrength = (3 - (2 * signalStrength)) * Math.Pow(signalStrength, 2);
                signalStrength = Math.Pow(signalStrength, Sim.DataRateDampingExponent);

                var controlPoint = new string[3];

                // name
                controlPoint[0] = Localizer.Format(link.end.displayName);
                if (link.end.isHome)
                {
                    controlPoint[0] = controlPoint[0].Replace("Kerbin", "DSN");
                }
                controlPoint[0] = Lib.Ellipsis(controlPoint[0], 35);

                // signal strength
                controlPoint[1] = Lib.HumanReadablePerc(Math.Ceiling(signalStrength * 10000) / 10000, "F2");

                // extra info
                controlPoint[2] = Lib.BuildString(
                    "Distance: ", Lib.HumanReadableDistance(linkDistance),
                    " (Max: ", Lib.HumanReadableDistance(linkMaxDistance), ")");

                connection.control_path.Add(controlPoint);
            }

            // set minimal data rate to what is defined in Settings (1 bit/s by default)
            if (connection.rate > 0.0 && connection.rate * Lib.bitsPerMB < Settings.DataRateMinimumBitsPerSecond)
                connection.rate = Settings.DataRateMinimumBitsPerSecond / Lib.bitsPerMB;
        }

        private static Vessel CommNodeToVessel(CommNode node)
        {
            return node?.transform?.gameObject.GetComponent<Vessel>();
        }

        public static void ApplyHarmonyPatches()
        {
            var CommNetVessel_OnNetworkPreUpdate_Info =
                AccessTools.Method(typeof(CommNetVessel), nameof(CommNetVessel.OnNetworkPreUpdate));

            Loader.HarmonyInstance.Patch(CommNetVessel_OnNetworkPreUpdate_Info,
                new HarmonyMethod(AccessTools.Method(typeof(CommHandlerCommNetBase),
                    nameof(CommNetVessel_OnNetworkPreUpdate_Prefix))));

            var CommNetVessel_OnNetworkPostUpdate_Info =
                AccessTools.Method(typeof(CommNetVessel), nameof(CommNetVessel.OnNetworkPostUpdate));

            Loader.HarmonyInstance.Patch(CommNetVessel_OnNetworkPostUpdate_Info,
                new HarmonyMethod(AccessTools.Method(typeof(CommHandlerCommNetBase),
                    nameof(CommNetVessel_OnNetworkPostUpdate_Prefix))));
        }

        // TODO - Kerbalism - Forked Science
        // TODO - Not sure if this was only needed for Storm related adjustments
        // ensure unloadedDoOnce is true for unloaded vessels
        private static void CommNetVessel_OnNetworkPreUpdate_Prefix(CommNetVessel __instance,
            ref bool ___unloadedDoOnce)
        {
            if (!__instance.Vessel.loaded && __instance.CanComm)
                ___unloadedDoOnce = true;
        }

        // TODO - Kerbalism - Forked Science
        // TODO - Not sure if this was only needed for Storm related adjustments
        // ensure unloadedDoOnce is true for unloaded vessels
        private static void CommNetVessel_OnNetworkPostUpdate_Prefix(CommNetVessel __instance,
            ref bool ___unloadedDoOnce)
        {
            if (!__instance.Vessel.loaded && __instance.CanComm)
                ___unloadedDoOnce = true;
        }
    }
}
