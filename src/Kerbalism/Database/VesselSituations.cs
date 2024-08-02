using System.Collections.Generic;
using Kerbalism.Science;

namespace Kerbalism.Database
{
    public class VesselSituations
    {
        private readonly VesselData _vd;

        private CelestialBody Body { get; set; }
        private int BiomeIndex { get; set; }
        private CBAttributeMapSO.MapAttribute biome;
        private List<ScienceSituation> Situations { get; } = new List<ScienceSituation>();
        private List<VirtualBiome> VirtualBiomes { get; } = new List<VirtualBiome>();

        private string BodyTitle => Body.displayName.LocalizeRemoveGender();
        private string BiomeTitle => biome != null ? biome.displayname : string.Empty;

        public string BodyName => Body.name;
        public string BiomeName => biome != null ? biome.name.Replace(" ", string.Empty) : string.Empty;

        public string FirstSituationTitle =>
            biome != null
                ? Lib.BuildString(BodyTitle, " ", Situations[0].Title(), " ", BiomeTitle)
                : Lib.BuildString(BodyTitle, " ", Situations[0].Title());

        public Situation FirstSituation => new Situation(Body.flightGlobalsIndex, Situations[0], BiomeIndex);

        public VesselSituations(VesselData vd)
        {
            _vd = vd;
        }

        /// <summary> Require EnvLanded, EnvInnerBelt and EnvOuterBelt to evaluated first </summary>
        public void Update()
        {
            Body = _vd.Vessel.mainBody;
            GetSituationsAndVirtualBiomes();
            BiomeIndex = GetBiomeIndex(_vd.Vessel);
            biome = BiomeIndex >= 0 ? Body.BiomeMap.Attributes[BiomeIndex] : null;
        }

        public Situation GetExperimentSituation(ExperimentInfo expInfo)
        {
            var expSituation = ScienceSituation.None;

            // TODO - Kerbalism - Forked Science
            // TODO - Can be greatly simplified
            foreach (var situation in Situations)
            {
                if (situation.IsAvailableForExperiment(expInfo))
                {
                    expSituation = situation;
                    break;
                }
            }

            var expBiomeIndex = BiomeIndex;
            if (expSituation.IsVirtualBiomesRelevantForExperiment(expInfo))
            {
                foreach (var virtualBiome in VirtualBiomes)
                {
                    if (expInfo.VirtualBiomes.Contains(virtualBiome))
                    {
                        expBiomeIndex = (int) virtualBiome;
                        break;
                    }
                }
            }

            return new Situation(Body.flightGlobalsIndex, expSituation, expBiomeIndex);
        }

        /// <summary>
        /// Return a list of available situations and special biomes for the vessel.
        /// The method is made so the lists are ordered with specific situations first and global ones last,
        /// because experiments will use the first valid situation/biome found.
        /// </summary>
        private void GetSituationsAndVirtualBiomes()
        {
            Situations.Clear();
            VirtualBiomes.Clear();

            if (_vd.EnvLanded)
            {
                switch (_vd.Vessel.situation)
                {
                    case Vessel.Situations.PRELAUNCH:
                    case Vessel.Situations.LANDED:
                        Situations.Add(ScienceSituation.SrfLanded);
                        break;
                    case Vessel.Situations.SPLASHED:
                        Situations.Add(ScienceSituation.SrfSplashed);
                        break;
                }

                Situations.Add(ScienceSituation.Surface);
                Situations.Add(ScienceSituation.BodyGlobal);

                if ((_vd.Vessel.latitude + 270.0) % 90.0 > 0.0)
                    VirtualBiomes.Add(VirtualBiome.NorthernHemisphere);
                else
                    VirtualBiomes.Add(VirtualBiome.SouthernHemisphere);

                VirtualBiomes.Add(VirtualBiome.NoBiome);
                return;
            }

            if (Body.atmosphere && _vd.Vessel.altitude < Body.atmosphereDepth)
            {
                if (_vd.Vessel.altitude < Body.scienceValues.flyingAltitudeThreshold)
                {
                    Situations.Add(ScienceSituation.FlyingLow);
                }
                else
                {
                    if (_vd.Vessel.verticalSpeed < 100.0
                        && (double.IsNaN(_vd.Vessel.orbit.ApA) || _vd.Vessel.orbit.ApA > Body.atmosphereDepth)
                        && _vd.Vessel.srfSpeed > _vd.Vessel.speedOfSound * 5.0)
                    {
                        VirtualBiomes.Add(VirtualBiome.Reentry);
                    }

                    Situations.Add(ScienceSituation.FlyingHigh);
                }

                Situations.Add(ScienceSituation.Flying);
                Situations.Add(ScienceSituation.BodyGlobal);

                if ((_vd.Vessel.latitude + 270.0) % 90.0 > 0.0)
                    VirtualBiomes.Add(VirtualBiome.NorthernHemisphere);
                else
                    VirtualBiomes.Add(VirtualBiome.SouthernHemisphere);

                VirtualBiomes.Add(VirtualBiome.NoBiome);
                return;
            }

            if (_vd.EnvInterstellar)
                VirtualBiomes.Add(VirtualBiome.Interstellar);

            if (_vd.EnvInnerBelt)
                VirtualBiomes.Add(VirtualBiome.InnerBelt);
            else if (_vd.EnvOuterBelt)
                VirtualBiomes.Add(VirtualBiome.OuterBelt);

            if (_vd.EnvMagnetosphere)
                VirtualBiomes.Add(VirtualBiome.Magnetosphere);

            if (_vd.Vessel.latitude > 0.0)
                VirtualBiomes.Add(VirtualBiome.NorthernHemisphere);
            else
                VirtualBiomes.Add(VirtualBiome.SouthernHemisphere);

            VirtualBiomes.Add(VirtualBiome.NoBiome);

            if (_vd.Vessel.altitude > Body.scienceValues.spaceAltitudeThreshold)
                Situations.Add(ScienceSituation.InSpaceHigh);
            else
                Situations.Add(ScienceSituation.InSpaceLow);

            Situations.Add(ScienceSituation.Space);
            Situations.Add(ScienceSituation.BodyGlobal);
        }

        private static int GetBiomeIndex(Vessel vessel)
        {
            var biomeMap = vessel.mainBody.BiomeMap;
            if (biomeMap == null)
                return -1;

            var lat = ((vessel.latitude + 180.0 + 90.0) % 180.0 - 90.0) * UtilMath.Deg2Rad; // clamp and convert to radians
            var lon = ((vessel.longitude + 360.0 + 180.0) % 360.0 - 180.0) * UtilMath.Deg2Rad; // clamp and convert to radians
            var biome = biomeMap.GetAtt(lat, lon);

            for (var i = biomeMap.Attributes.Length; i-- > 0;)
                if (biomeMap.Attributes[i] == biome)
                    return i;

            return -1;
        }
    }
}
