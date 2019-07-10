using System.Collections.Generic;

namespace CoordEDBT
{
    public class Request
    {
        private string type;
        private Dictionary<string, object> parameters;

        public Request(string type)
        {
            this.type = type;
            parameters = new Dictionary<string, object>();
        }

        public string GetRequestType()
        {
            return type;
        }

        public void Add(string parameter, object value)
        {
            parameters.Add(parameter, value);
        }

        public Dictionary<string, object> GetParameters()
        {
            return parameters;
        }

        public object GetParameter(string name)
        {
            return parameters[name];
        }
    }
}