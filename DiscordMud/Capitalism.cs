using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using Discord;
using Discord.WebSocket;
using MyCouch;
using MyCouch.Requests;

namespace DiscordMud {
    public class Member {
        public string Rev { get; set; }

        public string Id { get; set; }
        public string Name { get; set; }
        public Wallet Wallet { get; set; }

        public Member() {}

        public Member(string id, string name) { 
            Id = id;
            Name = name;
            Wallet = new Wallet();
        }

        public override string ToString() {
            return Name;
        }
    }

    public class Capitalism : CommandHandler {
        private static readonly Wallet ALLOWANCE = new Wallet { BBP = 100 };

        [Help("!balance: Prints your current wallet balance.")]
        public string Balance([Author]Member author) {
            return $"You have {author.Wallet}";
        }

        [Help("!give {reciever} {amount}: Gives the given person an amount of money. Example: !give keith 2 bbp 3 trips 1 quad")]
        public async Task<string> Give([Author]Member author, Member reciever, [Rest]string amountText, MyCouchStore db) {
            if (author.Id == reciever.Id) throw new CommandFailedException("You can't give to yourself.");

            var amount = new Wallet(amountText);
            author.Wallet.Subtract(amount);
            reciever.Wallet.Add(amount);
            await db.StoreAsync(author);
            await db.StoreAsync(reciever);
            return $"{author} has given {reciever} {amount.ToString()}.";
        }

        [Help("!spam {youtube url}: Spams a youtube video's audio to the assembly voice channel.")]
        public async Task<string> Spam([Author]Member author, MyCouchStore db, ISocketMessageChannel channel, SocketUserMessage message, [Rest]string youtubeUrl) {
            if (message.Author is SocketGuildUser discordAuthor && discordAuthor.VoiceChannel != null) {
                Spammable spammable;
                try {
                    spammable = await Spammable.Create(youtubeUrl);
                } catch (Exception exception) {
                    Console.WriteLine(exception);
                    return "Could not download youtube video. Maybe try a different video?";
                }

                var bbpCost = spammable.Length * 100 / 60;
                var walletCost = new Wallet(bbpCost + " BBP");
                author.Wallet.Subtract(walletCost);

                try {
                    await channel.SendMessageAsync($"{author.Name} has triggered a mic spam for {walletCost}.");
                    var client = await discordAuthor.VoiceChannel.ConnectAsync();
                    if (!await spammable.Spam(client)) {
                        return "Could not play youtube video in voice channel. Maybe try a different video.";
                    } else {
                        await db.StoreAsync(author);
                        return null;
                    }
                } finally {
                    await discordAuthor.VoiceChannel.DisconnectAsync();
                }
            } else {
                return "Could not connect to voice channel. Are you in a call?";
            }
        }

        [Secret]
        [Help("!grant {amount}: Gives yourself an amount of currency.")]
        public async Task<string> Grant([Author]Member author, MyCouchStore db, [Rest]string amountText) {
            var amount = new Wallet(amountText);
            author.Wallet.Add(amount);
            await db.StoreAsync(author);
            return $"You gave yourself {amount}.";
        }

        [Secret]
        [Help("!forfeit {amount}: Gives up an amount of currency.")]
        public async Task<string> Forfeit([Author]Member author, MyCouchStore db, [Rest]string amountText) {
            var amount = new Wallet(amountText);
            author.Wallet.Subtract(amount);
            await db.StoreAsync(author);
            return $"You gave up {amount}.";
        }

        [Secret]
        [Help("!onboard {id} {name}: Creates a new booty boy.")]
        public async Task<string> Onboard(string id, string name, MyCouchStore db) {
            await db.StoreAsync(new Member(id, name));
            return $"Created new booty boy with id {id} and name {name}";
        }

        public static async Task<bool> AddDubsToken(ulong id, int rank) {
            try {
                using (var db = Utils.OpenDatabase(Constants.DATABASE_NAME)) {
                    var member = await db.GetMember(id);
                    member.Wallet.Dubs[rank]++;
                    await db.StoreAsync(member);
                    return true;
                }
            } catch (Exception) {
                return false;
            }
        }

        public static async Task ManageAllowances(DiscordSocketClient client) {
            while (true) {
                var now = DateTime.Now;
                var midnight = (now - TimeSpan.FromHours(now.Hour)) - TimeSpan.FromMinutes(now.Minute);
                var fiveAm = midnight + TimeSpan.FromHours(5);
                if (fiveAm < DateTime.Now) {
                    fiveAm = fiveAm + TimeSpan.FromDays(1);
                }
                var timeTillFive = fiveAm - now;

                await Task.Delay(timeTillFive);
                
                var channel = (IMessageChannel)client.GetChannel(598338172958670862);
                using (var db = Utils.OpenDatabase(Constants.DATABASE_NAME)) {
                    var members = await db.GetAllAsync<Member>();

                    foreach (var member in members) {
                        member.Wallet.SubtractToZero(ALLOWANCE);
                        await db.StoreAsync(member);
                    }

                    await channel.SendMessageAsync($"Morning all. Everyone has been taxed {ALLOWANCE} for construction fees.");
                }
            }
        }
    }
}
