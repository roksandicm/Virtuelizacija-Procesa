using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string Value { get; set; }

        public ValidationFault()
        {}

        public ValidationFault(string message, string fieldName, string value)
        {
            Message = message;
            FieldName = fieldName;
            Value = value;
        }
    }
}
