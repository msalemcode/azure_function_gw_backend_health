using System;
using System.Collections.Generic;
using System.Text;

namespace GatwaybackendHealth.Models
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class BackendAddressPool2
    {
        public string id { get; set; }
        public string getId() 
        {
            String[]  myId = id.Split("/");
            return String.Concat(id[8],"/",id[10]);
        }
    }

    public class BackendHttpSettings
    {
        public string id { get; set; }
        public string getId() 
        {
            String[]  myId = id.Split("/");
            return String.Concat(id[10]);

        }
    }

    public class Server
    {
        public string address { get; set; }
        public string health { get; set; }
    }

    public class BackendHttpSettingsCollection
    {
        public BackendHttpSettings backendHttpSettings { get; set; }
        public List<Server> servers { get; set; }
    }

    public class BackendAddressPool
    {
        public BackendAddressPool2 backendAddressPool { get; set; }
        public List<BackendHttpSettingsCollection> backendHttpSettingsCollection { get; set; }
    }

    public class Root
    {
        public List<BackendAddressPool> backendAddressPools { get; set; }
    }


}
