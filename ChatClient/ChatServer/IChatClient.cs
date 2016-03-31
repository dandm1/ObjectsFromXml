using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace ChatServer
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IChatClient
    {
        [OperationContract]
        int ResendMessage(Message value);

        [OperationContract]
        bool ClientHeartbeat();
    }
    
}
