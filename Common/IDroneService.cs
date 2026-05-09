using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface IDroneService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        SessionResponse StartSession(SessionMeta sessionMeta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        SessionResponse PushSample(DroneSample sample);

        [OperationContract]
        //[FaultContract(typeof(DataFormatFault))]
        //[FaultContract(typeof(ValidationFault))]
        SessionResponse EndSession();
    }
}
