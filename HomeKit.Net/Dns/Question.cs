namespace HomeKit.Net.Dns
{
    #region Rfc 1034/1035
    /*
    4.1.2. Question section format

    The question section is used to carry the "question" in most queries,
    i.e., the parameters that define what is being asked.  The section
    contains QDCOUNT (usually 1) entries, each of the following format:

                                        1  1  1  1  1  1
          0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                                               |
        /                     QNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                     QTYPE                     |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                     QCLASS                    |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    QNAME           a domain name represented as a sequence of labels, where
                    each label consists of a length octet followed by that
                    number of octets.  The domain name terminates with the
                    zero length octet for the null label of the root.  Note
                    that this field may be an odd number of octets; no
                    padding is used.

    QTYPE           a two octet code which specifies the type of the query.
                    The values for this field include all codes valid for a
                    TYPE field, together with some more general codes which
                    can match more than one type of RR.


    QCLASS          a two octet code that specifies the class of the query.
                    For example, the QCLASS field is IN for the Internet.
    */
    #endregion

    public class Question
    {
        string m_QName;
        public string QName
        {
            get
            {
                return m_QName;
            }
            set
            {
                m_QName = value;
                if (!m_QName.EndsWith(".", StringComparison.Ordinal))
                    m_QName += ".";
            }
        }
        public QType QType;
        public QClass QClass;

        public Question(string QName, QType QType, QClass QClass)
        {
            this.QName = QName;
            this.QType = QType;
            this.QClass = QClass;
        }

        public Question(RecordReader rr)
        {
            QName = rr.ReadDomainName();
            QType = (QType)rr.ReadUInt16();
            QClass = (QClass)rr.ReadUInt16();
        }

        public void Write(RecordWriter writer)
        {
            writer.WriteDomainNameUncompressed(QName);
            writer.WriteUint16((UInt16)QType);
            writer.WriteUint16((UInt16)QClass);
        }

        public override string ToString()
        {
            return string.Format("{0,-32} {1} {2}", QName, QClass, QType);
        }
    }
}
