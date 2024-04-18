using System.Text;

namespace HomeKit.Net.Dns
{
    public class UnsupportedDomainNameCompressionException : Exception
    {
        public readonly int Offset;

        public UnsupportedDomainNameCompressionException(int offset)
            : base($"Unsupported domain name compression type at offset {offset}")
        {
            Offset = offset;
        }
    }

    public class RecordReader
    {
        private byte[] m_Data;
        private int m_Position;
        private bool enableSecurityExtensions;

        public RecordReader(byte[] data, bool enableSecurityExtensions)
        {
            m_Data = data;
            this.enableSecurityExtensions = enableSecurityExtensions;
        }

        public int Position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public int Length
        {
            get
            {
                if (m_Data == null)
                    return 0;
                else
                    return m_Data.Length;
            }
        }

        public RecordReader(byte[] data, int Position)
        {
            m_Data = data;
            m_Position = Position;
        }

        public byte ReadByte()
        {
            if (m_Position < 0 || m_Position >= m_Data.Length)
                return 0;
            else
                return m_Data[m_Position++];
        }

        private byte AccessByte(int position)
        {
            if (position < 0 || position >= m_Data.Length)
                return 0;
            else
                return m_Data[position];
        }

        public UInt16 ReadUInt16()
        {
            byte hi = ReadByte();
            byte lo = ReadByte();
            return (UInt16)((hi << 8) | lo);
        }

        public UInt32 ReadUInt32()
        {
            UInt16 hi = ReadUInt16();
            UInt16 lo = ReadUInt16();
            return (UInt32)((hi << 16) | lo);
        }

        public string ReadDomainName()
        {
            // A domain name is a series of zero or more "labels".
            // Each label has a length prefix byte.
            // If the high two bits are set for the length prefix, it indicates
            // that we are to copy a susbstring from an earlier part of the same packet.
            // Otherwise, the text bytes follow the length byte.
            // After each label we append ".".

            var bytes = new List<byte>();
            int length = 0;
            string name;

            // Get the length of the next label.
            while ((length = ReadByte()) != 0)
            {
                // Top 2 bits set denotes domain name compression and to reference elsewhere.
                if ((length & 0xc0) == 0xc0)
                {
                    // The actual label text lives at an earlier position in this same packet.
                    int position = ((length & 0x3f) << 8) | ReadByte();
                    if (position < 0 || position >= m_Position - 2)
                        throw new Exception($"Invalid domain name compression reference: position=0x{position:x}, m_Position=0x{m_Position:x}");

                    // Total hack: fake like we are reading from that position.
                    int savePosition = m_Position;
                    m_Position = position;
                    string tail = ReadDomainName();

                    // Restore the object back to its original state.
                    m_Position = savePosition;

                    string head = (bytes.Count == 0) ? "" : Encoding.UTF8.GetString(bytes.ToArray());
                    name = head + tail;
                    return name;
                }

                if ((length & 0xc0) != 0)
                {
                    // There are other encoding formats defined, but we do not support them.
                    // I mentioned this here on GitHub:
                    // https://github.com/novotnyllc/Zeroconf/issues/226
                    throw new UnsupportedDomainNameCompressionException(m_Position - 1);
                }

                // Not using compression, so copy the next label over.
                while (length > 0)
                {
                    bytes.Add(ReadByte());
                    --length;
                }
                bytes.Add((byte)'.');
            }

            name = (bytes.Count == 0) ? "." : Encoding.UTF8.GetString(bytes.ToArray());
            return name;
        }

        public string ReadString()
        {
            short length = ReadByte();
            var bytes = new List<byte>();
            for (int i=0; i<length; i++)
                bytes.Add(ReadByte());
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        public byte[] ReadBytes(int intLength)
        {
            byte[] list = new byte[intLength];
            for (int intI = 0; intI < intLength; intI++)
                list[intI] = ReadByte();
            return list;
        }

        public Record ReadRecord(Type type, int Length)
        {
            int pos_before = Position;

            Record rec;
            switch (type)
            {
                case Type.A:
                    rec = new RecordA(this);
                    break;

                case Type.PTR:
                    rec =  new RecordPTR(this);
                    break;

                case Type.TXT:
                    rec = new RecordTXT(this, Length);
                    break;

                case Type.AAAA:
                    rec = new RecordAAAA(this);
                    break;

                case Type.SRV:
                    rec = new RecordSRV(this);
                    break;

                case Type.NSEC:
                    if (enableSecurityExtensions)
                        rec = new RecordNSEC(this, Length);
                    else
                        rec = new RecordUnknown(this, type, Length);
                    break;

                default:
                    rec = new RecordUnknown(this, type, Length);
                    break;
            }

            int bytesRead = Position - pos_before;
            if (bytesRead != Length)
                throw new Exception($"Number of bytes read = {bytesRead}, but declared length = {Length}");

            return rec;
        }
    }
}
