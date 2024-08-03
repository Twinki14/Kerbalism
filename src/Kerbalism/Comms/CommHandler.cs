using System;
using Kerbalism.Database;
using Kerbalism.External;
using Kerbalism.System;

namespace Kerbalism.Comms
{
    public class CommHandler
    {
        private static bool _commNetPatchApplied = false;

        protected VesselData Vd;

        private bool _transmittersDirty;

        /// <summary>
        /// false while the network isn't initialized or when the transmitter list is not up-to-date
        /// </summary>
        public bool IsReady => NetworkIsReady && !_transmittersDirty;

        /// <summary>
        /// pseudo ctor for getting the right handler type
        /// </summary>
        public static CommHandler GetHandler(VesselData vd, bool isGroundController)
        {
            CommHandler handler;

            // Note : API CommHandlers may not be registered yet when this is called,
            // but this shouldn't be an issue, as the derived types UpdateTransmitters / UpdateNetwork
            // won't be called anymore once the API handler is registered.
            // This said, this isn't ideal, and it would be cleaner to have a "commHandledByAPI"
            // bool that mods should set once and for all before any vessel exist.

            if (!_commNetPatchApplied)
            {
                _commNetPatchApplied = true;

                if (API.Comm.handlers.Count == 0 && !RemoteTech.Installed)
                {
                    CommHandlerCommNetBase.ApplyHarmonyPatches();
                }
            }

            if (API.Comm.handlers.Count > 0)
            {
                handler = new CommHandler();
                Lib.LogDebug("Created new API CommHandler");
            }
            else if (isGroundController)
            {
                handler = new CommHandlerCommNetSerenity();
                Lib.LogDebug("Created new CommHandlerCommNetSerenity");
            }
            else
            {
                handler = new CommHandlerCommNetVessel();
                Lib.LogDebug("Created new CommHandlerCommNetVessel");
            }

            handler.Vd = vd;
            handler._transmittersDirty = true;

            return handler;
        }

        /// <summary> Update the provided Connection </summary>
        public void UpdateConnection(ConnectionInfo connection)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Kerbalism.CommHandler.UpdateConnection");

            UpdateInputs(connection);

            // Can this ever be anything other than 0? not atm, unless I'm missing something...
            if (API.Comm.handlers.Count == 0)
            {
                if (NetworkIsReady)
                {
                    if (_transmittersDirty)
                    {
                        UpdateTransmitters(connection, true);
                        _transmittersDirty = false;
                    }
                    else
                    {
                        UpdateTransmitters(connection, false);
                    }

                    UpdateNetwork(connection);
                }
            }
            else
            {
                _transmittersDirty = false;
                try
                {
                    API.Comm.handlers[0].Invoke(null, new object[] {connection, Vd.Vessel});
                }
                catch (Exception e)
                {
                    Lib.Log("CommInfo handler threw exception " + e.Message + "\n" + e, Lib.LogLevel.Error);
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// Clear and re-find all transmitters partmodules on the vessel.
        /// Must be called when parts have been removed / added on the vessel.
        /// </summary>
        public void ResetPartTransmitters() => _transmittersDirty = true;

        /// <summary>
        /// Get the cost for transmitting data with this CommHandler
        /// </summary>
        /// <param name="transmittedTotal">Amount of the total capacity of data that can be sent</param>
        /// <param name="elapsed_s"></param>
        /// <returns></returns>
        public virtual double GetTransmissionCost(double transmittedTotal, double elapsed_s)
        {
            return (Vd.Connection.ec - Vd.Connection.ec_idle) * (transmittedTotal / (Vd.Connection.rate * elapsed_s));
        }

        /// <summary>
        /// update the fields that can be used as an input by API handlers
        /// </summary>
        protected virtual void UpdateInputs(ConnectionInfo connection)
        {
            connection.transmitting = Vd.filesTransmitted.Count > 0;
            connection.powered = Vd.Powered;
        }

        protected virtual bool NetworkIsReady => true;

        protected virtual void UpdateNetwork(ConnectionInfo connection)
        {
        }

        protected virtual void UpdateTransmitters(ConnectionInfo connection, bool searchTransmitters)
        {
        }
    }
}
