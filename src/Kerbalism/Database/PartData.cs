using Kerbalism.Science;

namespace Kerbalism.Database
{
    public class PartData
    {
        public uint FlightId { get; private set; }

        public Drive Drive { get; set; }

        public PartData(Part part)
        {
            FlightId = part.flightID;
        }

        public PartData(ProtoPartSnapshot protopart)
        {
            FlightId = protopart.flightID;
        }

        public void Save(ConfigNode node)
        {
            if (Drive == null)
            {
                return;
            }

            var driveNode = node.AddNode("drive");
            Drive.Save(driveNode);
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode("drive"))
            {
                Drive = new Drive(node.GetNode("drive"));
            }
        }

        /// <summary> Must be called if the part is destroyed </summary>
        public void OnPartWillDie()
        {
            Drive?.DeleteDriveData();
        }
    }
}
