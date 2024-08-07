using System.Collections.Generic;
using System.Text;

namespace Kerbalism.Utility
{
    public interface ISpecifics
    {
        Specifics Specs();
    }

    public sealed class Specifics
    {
        public Specifics()
        {
            entries = new List<Entry>();
        }

        public void Add(string label, string value = "")
        {
            Entry e = new Entry
            {
                label = label,
                value = value
            };
            entries.Add(e);
        }

        public string Info(string desc = "")
        {
            StringBuilder sb = new StringBuilder();
            if (desc.Length > 0)
            {
                sb.Append("<i>");
                sb.Append(desc);
                sb.Append("</i>\n\n");
            }

            bool firstEntry = true;
            foreach (Entry e in entries)
            {
                if (!firstEntry)
                    sb.Append("\n");
                else
                    firstEntry = false;

                sb.Append(e.label);
                if (e.value.Length > 0)
                {
                    sb.Append(": <b>");
                    sb.Append(e.value);
                    sb.Append("</b>");
                }
            }

            return sb.ToString();
        }

        public class Entry
        {
            public string label = string.Empty;
            public string value = string.Empty;
        }

        public List<Entry> entries;
    }
} // KERBALISM