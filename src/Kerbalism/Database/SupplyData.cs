using System.Collections.Generic;

namespace Kerbalism.Database
{
    public class SupplyData
    {
        public uint Message; // used to avoid sending messages multiple times
        public List<ResourceBrokerRate> ResourceBrokers { get; } = new List<ResourceBrokerRate>();

        public SupplyData()
        {
            Message = 0;
        }

        public SupplyData(ConfigNode node)
        {
            Message = Lib.ConfigValue(node, "message", 0u);
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("message", Message);
        }

        public class ResourceBrokerRate
        {
            public readonly ResourceBroker Broker;
            public readonly double Rate;

            public ResourceBrokerRate(ResourceBroker broker, double amount)
            {
                Broker = broker;
                Rate = amount;
            }
        }

        public void UpdateResourceBrokers(Dictionary<ResourceBroker, double> brokersResAmount,
            Dictionary<ResourceBroker, double> ruleBrokersRate, double unsupportedBrokersRate, double elapsedSeconds)
        {
            ResourceBrokers.Clear();

            foreach (var p in ruleBrokersRate)
            {
                var broker = ResourceBroker.GetOrCreate(p.Key.Id + "Avg", p.Key.Category, Lib.BuildString(p.Key.Title, " (", Local.Generic_AVERAGE, ")"));
                ResourceBrokers.Add(new ResourceBrokerRate(broker, p.Value));
            }

            foreach (var p in brokersResAmount)
            {
                ResourceBrokers.Add(new ResourceBrokerRate(p.Key, p.Value / elapsedSeconds));
            }

            if (unsupportedBrokersRate != 0.0)
            {
                ResourceBrokers.Add(new ResourceBrokerRate(ResourceBroker.Generic, unsupportedBrokersRate));
            }
        }
    }
}
