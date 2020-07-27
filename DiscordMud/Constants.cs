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
    public static class Constants {
        public const string COUCHDB_URL = "localhost:5984";
        public const string DATABASE_NAME = "booty_boy_capitalism";
        public const string LOOT_DATABASE_SUFFIX = "_loot";
    }
}
