using System.Collections.Generic;
using System.Linq;

namespace Kerbalism.Automation
{
    public sealed class Script
    {
        public readonly Dictionary<uint, bool> States;
        public string Prev;

        public Script()
        {
            States = new Dictionary<uint, bool>();
            Prev = string.Empty;
        }

        public Script(ConfigNode node)
        {
            States = new Dictionary<uint, bool>();
            foreach (var s in node.GetValues("state"))
            {
                var tokens = Lib.Tokenize(s, '@');
                if (tokens.Count != 2) continue;
                States.Add(Lib.Parse.ToUInt(tokens[0]), Lib.Parse.ToBool(tokens[1]));
            }

            Prev = Lib.ConfigValue(node, "prev", string.Empty);
        }

        public void Save(ConfigNode node)
        {
            foreach (var p in States)
            {
                node.AddValue("state", Lib.BuildString(p.Key.ToString(), "@", p.Value.ToString()));
            }

            node.AddValue("prev", Prev);
        }

        public void Set(Device dev, bool? state)
        {
            States.Remove(dev.Id);
            if (state != null)
            {
                States.Add(dev.Id, state == true);
            }
        }

        public void Execute(List<Device> devices)
        {
            foreach (var p in States)
            {
                foreach (var device in devices.Where(device => device.Id == p.Key))
                {
                    device.Ctrl(p.Value);
                }
            }
        }
    }
}
