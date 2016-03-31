using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Threading;
using System.ServiceModel;
using ChatClient.MessageServer;
using System.Net;
using System.Net.Sockets;
using System.Windows.Data;

namespace ChatClient
{
    public class ChatModel : IDisposable
    {
        private ObservableCollection<MessageServer.Message> _messages;
        private MessageServer.ChatServerClient _server;
        private Object _messagesLock;
        public ChatModel()
        {
            _messages = new ObservableCollection<MessageServer.Message>();
            _messagesLock = new object();
            BindingOperations.EnableCollectionSynchronization(_messages, _messagesLock);
            var callback = new ServiceCallback(this);
            var context = new InstanceContext(callback);
            _server = new MessageServer.ChatServerClient(context);

            //var callbackBinding = _server.Endpoint.Binding as WSDualHttpBinding;
            //int port = 8009;//GetPort();
            //callbackBinding.ClientBaseAddress = new Uri("http://localhost:" + port + "/");
            _server.Subscribe();
        }

        private static int GetPort()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(endpoint);
                IPEndPoint local = socket.LocalEndPoint as IPEndPoint;
                return local.Port;
            }

        }

        public ObservableCollection<MessageServer.Message> Messages
        {
            get { return _messages;}
            set { _messages = value; }
        }

        internal void SendMessage(string _name, string _message, string _colour)
        {
            var fullMessage = new MessageServer.Message()
            {
                Content = _message,
                Sender = _name,
                Colour = _colour,
                SentTime = DateTime.Now
            };

            var send = _server.SendMessageAsync(fullMessage);
            send.Wait(5000);
            var returned = send.Result;

            //Messages.Add(fullMessage);
        }

        internal void ReceiveMessage(MessageServer.Message theMessage)
        {
                Messages.Add(theMessage);
            
        }

        public void Dispose()
        {
            if (_server != null)
                _server.Unsubscribe();

            _server = null;
        }

        [CallbackBehaviorAttribute(UseSynchronizationContext = false)]
        public class ServiceCallback : MessageServer.IChatServerCallback
        {
            ChatModel _parent;
            public ServiceCallback( ChatModel parent)
            {
                _parent = parent;
            }

            public bool ClientHeartbeat()
            {
                return true;
            }

            public int ResendMessage(Message value)
            {
               
                _parent.ReceiveMessage(value);
                return 1;
               
            }
        }
    }
}
