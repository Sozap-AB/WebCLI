using System;
using System.Collections.Generic;

namespace WebCli
{
    public class WebCliOptions
    {
        public string Path { get; }
        public IEnumerable<Type> CommandTypes { get; }

        public string Prompt { get; }
        public string Greetings { get; }
        public bool UseCDN { get; }

        public TimeSpan? HeartbeatInterval { get; }

        public WebCliOptions(
            IEnumerable<Type> commandTypes,
            string path = "/webcli",
            string greetings = "Welcome to WebCLI",
            string prompt = "admin",
            TimeSpan? heartbeatInterval = null,
            bool useCDN = true
        )
        {
            Path = path;
            CommandTypes = commandTypes;
            Prompt = prompt;
            Greetings = greetings;
            HeartbeatInterval = heartbeatInterval;
            UseCDN = useCDN;
        }
    }
}
