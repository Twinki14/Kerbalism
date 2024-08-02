namespace Kerbalism.Database
{
    public class LandmarkData
    {
        public bool BeltCrossing; // record first belt crossing
        public bool SpaceAnalysis; // record first lab sample analysis in space
        public bool HeliopauseCrossing; // record first heliopause crossing

        public LandmarkData()
        {

        }

        public LandmarkData(ConfigNode node)
        {
            BeltCrossing = Lib.ConfigValue(node, "belt_crossing", false);
            SpaceAnalysis = Lib.ConfigValue(node, "space_analysis", false);
            HeliopauseCrossing = Lib.ConfigValue(node, "heliopause_crossing", false);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("belt_crossing", BeltCrossing);
            node.AddValue("space_analysis", SpaceAnalysis);
            node.AddValue("heliopause_crossing", HeliopauseCrossing);
        }
    }
}
