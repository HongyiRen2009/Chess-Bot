using System;
using System.Runtime.CompilerServices;

namespace EngineCore
{
    public enum GameState
    {
        Ongoing,
        WhiteWins,
        BlackWins,
        Stalemate
    }
    public class RayDirections
    {
        public const int left = 0;
        public const int right = 1;
        public const int up = 2;
        public const int down = 3;
        public const int leftUp = 4;
        public const int rightUp = 5;
        public const int leftDown = 6;
        public const int rightDown = 7;
    }
    public class Piece
    {
        public const int whiteKing = 0;
        public const int whiteQueen = 1;
        public const int whiteBishop = 2;
        public const int whiteKnight = 3;
        public const int whiteRook = 4;
        public const int whitePawn = 5;
        public const int blackKing = 6;
        public const int blackQueen = 7;
        public const int blackBishop = 8;
        public const int blackKnight = 9;
        public const int blackRook = 10;
        public const int blackPawn = 11;
        public const int none = 12;
        public const int uncoloredKing = 0;
        public const int uncoloredQueen = 1;
        public const int uncoloredBishop = 2;
        public const int uncoloredKnight = 3;
        public const int uncoloredRook = 4;
        public const int uncoloredPawn = 5;

    };
    public class DirectionOffsets
    {
        public const int left = -1;
        public const int right = 1;
        public const int up = -8;
        public const int down = 8;
        public const int leftUp = -9;
        public const int rightUp = -7;
        public const int leftDown = 7;
        public const int rightDown = 9;
    }
    public static class CastlePiece
    {
        public const uint whiteKingside = 1;
        public const uint whiteQueenside = 2;
        public const uint blackKingside = 4;
        public const uint blackQueenside = 8;

        public const uint all = whiteKingside | whiteQueenside | blackKingside | blackQueenside;
    }
    public static class BitOperations
    {
        private const ulong Magic = 0x37E84A99DAE458F;

        private static readonly int[] MagicTable =
        {
        0, 1, 17, 2, 18, 50, 3, 57,
        47, 19, 22, 51, 29, 4, 33, 58,
        15, 48, 20, 27, 25, 23, 52, 41,
        54, 30, 38, 5, 43, 34, 59, 8,
        63, 16, 49, 56, 46, 21, 28, 32,
        14, 26, 24, 40, 53, 37, 42, 7,
        62, 55, 45, 31, 13, 39, 36, 6,
        61, 44, 12, 35, 60, 11, 10, 9,
    };

        public static int BitScanForward(ulong b)
        {
            return MagicTable[((ulong)((long)b & -(long)b) * Magic) >> 58];
        }

        public static int BitScanReverse(ulong b)
        {
            b |= b >> 1;
            b |= b >> 2;
            b |= b >> 4;
            b |= b >> 8;
            b |= b >> 16;
            b |= b >> 32;
            b = b & ~(b >> 1);
            return MagicTable[b * Magic >> 58];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PopCount(ulong x)
        {
            const ulong m1 = 0x5555555555555555UL;
            const ulong m2 = 0x3333333333333333UL;
            const ulong m4 = 0x0F0F0F0F0F0F0F0FUL; 
            const ulong h01 = 0x0101010101010101UL;

            x -= (x >> 1) & m1;
            x = (x & m2) + ((x >> 2) & m2);
            x = (x + (x >> 4)) & m4;
            return (int)((x * h01) >> 56);
        }
    }
    public static class PromotionFlags
    {
        public const int PromoteToQueen = 0;
        public const int PromoteToKnight = 1;
        public const int PromoteToBishop = 2;
        public const int PromoteToRook = 3;
    }

    public static class Move
    {
        // bits 0-5: source, bits 6-11: target, bits 12-13: promotion flag, bit 14: "has promotion"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort CreateMove(int from, int to, int promotionFlag = -1)
        {
            ushort data = (ushort)((uint)from | ((uint)to << 6));
            if (promotionFlag >= 0)
            {
                data |= (ushort)(((uint)promotionFlag << 12) | (1u << 14));
            }
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSourceSquare(ushort move) => move & 0x3F;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTargetSquare(ushort move) => (move >> 6) & 0x3F;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasPromotion(ushort move) => (move & (1 << 14)) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPromotionFlag(ushort move) => (move >> 12) & 0x3;

        // Maps the 2-bit flag to an actual piece for the side to move.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPromotionPiece(ushort move, bool isWhite)
        {
            if (!HasPromotion(move)) return Piece.none;
            int flag = GetPromotionFlag(move);
            return flag switch
            {
                PromotionFlags.PromoteToKnight => isWhite ? Piece.whiteKnight : Piece.blackKnight,
                PromotionFlags.PromoteToBishop => isWhite ? Piece.whiteBishop : Piece.blackBishop,
                PromotionFlags.PromoteToRook => isWhite ? Piece.whiteRook : Piece.blackRook,
                _ => isWhite ? Piece.whiteQueen : Piece.blackQueen,
            };
        }
    }

    public static class Utils
    {
        public static string[] pieceToString = { "K", "Q", "B", "N", "R", "P", "k", "q", "b", "n", "r", "p", "" };
        public static char[] letters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
        private static int[] pieceValues = new int[12] { 0, 900, 300, 300, 500, 100, 0, 900, 300, 300, 500, 100, };
        public static int GetPieceValue(int piece)
        {
            return pieceValues[piece];
        }
        public static string convertBoardIndexToChessNotation(int index)
        {
            return letters[index % 8] + (8 - Math.Floor(index / 8.0)).ToString();
        }
     public static int GetRank(int square)
        {
            return square / 8;
        }
        public static int GetFile(int square) {
            return square % 8;
        }
    }
}
