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
    public class Wallet {
        public int BBP { get; set; }
        public Dictionary<int, int> Dubs { get; set;}

        public Wallet() {
            BBP = 0;
            Dubs = new Dictionary<int, int> {
                [3] = 0,
                [4] = 0,
                [5] = 0,
                [6] = 0,
                [7] = 0,
                [8] = 0,
                [9] = 0
            };
        }

        public Wallet(string textAmount) {
            var parts = textAmount.Split(' ');
            if (parts.Length == 1 && int.TryParse(parts[0], out var amount)) {
                BBP = amount;
            }

            Dubs = new Dictionary<int, int> {
                [3] = 0,
                [4] = 0,
                [5] = 0,
                [6] = 0,
                [7] = 0,
                [8] = 0,
                [9] = 0
            };

            var pairs = parts.Pairs();
            foreach ((string amountText, string typeText) in pairs) {
                if (int.TryParse(amountText, out var tokenAmount)) {
                    int rank = Utils.DubsNameToRank(typeText.ToLower().Trim());
                    if (rank == 0) {
                        BBP += tokenAmount;
                    } else {
                        Dubs[rank] = tokenAmount;
                    }
                }
            }
        }

        public void Add(Wallet amountToAdd) {
            BBP += amountToAdd.BBP;
            for (int i = 3; i <= 9; i++) {
                Dubs[i] += amountToAdd.Dubs[i];
            }
        }

        public void Subtract(Wallet amountToSubtract) {
            if (BBP >= amountToSubtract.BBP) {
                BBP -= amountToSubtract.BBP;
            } else {
                throw new CommandFailedException("Insufficient BBP");
            }

            for (int i = 3; i <= 9; i++) {
                if (Dubs[i] >= amountToSubtract.Dubs[i]) {
                    Dubs[i] -= amountToSubtract.Dubs[i];
                } else {
                    throw new CommandFailedException($"Insufficient dubs tokens of rank {i}");
                }
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append($"{BBP} BBP ");
            for (int i = 3; i <= 9; i++) {
                var amount = Dubs[i];
                if (amount > 0) {
                    sb.Append($"{amount} {Utils.RankToDubsName(i).CapitalizeFirstLetter()}");
                }
            }
            return sb.ToString().Trim();
        }
    }

    public class Member {
        public string Rev { get; set; }

        public string Id { get; set; }
        public Wallet Wallet { get; set; }

        public Member() {}

        public Member(string name) { 
            Id = name.ToLower();
            Wallet = new Wallet();
        }
    }

    public class CommandFailedException : Exception {
        public string Reason { get; set; }

        public CommandFailedException(string reason) {
            Reason = reason;
        }
    }

    public static class Capitalism {
        const string COUCHDB_URL = "http://02credits.ddns.net:5984";
        const string DATABASE_NAME = "booty_boy_capitalism";
        private static readonly Wallet ALLOWANCE = new Wallet { BBP = 100 };

        public static async Task<string> GetBalance(string name) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                return (await db.GetMember(name)).Wallet.ToString();
            }
        }

        public static async Task AddDubsToken(string name, int rank) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                var member = await db.GetMember(name);
                member.Wallet.Dubs[rank]++;
                await db.StoreAsync(member);
            }
        }

        public static async Task<string> Transfer(string fromName, string toName, string amountText) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                var fromMember = await db.GetMember(fromName);
                var toMember = await db.GetMember(toName);
                var amount = new Wallet(amountText);
                Console.WriteLine(amount.ToString());

                fromMember.Wallet.Subtract(amount);
                toMember.Wallet.Add(amount);

                await db.StoreAsync(fromMember);
                await db.StoreAsync(toMember);

                return $"{fromMember.Id} has given {toMember.Id} {amount.ToString()}.";
            }
        }

        public static async Task Handle(SocketUserMessage message) {
            ISocketMessageChannel channel = message.Channel;

            string author = ((IGuildUser)message.Author).Nickname;
            string content = message.Content;

            try {
                if (content == "!balance") {
                    var balance = await GetBalance(author);
                    await channel.SendMessageAsync($"You have {balance}.");
                    await message.ConfirmReact();
                }

                if (content.StartsWith("!give")) {
                    var parts = content
                        .Substring(5)
                        .Split(" ")
                        .Select(part => part.Trim())
                        .Where(part => !string.IsNullOrWhiteSpace(part))
                        .ToArray();

                    var toName = parts[0];
                    var rest = string.Join(" ", parts.Skip(1));
                    Console.WriteLine($"author: {author} toName: {toName} rest: {rest}");
                    await channel.SendMessageAsync(await Transfer(author, toName, rest));
                    await message.ConfirmReact();
                }
            } catch (CommandFailedException exception) {
                await channel.SendMessageAsync(exception.Reason);
                await message.RejectReact();
            }
        }

        public static async Task GiveAllowances(IMessageChannel channel) {
            using (var db = new MyCouchStore(COUCHDB_URL, DATABASE_NAME)) {
                var query = new Query(SystemViewIdentity.AllDocs).Configure(config => config.IncludeDocs(true));
                var members = (await db.QueryAsync<dynamic, Member>(query)).Select(row => row.IncludedDoc);

                foreach (var member in members) {
                    member.Wallet.Add(ALLOWANCE);
                    db.StoreAsync(member);
                }

                await channel.SendMessageAsync($"Morning all. Everyone has been given {ALLOWANCE}. Spend it wisely");
            }
        }
    }
}
