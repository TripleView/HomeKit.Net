using System.Text;

#region Rfc info
/*
3.3.14. TXT RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   TXT-DATA                    /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

TXT-DATA        One or more <character-string>s.

TXT RRs are used to hold descriptive text.  The semantics of the text
depends on the domain where it is found.
 *
*/
#endregion

namespace HomeKit.Net.Dns
{
    public class RecordTXT : Record
    {
        public List<string> TXT;

        public override Type RecordType()
        {
            return Type.TXT;
        }

        public RecordTXT(RecordReader rr, int Length)
        {
            TXT = new List<string>();
            int pos = rr.Position;
            while ((rr.Position - pos < Length) && (rr.Position < rr.Length))
                TXT.Add(rr.ReadString());
        }

        public RecordTXT(Dictionary<string, string> dict)
        {
            TXT = dict.Select(kv => kv.Key + "=" + kv.Value).ToList();
        }

        public RecordTXT(IEnumerable<string> sequence)
        {
            TXT = sequence.ToList();
        }

        public override void Write(RecordWriter rw)
        {
            if (TXT != null)
                foreach (string s in TXT)
                    rw.WriteString(s);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (string txt in TXT)
            {
                sb.AppendFormat("TXT \"{0}\"", txt);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
