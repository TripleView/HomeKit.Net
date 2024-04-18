using System.Net;

/*
3.4.1. A RDATA format

+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
|                    ADDRESS                    |
+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

ADDRESS         A 32 bit Internet address.

Hosts that have multiple Internet addresses will have multiple A
records.
*
*/

namespace HomeKit.Net.Dns
{
    public class RecordA : Record
    {
        public byte[] data = new byte[4];
        public IPAddress Address;

        public override Type RecordType()
        {
            return Type.A;
        }

        public RecordA(RecordReader rr)
        {
            data[0] = rr.ReadByte();
            data[1] = rr.ReadByte();
            data[2] = rr.ReadByte();
            data[3] = rr.ReadByte();
            Address = new IPAddress(data);
        }

        public RecordA(byte[] addr)
        {
            if (addr == null || addr.Length != 4)
                throw new ArgumentException("IPv4 address must be 4 bytes.");

            data[0] = addr[0];
            data[1] = addr[1];
            data[2] = addr[2];
            data[3] = addr[3];
            Address = new IPAddress(data);
        }

        public override string ToString()
        {
            return Address.ToString();
        }

        public override void Write(RecordWriter rw)
        {
            rw.WriteByte(data[0]);
            rw.WriteByte(data[1]);
            rw.WriteByte(data[2]);
            rw.WriteByte(data[3]);
        }
    }
}
