using System;
using System.Text;
using System.Collections.Generic;

namespace DiscordMud {
    public class Wallet {
        public int BBP { get; set; }
        public Dictionary<int, int> Dubs { get; set; }

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
                    if (tokenAmount < 0) throw new CommandFailedException("You cannot deal in negative amounts...");
                    int rank = Utils.DubsNameToRank(typeText.ToLower().Trim());
                    if (rank == 0) {
                        BBP += tokenAmount;
                    } else {
                        Dubs[rank] = tokenAmount;
                    }
                } else {
                    throw new CommandFailedException("Invalid money amount.");
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
            bool any = false;
            var sb = new StringBuilder();
            if (BBP > 0) {
                sb.Append($"{BBP} BBP ");
                any = true;
            }
            
            for (int i = 3; i <= 9; i++) {
                var amount = Dubs[i];
                if (amount > 0) {
                    sb.Append($"{amount} {Utils.RankToDubsName(i, amount).CapitalizeFirstLetter()} ");
                    any = true;
                }
            }

            if (!any) {
                sb.Append($"nothing");
            }
            return sb.ToString().Trim();
        }
    }
}
