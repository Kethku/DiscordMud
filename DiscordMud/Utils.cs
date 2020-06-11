using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MyCouch;

namespace DiscordMud {
    public static class Utils {
        public static IEnumerable<Tuple<T, T>> Pairs<T>(this IEnumerable<T> source) {
            using (var iterator = source.GetEnumerator()) {
                while (iterator.MoveNext()) {
                    var first = iterator.Current;
                    var second = iterator.MoveNext() ? iterator.Current : default(T);
                    yield return Tuple.Create(first, second);
                }
            }
        }

        // Will throw CommandFailedException if the name is invalid.
        // 0 means BBP
        public static int DubsNameToRank(string possibleName) {
            switch (possibleName) {
                case "bbp": return 0;
                case "trip": case "trips": return 3;
                case "quad": case "quads": return 4;
                case "quint": case "quints": return 5;
                case "hex": case "hexts": case "hexes": return 6;
                case "hept": case "hepts": return 7;
                case "oct": case "octs": return 8;
                case "non": case "nons": return 9;
                default:
                    throw new CommandFailedException($"{possibleName} is not a valid currency or token name.");
            }
        }

        // Will throw CommandFailedException if the rank isn't in the correct range (0 or 3 to 9).
        // 0 means BBP
        public static string RankToDubsName(int rank) {
            switch (rank) {
                case 0: return "bbp";
                case 3: return "trips";
                case 4: return "quads";
                case 5: return "quints";
                case 6: return "hexes";
                case 7: return "hepts";
                case 8: return "octs";
                case 9: return "nons";
                default:
                    throw new CommandFailedException($"{rank} is not a valid currency level.");
            }
        }

        public static async Task<Member> GetMember(this MyCouchStore db, string name) {
            name = name.ToLower();
            if (await db.ExistsAsync(name)) {
                return (await db.GetByIdAsync<Member>(name));
            } else {
                throw new CommandFailedException($"User with name {name} is not in the backend database... Go ask Keith about it");
            }
        }

        public static string CapitalizeFirstLetter(this string text) {
            return Char.ToUpper(text[0]) + text.Substring(1);
        }

        public static async Task ConfirmReact(this SocketUserMessage message) {
            await message.AddReactionAsync(new Emoji("✔️"));
        }

        public static async Task RejectReact(this SocketUserMessage message) {
            await message.AddReactionAsync(new Emoji("❌"));
        }
    }
}
