using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Emoji = Discord.Emoji;


namespace DiscordMud {
    public static class Dubs {
        private static Random random = new Random();

        public static async Task Handle(SocketUserMessage message) {
            if (message.Author.Id == 598740888562302977) return;
            ISocketMessageChannel channel = message.Channel;

            if (!(channel is SocketGuildChannel) || (channel as SocketGuildChannel).Guild.Id != 598338172958670858) {
                return;
            }

            var id = new int[9];
            for (var i = 0; i < 9; i++) {
                id[i] = random.Next(9);
            }

            var lastDigit = id[8];
            var rank = 1;
            for (var i = 7; i >= 0; i--) {
                if (lastDigit != id[i]) {
                    break;
                }
                rank++;
            }

            Emoji emoji = null;

            switch (rank) {
                case 3:
                    emoji = new Emoji("3️⃣");
                    break;
                case 4:
                    emoji = new Emoji("4️⃣");
                    break;
                case 5:
                    emoji = new Emoji("5️⃣");
                    break;
                case 6:
                    emoji = new Emoji("6️⃣");
                    break;
                case 7:
                    emoji = new Emoji("7️⃣");
                    break;
                case 8:
                    emoji = new Emoji("8️⃣");
                    break;
                case 9:
                    emoji = new Emoji("9️⃣");
                    break;
            }

            if (emoji != null) {
                if (await Capitalism.AddDubsToken(message.Author.Id, rank)) {
                    await message.AddReactionAsync(emoji);
                }
            }
        }
    }
}
