using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public int LineNumber { get; set; }

        public DataFormatFault()
        {}

        public DataFormatFault(string message, string fieldName, int lineNumber)
        {
            Message = message;
            FieldName = fieldName;
            LineNumber = lineNumber;
        }
    }
}
