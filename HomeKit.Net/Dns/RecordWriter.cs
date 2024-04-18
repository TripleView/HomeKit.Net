using System.Text;

namespace HomeKit.Net.Dns
{
    public class RecordWriter
    {
        private List<byte> buffer = new List<byte>();

        public int Length
        {
            get { return buffer.Count; }
        }

        public byte GetByte(int index)
        {
            return buffer[index];
        }

        public byte[] GetData()
        {
            return buffer.ToArray();
        }

        public void WriteByte(byte x)
        {
            buffer.Add(x);
        }

        public void WriteData(byte[] data)
        {
            buffer.AddRange(data);
        }

        public void WriteUint16(UInt16 x)
        {
            buffer.Add((byte)(x >> 8));
            buffer.Add((byte)(x));
        }

        public void WriteUint32(UInt32 x)
        {
            buffer.Add((byte)(x >> 24));
            buffer.Add((byte)(x >> 16));
            buffer.Add((byte)(x >> 8));
            buffer.Add((byte)(x));
        }

        public void WriteDomainNameCompressed(string s)
        {
            List<byte> serial = WriteDomainNameUncompressed(new List<byte>(), s);

            int index = 0;
            while (serial[index] != 0)
            {
                // Can we represent the remaining tail portion using compression?
                // Search earlier for a matching tail pattern.
                if (FindTail(serial, index, out int position))
                {
                    // The remaining tail portion matches, so we can compress it.
                    buffer.Add((byte)(0xc0 | (position >> 8)));
                    buffer.Add((byte)position);
                    return;
                }

                // Write the next label to the output.
                buffer.Add(serial[index]);
                for (int k = 1; k <= serial[index]; ++k)
                    buffer.Add(serial[index + k]);

                // Skip to the next label, if any remain.
                index += 1 + serial[index];
            }

            // Completely uncompressed names need to end with a 0 terminator.
            buffer.Add(0);
        }

        private bool FindTail(List<byte> serial, int index, out int position)
        {
            // Domain names can be compressed by adding a pointer that
            // redirects to the rest of the string.  See:
            // https://datatracker.ietf.org/doc/html/rfc1035#section-4.1.4

            position = -1;

            int tailLength = serial.Count - index;
            if (tailLength < 3)
                return false;   // Tail compression would not make the packet shorter.

            // Compression cannot represent a position pointer beyond 0x3fff bytes.
            // And any position at or beyond 1+(buffer.Count-tailLength) will not
            // be long enough to hold the tail.
            int limit = Math.Min(0x3fff, 1 + (buffer.Count - tailLength));

            for (position = 0; position < limit; ++position)
            {
                bool match = true;
                for (int offset = 0; match && (offset < tailLength); ++offset)
                    if (buffer[position + offset] != serial[index + offset])
                        match = false;

                if (match)
                    return true;
            }

            return false;   // could not find a matching tail
        }

        public void WriteDomainNameUncompressed(string s)
        {
            WriteDomainNameUncompressed(buffer, s);
        }

        private static List<byte> WriteDomainNameUncompressed(List<byte> buffer, string s)
        {
            // Split the domain name into a series of labels that are delimited by ".".
            string[] list = s.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string label in list)
            {
                byte[] data = Encoding.UTF8.GetBytes(label);
                if (data.Length > 63)
                    throw new Exception($"Label [{label}] is longer than 63 bytes.");
                buffer.Add((byte)data.Length);
                for (int i = 0; i < data.Length; ++i)
                    buffer.Add(data[i]);
            }
            // Terminate the label list with a 0-length byte.
            buffer.Add(0);
            return buffer;
        }

        public void WriteString(string s)
        {
            byte[] data = Encoding.UTF8.GetBytes(s);
            buffer.Add((byte)data.Length);
            for (int i = 0; i < data.Length; ++i)
                buffer.Add(data[i]);
        }

        public void WriteRecord(Record rec)
        {
            // Reserve 2 bytes for the record length at the front.
            int front = buffer.Count;
            buffer.Add(0);
            buffer.Add(0);

            // Serialize the record into the buffer.
            rec.Write(this);

            // Calculate the number of bytes serialized.
            int length = (buffer.Count - front) - 2;
            if (length > 0xffff)
                throw new ArgumentException($"Record is too long: {length} bytes");

            // Store the length at the front.
            buffer[front] = (byte)(length >> 8);
            buffer[front+1] = (byte)(length);
        }
    }
}
