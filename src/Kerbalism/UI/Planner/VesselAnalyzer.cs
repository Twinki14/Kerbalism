using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kerbalism.Planner
{
    ///<summary> Planners simulator for all vessel aspects other than resource simulation </summary>
    public sealed class VesselAnalyzer
    {
        public void Analyze(List<Part> parts, ResourceSimulator sim, EnvironmentAnalyzer env)
        {
            // note: vessel analysis require resource analysis, but at the same time resource analysis
            // require vessel analysis, so we are using resource analysis from previous frame (that's okay)
            // in the past, it was the other way around - however that triggered a corner case when va.comforts
            // was null (because the vessel analysis was still never done) and some specific rule/process
            // in resource analysis triggered an exception, leading to the vessel analysis never happening
            // inverting their order avoided this corner-case

            Analyze_crew(parts);
            Analyze_comms(parts);
        }

        void Analyze_crew(List<Part> parts)
        {
            // get number of kerbals assigned to the vessel in the editor
            // note: crew manifest is not reset after root part is deleted
            VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
            crew = manifest.GetAllCrew(false).FindAll(k => k != null);
            crew_count = (uint) crew.Count;
            crew_engineer = crew.Find(k => k.trait == "Engineer") != null;
            crew_scientist = crew.Find(k => k.trait == "Scientist") != null;
            crew_pilot = crew.Find(k => k.trait == "Pilot") != null;

            crew_engineer_maxlevel = 0;
            crew_scientist_maxlevel = 0;
            crew_pilot_maxlevel = 0;
            foreach (ProtoCrewMember c in crew)
            {
                switch (c.trait)
                {
                    case "Engineer":
                        crew_engineer_maxlevel = Math.Max(crew_engineer_maxlevel, (uint) c.experienceLevel);
                        break;
                    case "Scientist":
                        crew_scientist_maxlevel = Math.Max(crew_scientist_maxlevel, (uint) c.experienceLevel);
                        break;
                    case "Pilot":
                        crew_pilot_maxlevel = Math.Max(crew_pilot_maxlevel, (uint) c.experienceLevel);
                        break;
                }
            }

            // scan the parts
            crew_capacity = 0;
            foreach (Part p in parts)
            {
                // accumulate crew capacity
                crew_capacity += (uint) p.CrewCapacity;
            }

            // if the user press ALT, the planner consider the vessel crewed at full capacity
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                crew_count = crew_capacity;
        }

        void Analyze_comms(List<Part> parts)
        {
            foreach (Part p in parts)
            {
                foreach (PartModule m in p.Modules)
                {
                    // skip disabled modules
                    if (!m.isEnabled)
                        continue;

                    // RemoteTech enabled, passive's don't count
                    if (m.moduleName == "ModuleRTAntenna")
                    {
                    }
                    else if (m is ModuleDataTransmitter mdt)
                    {
                        // CommNet enabled and external transmitter
                        if (HighLogic.fetch.currentGame.Parameters.Difficulty.EnableCommNet)
                            if (mdt.antennaType != AntennaType.INTERNAL)
                            {
                            }
                            // the simple stupid always connected signal system
                    }
                }
            }
        }

        // general
        public List<ProtoCrewMember> crew; // full information on all crew
        public uint crew_count; // crew member on board
        public uint crew_capacity; // crew member capacity
        public bool crew_engineer; // true if an engineer is among the crew
        public bool crew_scientist; // true if a scientist is among the crew
        public bool crew_pilot; // true if a pilot is among the crew
        public uint crew_engineer_maxlevel; // experience level of top engineer on board
        public uint crew_scientist_maxlevel; // experience level of top scientist on board
        public uint crew_pilot_maxlevel; // experience level of top pilot on board
    }
} // KERBALISM
