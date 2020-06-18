using System.Text;
using System.Threading.Tasks;
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
            {"wither", new Message("https://media.discordapp.net/attachments/598338172958670862/598556319334531073/WitherJon.gif") },
        };

        public static async Task Handle(SocketMessage message) {
            ISocketMessageChannel channel = message.Channel;

            string author = message.Author.Username;
            string content = message.Content;

            if (content.StartsWith("!help")) {
                var sb = new StringBuilder();
                sb.AppendLine("Custom Emoji");
                foreach (var command in messages.Keys) {
                    sb.AppendLine($"!{command}");
                }
                await channel.SendMessageAsync(sb.ToString());
            }

            foreach (var command in messages.Keys) {
                if (content.StartsWith("!" + command)) {
                    var commandMessage = messages[command];
                    await SendMessage(channel, commandMessage);
                }
            }
        }

        public static async Task SendMessage(ISocketMessageChannel channel, Message message) {
            var text = message.Text;
            await channel.SendMessageAsync(text);
            if (message.Mention != null) {
                await channel.SendMessageAsync($"<@{message.Mention}>");
            }
        }
    }
}
