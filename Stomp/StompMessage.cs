using System.Collections.Generic;

namespace StompLibrary
{
    public class StompMessage
    {
        private readonly Dictionary<string, string> _headers;

      
        public StompMessage(string command)
            : this(command, string.Empty)
        {
        }
        
        public StompMessage(string command, string body)
            : this(command, body, new Dictionary<string, string>())
        {
        }

        internal StompMessage(string command, string body, Dictionary<string, string> headers)
        {
            Command = command;
            Body = body;
            _headers = headers;

            this["content-length"] = body.Length.ToString();
        }

        public Dictionary<string, string> Headers
        {
            get { return _headers; }
        }

        public string Body { get; }
        public string Command { get; }
        public string this[string header]
        {
            get { return _headers.ContainsKey(header) ? _headers[header] : string.Empty; }
            set { _headers[header] = value; }
        }

    }
}
