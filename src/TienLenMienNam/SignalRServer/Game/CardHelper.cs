using System;
using System.Collections.Generic;
using System.Linq;
// QUAN TRỌNG: Trỏ về Models của Server để tìm thấy class Card
using CardGameServer.Models; 

namespace CardGameServer.Game // QUAN TRỌNG: Namespace của Server
{
    // Định nghĩa các kiểu bộ bài hợp lệ
    public enum HandType
    {
        None,           // Không hợp lệ
        Single,         // Rác (Lẻ)
        Pair,           // Đôi
        ThreeOfAKind,   // Sám cô (Ba lá)
        FourOfAKind,    // Tứ quý
        Straight,       // Sảnh (Dây)
        ThreePine,      // 3 Đôi thông
        FourPine,       // 4 Đôi thông
        FivePine        // 5 Đôi thông (Dùng cho tới trắng)
    }

    // Kiểu tới trắng
    public enum InstantWinType
    {
        None,
        FourTwos,       // Tứ quý heo
        DragonHall,     // Sảnh rồng (3-A khác chất)
        DragonRoller,   // Rồng cuốn (3-A đồng chất)
        FivePine,       // 5 đôi thông
        SixPairs,       // 6 đôi bất kỳ
        FourThree,      // Tứ quý 3 (Ván đầu)
        ThreePineThree  // 3 đôi thông chứa 3 bích (Ván đầu)
    }

    public static class CardHelper
    {
        // ==========================================
        // 1. CÁC HÀM CƠ BẢN & SẮP XẾP
        // ==========================================

        // Sắp xếp bài: Tăng dần theo Sức mạnh (3 -> 2), nếu cùng số thì so Chất
        public static List<Card> SortCards(IEnumerable<Card> cards)
        {
            return cards.OrderBy(c => c.Power).ThenBy(c => c.Suit).ToList();
        }

        // Kiểm tra xem bài có chứa con Heo (Rank 15) không
        private static bool HasTwo(List<Card> cards)
        {
            // Giả sử trong Model: 3=Rank 3 ... K=13, A=14, 2=Rank 15
            return cards.Any(c => c.Rank == 15);
        }

        // ==========================================
        // 2. NHẬN DIỆN BỘ BÀI (LOGIC QUAN TRỌNG)
        // ==========================================
        public static HandType GetHandType(List<Card> cards)
        {
            if (cards == null || cards.Count == 0) return HandType.None;

            var sorted = SortCards(cards);
            int n = sorted.Count;

            // --- 1. Lẻ (Rác) ---
            if (n == 1) return HandType.Single;

            // --- 2. Đôi ---
            if (n == 2 && sorted[0].Rank == sorted[1].Rank) return HandType.Pair;

            // --- 3. Sám cô (Ba lá) ---
            if (n == 3 && sorted[0].Rank == sorted[1].Rank && sorted[1].Rank == sorted[2].Rank)
                return HandType.ThreeOfAKind;

            // --- 4. Tứ quý ---
            if (n == 4 && sorted.All(c => c.Rank == sorted[0].Rank))
                return HandType.FourOfAKind;

            // --- 5. Sảnh (Dây) ---
            // Luật: 3 lá trở lên, liên tiếp, KHÔNG chứa 2
            if (n >= 3 && !HasTwo(sorted) && IsConsecutive(sorted))
                return HandType.Straight;

            // --- 6. Các loại Đôi Thông (3, 4) ---
            // Luật: Số lượng chẵn (6, 8), gồm các đôi, các đôi phải liên tiếp, KHÔNG chứa 2
            if (n >= 6 && n % 2 == 0 && !HasTwo(sorted))
            {
                if (IsPinePairs(sorted))
                {
                    if (n == 6) return HandType.ThreePine;
                    if (n == 8) return HandType.FourPine;
                    if (n == 10) return HandType.FivePine; 
                }
            }

            return HandType.None;
        }

        // Hàm phụ: Kiểm tra liên tiếp (Dùng cho Sảnh)
        private static bool IsConsecutive(List<Card> sortedCards)
        {
            for (int i = 0; i < sortedCards.Count - 1; i++)
            {
                if (sortedCards[i + 1].Rank != sortedCards[i].Rank + 1) return false;
            }
            return true;
        }

        // Hàm phụ: Kiểm tra Đôi Thông
        private static bool IsPinePairs(List<Card> sortedCards)
        {
            // Bước 1: Kiểm tra từng cặp có phải là Đôi không
            for (int i = 0; i < sortedCards.Count; i += 2)
            {
                if (sortedCards[i].Rank != sortedCards[i + 1].Rank) return false;
            }

            // Bước 2: Kiểm tra các đôi có liên tiếp thứ tự không (Ví dụ: Đôi 3, Đôi 4, Đôi 5)
            // Lấy đại diện mỗi đôi (là lá ở vị trí chẵn: 0, 2, 4...)
            for (int i = 0; i < sortedCards.Count - 2; i += 2)
            {
                // Rank của đôi sau phải lớn hơn đôi trước 1 đơn vị
                if (sortedCards[i + 2].Rank != sortedCards[i].Rank + 1) return false;
            }

            return true;
        }

        // ==========================================
        // 3. LOGIC CHẶT/ĐÈ (RULE ENGINE)
        // ==========================================
        
        /// <summary>
        /// Kiểm tra bài mới (newCards) có chặt được bài cũ (prevCards) không.
        /// </summary>
        public static bool CanBeat(List<Card> prevCards, List<Card> newCards)
        {
            if (prevCards == null || prevCards.Count == 0) return true; // Đánh đầu tiên
            if (newCards == null || newCards.Count == 0) return false;

            var prevType = GetHandType(prevCards);
            var newType = GetHandType(newCards);

            // Sắp xếp để so sánh
            var prevSorted = SortCards(prevCards);
            var newSorted = SortCards(newCards);
            
            // Lấy lá bài to nhất của mỗi bên để so sánh
            var prevHighest = prevSorted.Last();
            var newHighest = newSorted.Last();

            // --- TRƯỜNG HỢP 1: CÙNG LOẠI (ĐÈ BÀI BÌNH THƯỜNG) ---
            if (prevType == newType && prevCards.Count == newCards.Count)
            {
                // Nguyên tắc: Cùng loại thì so lá to nhất (Xét cả Chất)
                return newHighest.Power > prevHighest.Power;
            }

            // --- TRƯỜNG HỢP 2: LUẬT CHẶT (HÀNG VÀ HEO) ---
            
            // A. CHẶT HEO (Lá 2 - Rank 15)
            if (prevType == HandType.Single && prevHighest.Rank == 15) // Heo lẻ
            {
                // Bị chặt bởi: 3 Đôi thông, Tứ quý, 4 Đôi thông
                if (newType == HandType.ThreePine) return true;
                if (newType == HandType.FourOfAKind) return true;
                if (newType == HandType.FourPine) return true;
                return false;
            }

            if (prevType == HandType.Pair && prevHighest.Rank == 15) // Đôi Heo
            {
                // Bị chặt bởi: Tứ quý, 4 Đôi thông
                // (Lưu ý: 3 Đôi thông KHÔNG chặt được đôi Heo theo luật MN phổ biến)
                if (newType == HandType.FourOfAKind) return true;
                if (newType == HandType.FourPine) return true;
                return false;
            }

            // B. CHẶT HÀNG (Hàng chặt Hàng)
            
            // 3 Đôi thông bị chặt bởi: 3 Đôi thông lớn hơn, Tứ quý, 4 Đôi thông
            if (prevType == HandType.ThreePine)
            {
                if (newType == HandType.FourOfAKind) return true;
                if (newType == HandType.FourPine) return true;
                
                // Luật miền Nam: 3 đôi thông lớn chặt 3 đôi thông nhỏ (đã xử lý ở case cùng loại trên, 
                // nhưng nếu check loại khác ở đây thì an toàn hơn)
            }

            // Tứ quý bị chặt bởi: Tứ quý lớn hơn (đã check), 4 Đôi thông
            if (prevType == HandType.FourOfAKind)
            {
                if (newType == HandType.FourPine) return true;
            }

            // 4 Đôi thông bị chặt bởi: 4 Đôi thông lớn hơn (đã check ở case cùng loại)
            
            return false;
        }

        // Hàm kiểm tra xem có được phép đánh "nhảy cóc" không (Dành cho 4 đôi thông)
        public static bool IsSpecialTurn(List<Card> cards)
        {
            // 4 Đôi thông có thể chặt bất cứ lúc nào (không cần vòng)
            return GetHandType(cards) == HandType.FourPine;
        }

        // ==========================================
        // 4. TỚI TRẮNG (INSTANT WIN)
        // ==========================================
        public static InstantWinType CheckInstantWin(List<Card> hand, bool isFirstGame = false)
        {
            if (hand.Count != 13) return InstantWinType.None;
            var sorted = SortCards(hand);

            // 1. Tứ quý Heo (4 con 2)
            if (hand.Count(c => c.Rank == 15) == 4) return InstantWinType.FourTwos;

            // 2. Sảnh rồng (3 -> A)
            var distinctRanks = hand.Select(c => c.Rank).Distinct().OrderBy(r => r).ToList();
            bool has3toA = distinctRanks.Count >= 12 && distinctRanks[0] == 3 && distinctRanks[11] == 14;

            if (has3toA)
            {
                // Rồng cuốn (Đồng chất)
                if (hand.All(c => c.Suit == hand[0].Suit)) return InstantWinType.DragonRoller;
                return InstantWinType.DragonHall;
            }

            // 3. 6 Đôi (Bất kỳ)
            int pairCount = 0;
            var groups = hand.GroupBy(c => c.Rank);
            foreach (var g in groups) pairCount += g.Count() / 2;
            if (pairCount >= 6) return InstantWinType.SixPairs;

            // 4. Ván đầu tiên
            if (isFirstGame)
            {
                // Tứ quý 3
                if (hand.Count(c => c.Rank == 3) == 4) return InstantWinType.FourThree;
                // 3 đôi thông chứa 3 bích (check đơn giản)
                // ...
            }

            return InstantWinType.None;
        }
    }
}