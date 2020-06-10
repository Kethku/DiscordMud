using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordMud {
    public static class RemindMe {
        const string commandHook = "!remindme";
        public static void Handle(SocketMessage message) {
            ISocketMessageChannel channel = message.Channel;
            string author = message.Author.Username;
            string content = message.Content;

            if (content.StartsWith(commandHook)) {
                SetReminder(channel, author, content.Substring(commandHook.Length).Trim());
            }
        }

        public static async void SetReminder(ISocketMessageChannel channel, string author,  string commandArgument) {
            string numberPart = new string(commandArgument.TakeWhile(c => char.IsDigit(c)).ToArray());
            string wordsPart = commandArgument.Substring(numberPart.Length);

            if (!float.TryParse(numberPart, out float number)) {
                await channel.SendMessageAsync($"No number in command argument");
                return;
            }

            float multiplier = 0;
            switch (wordsPart.Trim().ToLower()) {
                case "second":
                case "seconds":
                    multiplier = 1000;
                    break;
                case "minute":
                case "minutes":
                    multiplier = 60000;
                    break;
                case "hour":
                case "hours":
                    multiplier = 3600000;
                    break;
                case "day":
                case "days":
                    multiplier = 86400000;
                    break;
                default:
                    await channel.SendMessageAsync($"I don't understand {wordsPart} as a unit of time, or it is too long of a period.");
                    return;
            }

            await channel.SendMessageAsync("I will remind you unless Keith turns his computer off...");
            await Task.Delay(TimeSpan.FromMilliseconds(number * multiplier));
            await channel.SendMessageAsync($"{author}, this is your reminder.");
        }
    }
}