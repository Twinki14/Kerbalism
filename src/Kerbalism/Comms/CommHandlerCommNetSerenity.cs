﻿using Expansions.Serenity.DeployedScience.Runtime;
using Kerbalism.System;
using Kerbalism.Utility;

namespace Kerbalism.Comms
{
    public class CommHandlerCommNetSerenity : CommHandlerCommNetBase
    {
        private DeployedScienceCluster cluster;

        protected override void UpdateInputs(ConnectionInfo connection)
        {
            connection.transmitting = vd.filesTransmitted.Count > 0;

            if (cluster == null)
                cluster = Serenity.GetScienceCluster(vd.Vessel);

            connection.ec = 0.0;
            connection.ec_idle = 0.0;

            if (cluster == null)
            {
                BaseRate = 0.0;
                connection.powered = false;
                connection.hasActiveAntenna = false;
            }
            else
            {
                BaseRate = Settings.DataRateSurfaceExperiment;
                connection.powered = cluster.IsPowered;
                connection.hasActiveAntenna = cluster.AntennaParts.Count > 0;
            }
        }
    }
}
