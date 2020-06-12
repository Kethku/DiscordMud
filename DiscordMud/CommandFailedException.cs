using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MyCouch;
using MyCouch.Requests;

namespace DiscordMud {
    public class CommandFailedException : Exception {
        public string Reason { get; set; }

        public CommandFailedException(string reason) {
            Reason = reason;
        }
    }
}
