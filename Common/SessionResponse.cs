using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SessionResponse
    {
        [DataMember]
        public bool Ack { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public SessionStatus Status { get; set; }

        public SessionResponse()
        {
        }

        public SessionResponse(
            bool ack,
            string message,
            SessionStatus status)
        {
            Ack = ack;
            Message = message;
            Status = status;
        }
    }
}
