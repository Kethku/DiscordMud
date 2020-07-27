using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordMud {
    public static class Purge {
        public static Dictionary<ulong, int> purgeCounts = new Dictionary<ulong, int>();

        public static async void Added(ulong messageId, SocketReaction reaction) {
            if (reaction.Emote.Name == "purge") {
                int purgeCount = 0;
                purgeCounts.TryGetValue(messageId, out purgeCount);
                purgeCount++;

                int limit = 3;
                if (reaction.Channel.Id == 598667473012785161) {
                    limit = 5;
                }

                if (purgeCount >= limit) {
                    var message = await reaction.Channel.GetMessageAsync(messageId);
                    await reaction.Channel.DeleteMessageAsync(message);
                } else {
                    purgeCounts[messageId] = purgeCount;
                }
            }
        }

        public static void Removed(ulong messageId, SocketReaction reaction) {
            if (reaction.Emote.Name == "purge") {
                int purgeCount = 0;
                purgeCounts.TryGetValue(messageId, out purgeCount);
                purgeCount--;
                purgeCounts[messageId] = purgeCount;
            }
        }
    }
}
