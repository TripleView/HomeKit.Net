namespace HomeKit.Net.Dns
{
    public class RecordUnknown : Record
    {
        public Type TYPE;
        public byte[] RDATA;

        public override Type RecordType()
        {
            return TYPE;
        }

        public RecordUnknown(RecordReader rr, Type type, int recordLength)
        {
            TYPE = type;
            RDATA = rr.ReadBytes(recordLength);
        }

        public override void Write(RecordWriter rw)
        {
            if (RDATA != null)
                for (int i = 0; i < RDATA.Length; ++i)
                    rw.WriteByte(RDATA[i]);
        }

        public override string ToString()
        {
            if (RDATA == null)
                return "RDATA = null";
            return "RDATA = [" + string.Join(" ", RDATA.Select(b => b.ToString("x2"))) + "]";
        }
    }
}
