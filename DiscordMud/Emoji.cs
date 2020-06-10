using System.Collections.Generic;
using Discord.WebSocket;

namespace DiscordMud {
    public class Message {
        public string Text { get; set; }
        public string Mention { get; set; }

        public Message(string text) {
            Text = text;
            Mention = null;
        }

        public Message(string text, string mention) {
            Text = text;
            Mention = mention;
        }
    }

    public static class CustomEmoji {
        static Dictionary<string, Message> messages = new Dictionary<string, Message>() {
            {"sidde", new Message("https://i.imgur.com/PASMAib.png", "143979701960966144") },
            {"wither", new Message("Wither temp") },
        };

        public static void Handle(SocketMessage message) {
            ISocketMessageChannel channel = message.Channel;

            string author = message.Author.Username;
            string content = message.Content;

            foreach (var command in messages.Keys) {
                if (content.StartsWith("!" + command)) {
                    var commandMessage = messages[command];
                    SendMessage(channel, commandMessage);
                }
            }
        }

        public static async void SendMessage(ISocketMessageChannel channel, Message message) {
            var text = message.Text;
            await channel.SendMessageAsync(text);
            if (message.Mention != null) {
                await channel.SendMessageAsync($"<@{message.Mention}>");
            }
        }
    }
}
