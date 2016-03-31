using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    [ServiceContract(CallbackContract = typeof(IChatClient),SessionMode =SessionMode.Required)]
    interface IChatServer
    {
        [OperationContract]
        bool ServerHeartbeat();

        [OperationContract(IsOneWay = false, IsInitiating = true)]
        bool Subscribe();

        [OperationContract(IsOneWay = false, IsTerminating = true)]
        bool Unsubscribe();

        [OperationContract]
        int SendMessage(Message value);

    }


    [DataContract]
    public class Message
    {
        string sender = string.Empty;
        string content = string.Empty;
        DateTime sentTime = DateTime.MinValue;
        string colour = "White";

        public int NumReceived { get; set; }

        [DataMember]
        public string Sender
        {
            get { return sender; }
            set { sender = value; }
        }

        [DataMember]
        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        [DataMember]
        public string Colour
        {
            get { return colour; }
            set { colour = value; }
        }

        [DataMember]
        public DateTime SentTime
        {
            get { return sentTime; }
            set { sentTime = value; }
        }
    }
}