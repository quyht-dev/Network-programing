// CardGameServer/Models/Deck.cs
using System;
using System.Collections.Generic;

namespace CardGameServer.Models
{
    public sealed class Deck
    {
        private readonly List<Card> _cards = new List<Card>();
        private readonly Random _rng = new Random();

        public Deck()
        {
            Reset();
        }

        public void Reset()
        {
            _cards.Clear();
            for (int rank = 3; rank <= 15; rank++)
            {
                _cards.Add(new Card(rank, Suit.Spades));
                _cards.Add(new Card(rank, Suit.Clubs));
                _cards.Add(new Card(rank, Suit.Diamonds));
                _cards.Add(new Card(rank, Suit.Hearts));
            }
        }

        public void Shuffle()
        {
            // Fisher–Yates
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = tmp;
            }
        }

        public List<Card> Draw(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            if (_cards.Count < count) throw new InvalidOperationException("Not enough cards.");

            var result = _cards.GetRange(0, count);
            _cards.RemoveRange(0, count);
            return result;
        }

        public int Count { get { return _cards.Count; } }
    }
}
