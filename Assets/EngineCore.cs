using UnityEngine;

namespace EngineCore
{
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
    public class CastlePiece
    {
        public const uint whiteKing = 1;
        public const uint whiteLeftRook = 2;
        public const uint whiteRightRook = 4;
        public const uint blackKing = 8;
        public const uint blackLeftRook = 16;
        public const uint blackRightRook = 32;
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
     
    public readonly struct Move
    {
        public readonly int s1;
        public readonly int s2;
        public readonly int promotionPiece;
        public readonly int capturePiece;
        public readonly int movePiece;
        public readonly bool isCastling;
        public readonly bool isWhite;
        public readonly bool isRemovingCastlingPrivilege;
        public readonly bool isDoublePawnPush;
        public readonly bool isEnPassent;
        public Move(int currentMovePiece, int square1, int square2, bool isWhitePiece, int capturePiece,uint castlePiecesMoved, int promotionPiece = Piece.none, bool castleMove = false, bool isDoublePawnPush = false,bool isEnPassent = false)
        {
            s1 = square1;
            s2 = square2;
            this.promotionPiece = promotionPiece;
            movePiece = currentMovePiece;
            isCastling = castleMove;
            isWhite = isWhitePiece;
            this.capturePiece = capturePiece;
            this.isDoublePawnPush = isDoublePawnPush;
            this.isEnPassent = isEnPassent;
            this.isRemovingCastlingPrivilege = Utils.isRemovingCastlingPrivleges(square1,square2,currentMovePiece,capturePiece,castlePiecesMoved);

        }
    }
    public static class Utils
    {
        public static string[] pieceToString = { "K", "Q", "B", "N", "R", "P", "k", "q", "b", "n", "r", "p", "" };
        private static string[] letters = { "a", "b", "c", "d", "e", "f", "g", "h" };
        public static string convertBoardIndexToChessNotation(int index)
        {
            return letters[index % 8] + (8 - Mathf.Floor(index / 8)).ToString();
        }
        public static bool isRemovingCastlingPrivleges(int sourceSquare, int targetSquare, int piece, int capturePiece, uint CastlePiecesMoved)
        {
            switch (piece)
            {
                case Piece.whiteKing:
                    if ((CastlePiecesMoved & CastlePiece.whiteKing) == 0)
                    {
                        return true;
                    }
                    break;
                case Piece.blackKing:
                    if ((CastlePiecesMoved & CastlePiece.blackKing) == 0)
                    {
                        return true;
                    }
                    break;
                case Piece.whiteRook:
                    if (sourceSquare == 56)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteLeftRook) == 0)
                        {
                            return true;
                        }
                        break;
                    }
                    else if (sourceSquare == 63)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteRightRook) == 0)
                        {
                            return true;
                        }
                        break;
                    }
                    break;
                case Piece.blackRook:
                    if (sourceSquare == 0)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackLeftRook) == 0)
                        {
                            return true;
                        }
                        break;
                    }
                    else if (sourceSquare == 7)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackRightRook) == 0)
                        {
                            return true;
                        }
                        break;
                    }
                    break;

            }
            switch (capturePiece)
            {
                case Piece.whiteRook:
                    if (targetSquare == 56)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteLeftRook) == 0)
                        {
                            CastlePiecesMoved |= CastlePiece.whiteLeftRook;
                            return true;
                        }
                        break;
                    }
                    else if (targetSquare == 63)
                    {
                        if ((CastlePiecesMoved & CastlePiece.whiteRightRook) == 0)
                        {
                            CastlePiecesMoved |= CastlePiece.whiteRightRook;
                            return true;
                        }
                        break;
                    }
                    break;
                case Piece.blackRook:
                    if (targetSquare == 0)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackLeftRook) == 0)
                        {
                            CastlePiecesMoved |= CastlePiece.blackLeftRook;
                            return true;
                        }
                        break;
                    }
                    else if (targetSquare == 7)
                    {
                        if ((CastlePiecesMoved & CastlePiece.blackRightRook) == 0)
                        {
                            CastlePiecesMoved |= CastlePiece.blackRightRook;
                            return true;
                        }
                        break;
                    }
                    break;
            }
            return false;
        }
    }
}
