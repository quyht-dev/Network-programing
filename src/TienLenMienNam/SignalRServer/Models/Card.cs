// CardGameServer/Models/Card.cs
using System;

namespace CardGameServer.Models
{
    public enum Suit
    {
        Spades = 0,   // Bích
        Clubs = 1,    // Tép
        Diamonds = 2, // Rô
        Hearts = 3    // Cơ
    }

    /// <summary>
    /// Rank theo Tiến Lên: 3..A..2
    /// 3=3 ... 10=10, 11=J, 12=Q, 13=K, 14=A, 15=2
    /// </summary>
    public sealed class Card : IComparable<Card>
    {
        public int Rank { get; private set; }   // 3..15
        public Suit Suit { get; private set; }

        public Card(int rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public int Power
        {
            get
            {
                // cùng Rank thì chất quyết định (Bích < Tép < Rô < Cơ)
                return (Rank * 10) + (int)Suit;
            }
        }

        public int CompareTo(Card other)
        {
            if (other == null) return 1;
            return Power.CompareTo(other.Power);
        }

        public override string ToString()
        {
            return ToCode();
        }

        public string ToCode()
        {
            // Ví dụ: 3S, TD, AH, 2H
            char r;
            switch (Rank)
            {
                case 10: r = 'T'; break;
                case 11: r = 'J'; break;
                case 12: r = 'Q'; break;
                case 13: r = 'K'; break;
                case 14: r = 'A'; break;
                case 15: r = '2'; break;
                default:
                    if (Rank >= 3 && Rank <= 9) r = Rank.ToString()[0];
                    else throw new InvalidOperationException("Invalid rank: " + Rank);
                    break;
            }

            char s;
            switch (Suit)
            {
                case Suit.Spades: s = 'S'; break;
                case Suit.Clubs: s = 'C'; break;
                case Suit.Diamonds: s = 'D'; break;
                case Suit.Hearts: s = 'H'; break;
                default: throw new InvalidOperationException("Invalid suit: " + Suit);
            }

            return new string(new[] { r, s });
        }

        public static Card FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length != 2)
                throw new FormatException("Card code must be 2 chars, e.g. 3S, TD, AH.");

            int rank;
            char r = code[0];
            switch (r)
            {
                case '3': rank = 3; break;
                case '4': rank = 4; break;
                case '5': rank = 5; break;
                case '6': rank = 6; break;
                case '7': rank = 7; break;
                case '8': rank = 8; break;
                case '9': rank = 9; break;
                case 'T': rank = 10; break;
                case 'J': rank = 11; break;
                case 'Q': rank = 12; break;
                case 'K': rank = 13; break;
                case 'A': rank = 14; break;
                case '2': rank = 15; break;
                default: throw new FormatException("Invalid rank char: " + r);
            }

            Suit suit;
            char s = code[1];
            switch (s)
            {
                case 'S': suit = Suit.Spades; break;
                case 'C': suit = Suit.Clubs; break;
                case 'D': suit = Suit.Diamonds; break;
                case 'H': suit = Suit.Hearts; break;
                default: throw new FormatException("Invalid suit char: " + s);
            }

            return new Card(rank, suit);
        }
    }
}
