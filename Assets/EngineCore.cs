using System.Runtime.CompilerServices;
using UnityEngine;

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
    public static class BitScanner
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
    }
    public static class Move
    {
        public static uint CreateMove(
        int from,
        int to,
        int movingPiece,
        int capturedPiece,
        uint CastlePiecesMoved,
        int promotionPiece = Piece.none,
        bool castle = false,
        bool enPassant = false,
        bool doublePush = false)
        {
            uint data =
                (uint)from |
                ((uint)to << 6) |
                ((uint)movingPiece << 12) |
                ((uint)capturedPiece << 16) |
                ((uint)promotionPiece << 20);

            if (castle) data |= 1u << 24;
            if (enPassant) data |= 1u << 25;
            if (doublePush) data |= 1u << 26;
            if (Utils.isRemovingCastlingPrivleges(from,to,movingPiece, capturedPiece, CastlePiecesMoved)) data |= 1u << 27;
            return data;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSourceSquare(uint move) { return (int)(move & 0x3F); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTargetSquare(uint move) { return (int)((move >> 6) & 0x3F); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPiece(uint move) { return (int)((move >> 12) & 0xF); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCapturedPiece(uint move) { return (int)((move >> 16) & 0xF); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPromotionPiece(uint move) { return (int)((move >> 20) & 0xF); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCastling(uint move) { return (move & (1u << 24)) != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnPassent(uint move) { return (move & (1u << 25)) != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDoublePush(uint move) { return (move & (1u << 26)) != 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhite(uint move) { return GetPiece(move) < 6; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRemovingCastlePrivileges(uint move) { return (move & (1u << 27)) != 0; }
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
            return letters[index % 8] + (8 - Mathf.Floor(index / 8)).ToString();
        }
        public static bool isRemovingCastlingPrivleges(int sourceSquare, int targetSquare, int piece, int capturePiece, uint CastlePiecesMoved)
        {
            switch (piece)
            {
                case Piece.whiteKing:
                    if ((CastlePiecesMoved & (CastlePiece.whiteKingside | CastlePiece.whiteQueenside)) != (CastlePiece.whiteKingside | CastlePiece.whiteQueenside))
                    {
                        return true;
                    }
                    break;
                case Piece.blackKing:
                    if ((CastlePiecesMoved & (CastlePiece.blackKingside | CastlePiece.blackQueenside)) != (CastlePiece.blackKingside | CastlePiece.blackQueenside))
                    {
                        return true;
                    }
                    break;
                case Piece.whiteRook:
                    if (sourceSquare == 56)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteQueenside) == 0) return true;
                        break;
                    }
                    else if (sourceSquare == 63)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteKingside) == 0) return true;
                        break;
                    }
                    break;
                case Piece.blackRook:
                    if (sourceSquare == 0)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackQueenside) == 0) return true;
                        break;
                    }
                    else if (sourceSquare == 7)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackKingside) == 0) return true;
                        break;
                    }
                    break;
            }
            switch (capturePiece)
            {
                case Piece.whiteRook:
                    if (targetSquare == 56)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteQueenside) == 0) return true;
                        break;
                    }
                    else if (targetSquare == 63)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteKingside) == 0) return true;
                        break;
                    }
                    break;
                case Piece.blackRook:
                    if (targetSquare == 0)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackQueenside) == 0) return true;
                        break;
                    }
                    else if (targetSquare == 7)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackKingside) == 0) return true;
                        break;
                    }
                    break;
            }
            return false;
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
