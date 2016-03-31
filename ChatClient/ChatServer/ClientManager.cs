using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class ClientManager
    {
        static Dictionary<IChatClient,DateTime> subscribers;

        public static IEnumerable<IChatClient> Clients
        {
            get { return subscribers.Keys; }
        }

        static ClientManager()
        {
            subscribers = new Dictionary<IChatClient, DateTime>();
        }

        public static bool Add(IChatClient newClient)
        {
            if (subscribers.Keys.Contains(newClient))
                return false;

            subscribers.Add(newClient,DateTime.Now);

            return true;
        }

        public static bool Remove(IChatClient removeClient)
        {
            if (!subscribers.Keys.Contains(removeClient))
                return false;

            subscribers.Remove(removeClient);

            return true;
        }

        public static bool Contact(IChatClient theClient)
        {
            if (!subscribers.ContainsKey(theClient))
                return false;

            subscribers[theClient] = DateTime.Now;
            return true;
        }
    }
}
