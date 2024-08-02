using System;
using System.Collections.Generic;
using System.Text;
using Kerbalism.Database;
using Kerbalism.Modules;
using Kerbalism.Profile;
using Kerbalism.System;

namespace Kerbalism
{
    public static class Telemetry
    {
        public static void TelemetryPanel(this Panel p, Vessel v)
        {
            // avoid corner-case when this is called in a lambda after scene changes
            v = FlightGlobals.FindVessel(v.id);

            // if vessel doesn't exist anymore, leave the panel empty
            if (v == null) return;

            // get vessel data
            VesselData vd = v.KerbalismData();

            // if not a valid vessel, leave the panel empty
            if (!vd.IsSimulated) return;

            // set metadata
            p.Title(Lib.BuildString(Lib.Ellipsis(v.vesselName, Styles.ScaleStringLength(20)), " ",
                Lib.Color(Local.TELEMETRY_title, Lib.Kolor.LightGrey))); //"TELEMETRY"
            p.Width(Styles.ScaleWidthFloat(355.0f));
            p.paneltype = Panel.PanelType.telemetry;

            // time-out simulation
            if (p.Timeout(vd)) return;

            // get resources
            VesselResources resources = ResourceCache.Get(v);

            if (Features.Science) Render_science(p, v, vd);
            Render_supplies(p, v, vd, resources);
            Render_environment(p, v, vd);

            // collapse eva kerbal sections into one
            if (v.isEVA) p.Collapse(Local.TELEMETRY_EVASUIT); //"EVA SUIT"
        }


        static void Render_environment(Panel p, Vessel v, VesselData vd)
        {
            // don't show env panel in eva kerbals
            if (v.isEVA) return;

            // get all sensor readings
            HashSet<string> readings = new HashSet<string>();
            if (v.loaded)
            {
                foreach (var s in Lib.FindModules<Sensor>(v))
                {
                    readings.Add(s.type);
                }
            }
            else
            {
                foreach (ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Sensor"))
                {
                    readings.Add(Lib.Proto.GetString(m, "type"));
                }
            }

            readings.Remove(string.Empty);

            p.AddSection(Local.TELEMETRY_ENVIRONMENT); //"ENVIRONMENT"

            if (vd.SolarPanelsAverageExposure >= 0.0)
            {
                var exposureString = vd.SolarPanelsAverageExposure.ToString("P1");
                if (vd.SolarPanelsAverageExposure < 0.2) exposureString = Lib.Color(exposureString, Lib.Kolor.Orange);
                p.AddContent(Local.TELEMETRY_SolarPanelsAverageExposure, exposureString,
                    "<b>" + Local.TELEMETRY_Exposureignoringbodiesocclusion + "</b>\n<i>" +
                    Local.TELEMETRY_Exposureignoringbodiesocclusion_desc +
                    "</i>"); //"solar panels average exposure""Exposure ignoring bodies occlusion""Won't change on unloaded vessels\nMake sure to optimize it before switching
            }

            foreach (string type in readings)
            {
                p.AddContent(type.ToLower().Replace('_', ' '), Sensor.Telemetry_content(v, vd, type),
                    Sensor.Telemetry_tooltip(v, vd, type));
            }

            if (readings.Count == 0)
                p.AddContent("<i>" + Local.TELEMETRY_nosensorsinstalled + "</i>"); //no sensors installed
        }

        static void Render_science(Panel p, Vessel v, VesselData vd)
        {
            // don't show env panel in eva kerbals
            if (v.isEVA) return;

            p.AddSection(Local.TELEMETRY_TRANSMISSION); //"TRANSMISSION"

            // comm status
            if (vd.filesTransmitted.Count > 0)
            {
                double transmitRate = 0.0;
                StringBuilder tooltip = new StringBuilder();
                tooltip.Append(string.Format("<align=left /><b>{0,-15}\t{1}</b>\n", Local.TELEMETRY_TRANSMISSION_rate,
                    Local.TELEMETRY_filetransmitted)); //"rate""file transmitted"
                for (int i = 0; i < vd.filesTransmitted.Count; i++)
                {
                    transmitRate += vd.filesTransmitted[i].transmitRate;
                    tooltip.Append(string.Format("{0,-15}\t{1}",
                        Lib.HumanReadableDataRate(vd.filesTransmitted[i].transmitRate),
                        Lib.Ellipsis(vd.filesTransmitted[i].subjectData.FullTitle, 40u)));
                    if (i < vd.filesTransmitted.Count - 1) tooltip.Append("\n");
                }

                p.AddContent(Local.TELEMETRY_transmitting,
                    Lib.BuildString(vd.filesTransmitted.Count.ToString(),
                        vd.filesTransmitted.Count > 1 ? " files at " : " file at ",
                        Lib.HumanReadableDataRate(transmitRate)), tooltip.ToString()); //"transmitting"
            }
            else
            {
                p.AddContent(Local.TELEMETRY_maxtransmissionrate,
                    Lib.HumanReadableDataRate(vd.Connection.rate)); //"max transmission rate"
            }

            p.AddContent(Local.TELEMETRY_target, vd.Connection.target_name); //"target"

            // total science gained by vessel
            p.AddContent(Local.TELEMETRY_totalsciencetransmitted,
                Lib.HumanReadableScience(vd.scienceTransmitted, false)); //"total science transmitted"
        }

        static void Render_supplies(Panel p, Vessel v, VesselData vd, VesselResources resources)
        {
            int supplies = 0;
            // for each supply
            foreach (Supply supply in Profile.Profile.supplies)
            {
                // get resource info
                ResourceInfo res = resources.GetResource(v, supply.resource);

                // only show estimate if the resource is present
                if (res.Capacity <= 1e-10) continue;

                // render panel title, if not done already
                if (supplies == 0) p.AddSection(Local.TELEMETRY_SUPPLIES); //"SUPPLIES"

                // determine label
                var resource = PartResourceLibrary.Instance.resourceDefinitions[supply.resource];
                string label = Lib.SpacesOnCaps(resource.displayName).ToLower();

                StringBuilder sb = new StringBuilder();

                sb.Append("<align=left />");
                if (res.AverageRate != 0.0)
                {
                    sb.Append(Lib.Color(res.AverageRate > 0.0,
                        Lib.BuildString("+", Lib.HumanOrSIRate(Math.Abs(res.AverageRate), resource.id)),
                        Lib.Kolor.PosRate,
                        Lib.BuildString("-", Lib.HumanOrSIRate(Math.Abs(res.AverageRate), resource.id)),
                        Lib.Kolor.NegRate,
                        true));
                }
                else
                {
                    sb.Append("<b>");
                    sb.Append(Local.TELEMETRY_nochange); //no change
                    sb.Append("</b>");
                }

                if (res.AverageRate < 0.0 && res.Level < 0.0001)
                {
                    sb.Append(" <i>");
                    sb.Append(Local.TELEMETRY_empty); //(empty)
                    sb.Append("</i>");
                }
                else if (res.AverageRate > 0.0 && res.Level > 0.9999)
                {
                    sb.Append(" <i>");
                    sb.Append(Local.TELEMETRY_full); //(full)
                    sb.Append("</i>");
                }
                else sb.Append("   "); // spaces to prevent alignement issues

                sb.Append("\t");
                sb.Append(Lib.HumanOrSIAmount(res.Amount, resource.id));
                sb.Append("/");
                sb.Append(Lib.HumanOrSIAmount(res.Capacity, resource.id));
                sb.Append(" (");
                sb.Append(res.Level.ToString("P0"));
                sb.Append(")");

                List<SupplyData.ResourceBrokerRate> brokers = vd.Supply(supply.resource).ResourceBrokers;
                if (brokers.Count > 0)
                {
                    sb.Append("\n<b>------------    \t------------</b>");
                    foreach (SupplyData.ResourceBrokerRate rb in brokers)
                    {
                        sb.Append("\n");
                        sb.Append(Lib.Color(rb.Rate > 0.0,
                            Lib.BuildString("+", Lib.HumanOrSIRate(Math.Abs(rb.Rate), resource.id), "   "),
                            Lib.Kolor.PosRate, // spaces to mitigate alignement issues
                            Lib.BuildString("-", Lib.HumanOrSIRate(Math.Abs(rb.Rate), resource.id), "   "),
                            Lib.Kolor.NegRate, // spaces to mitigate alignement issues
                            true));
                        sb.Append("\t");
                        sb.Append(rb.Broker.Title);
                    }
                }

                string rate_tooltip = sb.ToString();

                // finally, render resource supply
                p.AddContent(label, Lib.HumanReadableDuration(res.DepletionTime()), rate_tooltip);
                ++supplies;
            }
        }
    }
} // KERBALISM
