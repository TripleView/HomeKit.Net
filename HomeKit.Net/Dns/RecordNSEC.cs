namespace HomeKit.Net.Dns
{
    public class RecordNSEC : Record
    {
        // https://datatracker.ietf.org/doc/html/rfc4034#page-12
        // 1. Next Domain Name: string
        // 2. Type Bit Maps: ( Window Block # | Bitmap Length | Bitmap )+
        public string NextDomainName;
        public HashSet<Type> IncludedRecordTypes;

        public override Type RecordType()
        {
            return Type.NSEC;
        }

        public RecordNSEC(RecordReader rr, int totalRecordLength)
        {
            int posBeforeName = rr.Position;
            NextDomainName = rr.ReadDomainName();
            int nameLength = rr.Position - posBeforeName;
            int bitmapLength = totalRecordLength - nameLength;

            IncludedRecordTypes = new HashSet<Type>();
            int i = 0;
            while (i < bitmapLength)
            {
                if (i >= bitmapLength)
                    return;     // prevent trying to read past end of data

                int window = rr.ReadByte();
                ++i;

                if (i >= bitmapLength)
                    return;     // prevent trying to read past end of data

                int length = rr.ReadByte();
                ++i;

                for (int n = 0; n < length; ++n)
                {
                    if (i >= bitmapLength)
                        return;     // prevent trying to read past end of data

                    int data = rr.ReadByte();
                    ++i;

                    for (int bit = 0; bit < 8; ++bit)
                    {
                        if (0 != (data & (1 << bit)))
                        {
                            Type type = (Type)(bit + (n * 8) + (window * 256));
                            IncludedRecordTypes.Add(type);
                        }
                    }
                }
            }
        }

        public RecordNSEC(string nextDomainName, params Type[] typeList)
        {
            NextDomainName = nextDomainName;
            IncludedRecordTypes = new HashSet<Type>(typeList);
        }

        public override void Write(RecordWriter rw)
        {
            if (IncludedRecordTypes == null || IncludedRecordTypes.Count == 0)
                throw new Exception("NSEC record must contain at least one record type.");

            rw.WriteDomainNameUncompressed(NextDomainName);

            var map = new byte[256, 32];
            foreach (Type t in IncludedRecordTypes)
            {
                int n = (int)t;
                if (n < 0 || n > 0xffff)
                    throw new Exception($"Invalid type value: {n}");
                int window = n >> 8;
                int index = n & 0xff;
                int slot = index >> 3;
                int bit = index & 7;
                map[window, slot] |= (byte)(1 << bit);
            }

            for (int window = 0; window < 256; ++window)
            {
                int length = 0;
                for (int slot = 0; slot < 32; ++slot)
                    if (map[window, slot] != 0)
                        length = slot + 1;

                if (length > 0)
                {
                    rw.WriteByte((byte)window);
                    rw.WriteByte((byte)length);
                    for (int slot = 0; slot < length; ++slot)
                        rw.WriteByte(map[window, slot]);
                }
            }
        }

        public override string ToString()
        {
            return "NSEC " + NextDomainName + " [" + string.Join(", ", IncludedRecordTypes.OrderBy(t => t)) + "]";
        }
    }
}
