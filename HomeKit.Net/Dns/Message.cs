namespace HomeKit.Net.Dns
{
    public class Message
    {
        public Header header;
        public List<Question> Questions;
        public List<RR> Answers;
        public List<RR> Authorities;
        public List<RR> Additionals;
        public DateTime TimeStamp;

        public Message()
        {
            Questions = new List<Question>();
            Answers = new List<RR>();
            Authorities = new List<RR>();
            Additionals = new List<RR>();
            TimeStamp = DateTime.Now;
            header = new Header();
        }

        public Message(byte[] data, bool enableSecurityExtensions)
        {
            TimeStamp = DateTime.Now;
            var rr = new RecordReader(data, enableSecurityExtensions);

            Questions = new List<Question>();
            Answers = new List<RR>();
            Authorities = new List<RR>();
            Additionals = new List<RR>();

            header = new Header(rr);

            for (int i = 0; i < header.QDCOUNT; i++)
                Questions.Add(new Question(rr));

            for (int i = 0; i < header.ANCOUNT; i++)
                Answers.Add(new RR(rr));

            for (int i = 0; i < header.NSCOUNT; i++)
                Authorities.Add(new RR(rr));

            for (int i = 0; i < header.ARCOUNT; i++)
                Additionals.Add(new RR(rr));
        }

        public void Write(RecordWriter rw)
        {
            header.QDCOUNT = CheckUInt16(Questions.Count);
            header.ANCOUNT = CheckUInt16(Answers.Count);
            header.NSCOUNT = CheckUInt16(Authorities.Count);
            header.ARCOUNT = CheckUInt16(Additionals.Count);
            header.Write(rw);

            foreach (Question q in Questions)
                q.Write(rw);

            foreach (RR a in Answers)
                a.Write(rw);

            foreach (RR a in Authorities)
                a.Write(rw);

            foreach (RR a in Additionals)
                a.Write(rw);
        }

        private static ushort CheckUInt16(int value)
        {
            if (value < ushort.MinValue || value > ushort.MaxValue)
                throw new ArgumentException($"Value is outside the range allowed for uint16: {value}");

            return (ushort)value;
        }
    }
}
