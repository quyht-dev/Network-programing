// CardGameClient/Models/Card.cs
using System;

namespace CardGameClient.Models
{
    public enum Suit
    {
        Spades = 0,   // bích
        Clubs = 1,    // tép/chuồn
        Diamonds = 2, // rô
        Hearts = 3    // cơ
    }

    public sealed class Card : IComparable<Card>
    {
        public int Rank { get; private set; } // 3..15 (Trong đó 2 được quy đổi là 15)
        public Suit Suit { get; private set; }

        public Card(int rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        // Logic tính điểm: Rank quan trọng hơn, Suit để phá hoà
        public int Power => (Rank * 10) + (int)Suit;

        public int CompareTo(Card other)
        {
            if (other == null) return 1;
            return Power.CompareTo(other.Power);
        }

        // --- Dùng để gửi qua mạng (Giữ nguyên logic cũ) ---
        public string ToCode()
        {
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
                throw new FormatException("Card code must be 2 chars.");

            int rank;
            switch (code[0])
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
                default: throw new FormatException("Invalid rank char: " + code[0]);
            }

            Suit suit;
            switch (code[1])
            {
                case 'S': suit = Suit.Spades; break;
                case 'C': suit = Suit.Clubs; break;
                case 'D': suit = Suit.Diamonds; break;
                case 'H': suit = Suit.Hearts; break;
                default: throw new FormatException("Invalid suit char: " + code[1]);
            }

            return new Card(rank, suit);
        }

        // --- SỬA ĐỔI QUAN TRỌNG NHẤT Ở ĐÂY ---
        /// <summary>
        /// Map sang tên file ảnh khớp với thư mục Resources của bạn.
        /// Định dạng: "{rankName}_of_{suitName}.png"
        /// Ví dụ: "2_of_clubs.png", "jack_of_diamonds.png"
        /// </summary>
        public string ToImageFileName()
        {
            // 1. Lấy tên chất (Suit)
            string suitName = Suit switch
            {
                Suit.Clubs => "clubs",
                Suit.Diamonds => "diamonds",
                Suit.Hearts => "hearts",
                _ => "spades"
            };

            // 2. Lấy tên số (Rank) - Lưu ý các quân J, Q, K, A dùng tên tiếng Anh
            string rankName = Rank switch
            {
                11 => "jack",  // J
                12 => "queen", // Q
                13 => "king",  // K
                14 => "ace",   // A
                15 => "2",     // Heo (Logic game là 15, nhưng ảnh tên là 2)
                _ => Rank.ToString() // Các số 3, 4... 10
            };

            // 3. Ghép lại đúng chuẩn
            return $"{rankName}_of_{suitName}.png";
        }
    }
}