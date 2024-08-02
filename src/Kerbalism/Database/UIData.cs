namespace Kerbalism.Database
{
    public class UIData
    {
        public uint WinLeft; // popout window position left
        public uint WinTop; // popout window position top
        public bool MapViewed; // has the user entered map-view/tracking-station

        public UIData()
        {
            WinLeft = 280u;
            WinTop = 100u;
            MapViewed = false;
        }

        public UIData(ConfigNode node)
        {
            WinLeft = Lib.ConfigValue(node, "win_left", 280u);
            WinTop = Lib.ConfigValue(node, "win_top", 100u);
            MapViewed = Lib.ConfigValue(node, "map_viewed", false);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("win_left", WinLeft);
            node.AddValue("win_top", WinTop);
            node.AddValue("map_viewed", MapViewed);
        }
    }
}
