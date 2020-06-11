using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordMud {
    class Program {
        private static DiscordSocketClient client;

        static async Task Main(string[] args) {
            client = new DiscordSocketClient();
            client.Log += Log;
            client.MessageReceived += MessageRecieved;
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;

            string token = File.ReadAllText("./Discord.token");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            while (true) {

                var now = DateTime.Now;
                var midnight = (now - TimeSpan.FromHours(now.Hour)) - TimeSpan.FromMinutes(now.Minute);
                var fiveAm = midnight + TimeSpan.FromHours(5);
                if (fiveAm < DateTime.Now) {
                    fiveAm = fiveAm + TimeSpan.FromDays(1);
                }
                var timeTillFive = fiveAm - now;

                Console.WriteLine(timeTillFive);

                await Task.Delay(timeTillFive);
                await Capitalism.GiveAllowances((IMessageChannel)client.GetChannel(598338172958670862));
            }
        }

        private static async Task MessageRecieved(SocketMessage message) {
            var userSocketMessage = message as SocketUserMessage;
            if (userSocketMessage != null) {
                CustomEmoji.Handle(userSocketMessage);
                Dubs.Handle(userSocketMessage);
                await Capitalism.Handle(userSocketMessage);
                Console.WriteLine(message.Content);
            }
        }

        private static Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            Purge.Added(message.Id, reaction);
            return Task.CompletedTask;
        }

        private static Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            Purge.Removed(message.Id, reaction);
            return Task.CompletedTask;
        }

        private static Task Log(LogMessage message) {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }
}
