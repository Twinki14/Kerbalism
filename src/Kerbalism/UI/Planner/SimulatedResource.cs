using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kerbalism.Planner
{
    ///<summary> Contains all the data for a single resource within the planners vessel simulator </summary>
    public sealed class SimulatedResource
    {
        public SimulatedResource(string name)
        {
            ResetSimulatorDisplayValues();

            _storage = new Dictionary<Resource_location, double>();
            _capacity = new Dictionary<Resource_location, double>();
            _amount = new Dictionary<Resource_location, double>();

            vessel_wide_location = new Resource_location();
            InitDicts(vessel_wide_location);
            _vessel_wide_view = new Simulated_resource_view_impl(null, resource_name, this);

            resource_name = name;
        }

        /// <summary>reset the values that are displayed to the user in the planner UI</summary>
        /// <remarks>
        /// use this after several simulator steps to do the final calculations under steady state
        /// where resources that are initially empty at vessel start have been created, otherwise
        /// user sees data only relevant for first simulation step (typically 1/50 seconds)
        /// </remarks>
        public void ResetSimulatorDisplayValues()
        {
            consumers = new Dictionary<string, Wrapper>();
            producers = new Dictionary<string, Wrapper>();
            consumed = 0.0;
            produced = 0.0;
        }

        /// <summary>
        /// Identifier to identify the part or vessel where resources are stored
        /// </summary>
        /// <remarks>
        /// KSP 1.3 does not support the necessary persistent identifier for per part resources
        /// KSP 1.3 always defaults to vessel wide
        /// design is shared with Resource_location in Resource.cs module
        /// </remarks>
        private class Resource_location
        {
            public Resource_location(Part p)
            {
                vessel_wide = false;
                persistent_identifier = p.persistentId;
            }

            public Resource_location()
            {
            }

            /// <summary>Equals method in order to ensure object behaves like a value object</summary>
            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != GetType())
                {
                    return false;
                }

                return (((Resource_location) obj).persistent_identifier == persistent_identifier) &&
                       (((Resource_location) obj).vessel_wide == vessel_wide);
            }

            /// <summary>GetHashCode method in order to ensure object behaves like a value object</summary>
            public override int GetHashCode()
            {
                return (int) persistent_identifier;
            }

            public bool IsVesselWide()
            {
                return vessel_wide;
            }

            public uint GetPersistentPartId()
            {
                return persistent_identifier;
            }

            private bool vessel_wide = true;
            private uint persistent_identifier = 0;
        }

        /// <summary>implementation of Simulated_resource_view</summary>
        /// <remarks>only constructed by Simulated_resource class to hide the dependencies between the two</remarks>
        private class Simulated_resource_view_impl : SimulatedResourceView
        {
            public Simulated_resource_view_impl(Part p, string resource_name, SimulatedResource i)
            {
                info = i;
                location = info.vessel_wide_location;
            }

            public override void AddPartResources(Part p)
            {
                info.AddPartResources(location, p);
            }

            public override void Produce(double quantity, string name)
            {
                info.Produce(location, quantity, name);
            }

            public override void Consume(double quantity, string name)
            {
                info.Consume(location, quantity, name);
            }

            public override void Clamp()
            {
                info.Clamp(location);
            }

            public override double amount => info._amount[location];

            public override double capacity => info._capacity[location];

            public override double storage => info._storage[location];

            private SimulatedResource info;
            private Resource_location location;
        }

        /// <summary>initialize resource amounts for new resource location</summary>
        /// <remarks>typically for a part that has not yet used this resource</remarks>
        private void InitDicts(Resource_location location)
        {
            _storage[location] = 0.0;
            _amount[location] = 0.0;
            _capacity[location] = 0.0;
        }

        /// <summary>obtain a view on this resource for a given loaded part</summary>
        /// <remarks>passing a null part forces it vessel wide view</remarks>
        public SimulatedResourceView GetSimulatedResourceView(Part p)
        {
            return _vessel_wide_view;
        }

        /// <summary>add resource information contained within part to vessel wide simulator</summary>
        public void AddPartResources(Part p)
        {
            AddPartResources(vessel_wide_location, p);
        }

        /// <summary>add resource information within part to per-part simulator</summary>
        private void AddPartResources(Resource_location location, Part p)
        {
            _storage[location] += Lib.Amount(p, resource_name);
            _amount[location] += Lib.Amount(p, resource_name);
            _capacity[location] += Lib.Capacity(p, resource_name);
        }

        /// <summary>consume resource from the vessel wide bookkeeping</summary>
        public void Consume(double quantity, string name)
        {
            Consume(vessel_wide_location, quantity, name);
        }

        /// <summary>consume resource from the per-part bookkeeping</summary>
        /// <remarks>also works for vessel wide location</remarks>
        private void Consume(Resource_location location, double quantity, string name)
        {
            if (quantity >= double.Epsilon)
            {
                _amount[location] -= quantity;
                consumed += quantity;

                if (!consumers.ContainsKey(name))
                    consumers.Add(name, new Wrapper());
                consumers[name].value += quantity;
            }
        }

        /// <summary>produce resource for the vessel wide bookkeeping</summary>
        public void Produce(double quantity, string name)
        {
            Produce(vessel_wide_location, quantity, name);
        }

        /// <summary>produce resource for the per-part bookkeeping</summary>
        /// <remarks>also works for vessel wide location</remarks>
        private void Produce(Resource_location location, double quantity, string name)
        {
            if (quantity >= double.Epsilon)
            {
                _amount[location] += quantity;
                produced += quantity;

                if (!producers.ContainsKey(name))
                    producers.Add(name, new Wrapper());
                producers[name].value += quantity;
            }
        }

        /// <summary>clamp resource amount to capacity for the vessel wide bookkeeping</summary>
        public void Clamp()
        {
            Clamp(vessel_wide_location);
        }

        /// <summary>clamp resource amount to capacity for the per-part bookkeeping</summary>
        private void Clamp(Resource_location location)
        {
            _amount[location] = Lib.Clamp(_amount[location], 0.0, _capacity[location]);
        }

        /// <summary>determine how long a resource will last at simulated consumption/production levels</summary>
        public double Lifetime()
        {
            double rate = produced - consumed;
            return amount <= double.Epsilon ? 0.0 : rate > -1e-10 ? double.NaN : amount / -rate;
        }

        /// <summary>generate resource tooltip multi-line string</summary>
        public string Tooltip(bool invert = false)
        {
            IDictionary<string, Wrapper> green = !invert ? producers : consumers;
            IDictionary<string, Wrapper> red = !invert ? consumers : producers;

            StringBuilder sb = new StringBuilder();
            int id = resource_name.GetHashCode();
            foreach (KeyValuePair<string, Wrapper> pair in green)
            {
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(Lib.Color(Lib.HumanOrSIRate(pair.Value.value, id), Lib.Kolor.PosRate, true));
                sb.Append("\t");
                sb.Append(pair.Key);
            }

            foreach (KeyValuePair<string, Wrapper> pair in red)
            {
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(Lib.Color(Lib.HumanOrSIRate(pair.Value.value, id), Lib.Kolor.NegRate, true));
                sb.Append("\t");
                sb.Append(pair.Key);
            }

            return Lib.BuildString("<align=left />", sb.ToString());
        }

        // Enforce that modification happens through official accessor functions
        // Many external classes need to read these values, and they want convenient access
        // However direct modification of these members from outside would make the coupling far too high
        public string resource_name { get; private set; }

        public double consumed { get; private set; }

        public double produced { get; private set; }

        // only getters, use official interface for setting that support resource location
        public double storage => _storage.Values.Sum();

        public double capacity => _capacity.Values.Sum();

        public double amount => _amount.Values.Sum();

        private IDictionary<Resource_location, double> _storage; // amount stored (at the start of simulation)
        private IDictionary<Resource_location, double> _capacity; // storage capacity
        private IDictionary<Resource_location, double> _amount; // amount stored (during simulation)

        private class Wrapper
        {
            public double value;
        }

        private IDictionary<string, Wrapper> consumers; // consumers metadata
        private IDictionary<string, Wrapper> producers; // producers metadata
        private Resource_location vessel_wide_location;
        private SimulatedResourceView _vessel_wide_view;
    }
} // KERBALISM
