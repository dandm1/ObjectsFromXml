using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.PerCall,ConcurrencyMode =ConcurrencyMode.Reentrant)]
    class Server : IChatServer
    {
        public bool ServerHeartbeat()
        {
            var serviceCallback = OperationContext.Current.GetCallbackChannel<IChatClient>();
            return ClientManager.Contact(serviceCallback);
        }

        public bool Subscribe()
        {
            var serviceCallback = OperationContext.Current.GetCallbackChannel<IChatClient>();
            return ClientManager.Add(serviceCallback);
        }

        public bool Unsubscribe()
        {
            var serviceCallback = OperationContext.Current.GetCallbackChannel<IChatClient>();
            return ClientManager.Remove(serviceCallback);
        }

        public int SendMessage(Message value)
        {
            int nAcknowledged = 0;
            Parallel.ForEach<IChatClient>
                (
                ClientManager.Clients,
                (x) => 
                {
                try
                    {
                        nAcknowledged += x.ResendMessage(value);
                    }
                    catch(Exception ex)
                    {
                        if (ex is System.ServiceModel.CommunicationObjectAbortedException)
                            ClientManager.Remove(x);
                    }
                });
                

            return nAcknowledged;
        }
    }
}
