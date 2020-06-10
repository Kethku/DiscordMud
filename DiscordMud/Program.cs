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

            await Task.Delay(-1);
        }

        private static Task MessageRecieved(SocketMessage message) {
            RemindMe.Handle(message);
            CustomEmoji.Handle(message);
            Dubs.Handle(message);
            Console.WriteLine(message.Content);
            return Task.CompletedTask;
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
