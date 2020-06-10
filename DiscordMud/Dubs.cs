using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Emoji = Discord.Emoji;


namespace DiscordMud {
    public static class Dubs {
        private static Random random = new Random();

        public static async void Handle(SocketMessage message) {
            var userSocketMessage = message as SocketUserMessage;
            if (userSocketMessage != null) {
                ISocketMessageChannel channel = userSocketMessage.Channel;

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
                    await userSocketMessage.AddReactionAsync(emoji);
                    BBPs.AddDubs(userSocketMessage.Author.Nickname, rank);
                }
            }
        }
    }
}
