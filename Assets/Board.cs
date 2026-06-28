using EngineCore;
using System.ComponentModel;
using UnityEngine;

public class Board
{
    private static ulong[] bitBoards = { 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul };
    private ulong whitePiecesBitboard;
    private ulong blackPiecesBitboard;
    private uint CastlePiecesMoved = 0;
    private int enPassentTargetSquare = 64;
    private int prevEnPassentTargetSquare = 64;
    public Board() { }
    public bool hasCastlePieceNotMoved(uint piece)
    {
        return (CastlePiecesMoved & piece) == 0;
    }
    public uint GetCastlePiecesMovedMask()
    {
        return CastlePiecesMoved;
    }
    public ulong GetBitboard(int piece, bool isWhite)
    {
        return bitBoards[isWhite ? (int)piece : (int)piece + 6];
    }
    public ulong GetBitboard(int piece)
    {
        return bitBoards[piece];
    }
    public ulong GetCombinedBitboard(bool isWhite)
    {
        return isWhite ? whitePiecesBitboard : blackPiecesBitboard;
    }
    public ulong GetAllPiecesBitboard()
    {
        return whitePiecesBitboard | blackPiecesBitboard;
    }
    public ulong GetAllBlockersBitboard(bool isWhite)
    {
        return (whitePiecesBitboard | blackPiecesBitboard) & ~(isWhite ? bitBoards[Piece.blackKing] : bitBoards[Piece.whiteKing]);
    }
    public int GetEnpassentTargetSquare()
    {
        return enPassentTargetSquare;
    }
    private void generateCombinedBitboards()
    {
        whitePiecesBitboard = 0ul;
        whitePiecesBitboard |= bitBoards[Piece.whiteKing];
        whitePiecesBitboard |= bitBoards[Piece.whiteKnight];
        whitePiecesBitboard |= bitBoards[Piece.whiteBishop];
        whitePiecesBitboard |= bitBoards[Piece.whiteRook];
        whitePiecesBitboard |= bitBoards[Piece.whiteQueen];
        whitePiecesBitboard |= bitBoards[Piece.whitePawn];
        blackPiecesBitboard = 0ul;
        blackPiecesBitboard |= bitBoards[Piece.blackKing];
        blackPiecesBitboard |= bitBoards[Piece.blackKnight];
        blackPiecesBitboard |= bitBoards[Piece.blackBishop];
        blackPiecesBitboard |= bitBoards[Piece.blackRook];
        blackPiecesBitboard |= bitBoards[Piece.blackQueen];
        blackPiecesBitboard |= bitBoards[Piece.blackPawn];
    }
    private int addFenCharToBitBoard(int index, char fenChar)
    {
        switch (fenChar)
        {
            case 'r':
                bitBoards[Piece.blackRook] |= (1ul << index);
                break;
            case 'n':
                bitBoards[Piece.blackKnight] |= (1ul << index);

                break;

            case 'b':
                bitBoards[Piece.blackBishop] |= (1ul << index);

                break;

            case 'q':
                bitBoards[Piece.blackQueen] |= (1ul << index);

                break;

            case 'p':
                bitBoards[Piece.blackPawn] |= (1ul << index);

                break;
            case 'k':
                bitBoards[Piece.blackKing] |= (1ul << index);

                break;
            case 'R':
                bitBoards[Piece.whiteRook] |= (1ul << index);

                break;

            case 'N':
                bitBoards[Piece.whiteKnight] |= (1ul << index);

                break;

            case 'B':
                bitBoards[Piece.whiteBishop] |= (1ul << index);

                break;

            case 'Q':
                bitBoards[Piece.whiteQueen] |= (1ul << index);

                break;

            case 'P':
                bitBoards[Piece.whitePawn] |= (1ul << index);

                break;
            case 'K':
                bitBoards[Piece.whiteKing] |= (1ul << index);

                break;
            default:
                if (int.TryParse(fenChar.ToString(), out _))
                {

                    return index + int.Parse(fenChar.ToString());
                }
                return index;
        }
        return index + 1;
    }
    private void addFenCharCastleRights(char fenChar)
    {

        switch (fenChar)
        {
            case 'K':
                CastlePiecesMoved &= ~CastlePiece.whiteRightRook;
                break;
            case 'Q':
                CastlePiecesMoved &= ~CastlePiece.whiteLeftRook;
                break;
            case 'k':
                CastlePiecesMoved &= ~CastlePiece.blackRightRook;
                break;
            case 'q':
                CastlePiecesMoved &= ~CastlePiece.blackLeftRook;
                break;
        }
    }
    public bool convertFenStringToBitBoard(string fenString)
    {
        int index = 0;
        int informationTypeIndex = 0;
        bool isWhiteMove = true;
        for (int i = 0; i < fenString.Length; i++)
        {
            if (fenString[i] == ' ')
            {
                informationTypeIndex++;
                if (informationTypeIndex == 2)
                {
                    CastlePiecesMoved = 0; //Both rooks have moved (they will be set back based on infromation given in the fen string)
                    CastlePiecesMoved |= CastlePiece.whiteRightRook | CastlePiece.whiteLeftRook | CastlePiece.blackRightRook | CastlePiece.blackLeftRook;
                }

            }
            switch (informationTypeIndex)
            {
                case 0:
                    index = addFenCharToBitBoard(index, fenString[i]);
                    break;
                case 1:
                    if (fenString[i] == 'w')
                    {
                        isWhiteMove = true;
                    }
                    else
                    {
                        isWhiteMove = false;
                    }
                    break;
                case 2:
                    addFenCharCastleRights(fenString[i]);
                    break;

            }

        }
        generateCombinedBitboards();
        return isWhiteMove;
    }
    public int getCapturePiece(bool isWhite, int s2)
    {
        int capturePiece = Piece.none;
        if (isWhite)
        {
            if ((bitBoards[Piece.blackKnight] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.blackKnight;
            }
            else if ((bitBoards[Piece.blackBishop] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.blackBishop;
            }
            else if ((bitBoards[Piece.blackPawn] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.blackPawn;
            }
            else if ((bitBoards[Piece.blackQueen] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.blackQueen;
            }
            else if ((bitBoards[Piece.blackRook] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.blackRook;
            }
        }
        else
        {
            if ((bitBoards[Piece.whiteKnight] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.whiteKnight;
            }
            else if ((bitBoards[Piece.whiteBishop] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.whiteBishop;
            }
            else if ((bitBoards[Piece.whiteRook] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.whiteRook;
            }
            else if ((bitBoards[Piece.whiteQueen] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.whiteQueen;
            }
            else if ((bitBoards[Piece.whitePawn] & (1ul << s2)) != 0)
            {
                capturePiece = Piece.whitePawn;
            }
        }
        return capturePiece;
    }

    public void makeMove(Move move)
    {
        if (move == null) return;
        switch (move.movePiece)
        {
            case Piece.whiteKing:
                if ((CastlePiecesMoved & CastlePiece.whiteKing) == 0)
                {
                    CastlePiecesMoved |= CastlePiece.whiteKing;

                }
                break;
            case Piece.blackKing:
                if ((CastlePiecesMoved & CastlePiece.blackKing) == 0)
                {
                    CastlePiecesMoved |= CastlePiece.blackKing;

                }
                break;
            case Piece.whiteRook:
                if (move.s1 == 56)
                {
                    if ((CastlePiecesMoved & CastlePiece.whiteLeftRook) == 0)
                    {
                        CastlePiecesMoved |= CastlePiece.whiteLeftRook;
    
                    }
                    break;
                }
                else if (move.s1 == 63)
                {
                    if ((CastlePiecesMoved & CastlePiece.whiteRightRook) == 0)
                    {
                        CastlePiecesMoved |= CastlePiece.whiteRightRook;
    
                    }
                    break;
                }
                break;
            case Piece.blackRook:
                if (move.s1 == 0)
                {
                    if ((CastlePiecesMoved & CastlePiece.blackLeftRook) == 0)
                    {
                        CastlePiecesMoved |= CastlePiece.blackLeftRook;
    
                    }
                    break;
                }
                else if (move.s1 == 7)
                {
                    if ((CastlePiecesMoved & CastlePiece.blackRightRook) == 0)
                    {
                        CastlePiecesMoved |= CastlePiece.blackRightRook;
    
                    }
                    break;
                }
                break;

        }

        if (move.isCastling)
        {
            move.isRemovingCastlingPrivilege = true;
            if (move.isWhite)
            {
                bitBoards[Piece.whiteKing] = 1ul << move.s2;
                if (move.s2 == 58)
                {
                    bitBoards[Piece.whiteRook] &= ~0x0100000000000000ul;
                    bitBoards[Piece.whiteRook] |= 0x0800000000000000ul;
                }
                else
                {
                    bitBoards[Piece.whiteRook] |= 0x2000000000000000ul;
                    bitBoards[Piece.whiteRook] &= ~0x8000000000000000ul;
                }
            }
            else
            {
                bitBoards[Piece.blackKing] = 1ul << move.s2;
                if (move.s2 == 2)
                {
                    bitBoards[Piece.blackRook] &= ~1ul;
                    bitBoards[Piece.blackRook] |= 8ul;
                }
                else
                {
                    bitBoards[Piece.blackRook] |= 32ul;
                    bitBoards[Piece.blackRook] &= ~128ul;
                }
            }
        }
        else
        {

            bitBoards[move.movePiece] &= ~(1ul << move.s1);
            bitBoards[move.movePiece] |= (1ul << move.s2);
            if (move.capturePiece != Piece.none)
            {
                int enPassentOffset = 0;
                if (move.isEnPassent) enPassentOffset += (move.isWhite ? 8 : -8);
                bitBoards[move.capturePiece] &= ~(1ul << (move.s2 + enPassentOffset));

                switch (move.capturePiece)
                {
                    case Piece.whiteRook:
                        if (move.s2 == 56)
                        {
                            if ((CastlePiecesMoved & CastlePiece.whiteLeftRook) == 0)
                            {
                                CastlePiecesMoved |= CastlePiece.whiteLeftRook;
            
                            }
                            break;
                        }
                        else if (move.s2 == 63)
                        {
                            if ((CastlePiecesMoved & CastlePiece.whiteRightRook) == 0)
                            {
                                CastlePiecesMoved |= CastlePiece.whiteRightRook;
            
                            }
                            break;
                        }
                        break;
                    case Piece.blackRook:
                        if (move.s2 == 0)
                        {
                            if ((CastlePiecesMoved & CastlePiece.blackLeftRook) == 0)
                            {
                                CastlePiecesMoved |= CastlePiece.blackLeftRook;
            
                            }
                            break;
                        }
                        else if (move.s2 == 7)
                        {
                            if ((CastlePiecesMoved & CastlePiece.blackRightRook) == 0)
                            {
                                CastlePiecesMoved |= CastlePiece.blackRightRook;
            
                            }
                            break;
                        }
                        break;
                }
            }
            if (move.promotionPiece != Piece.none)
            {
                bitBoards[move.movePiece] &= ~(1ul << move.s2);
                bitBoards[move.promotionPiece] |= (1ul << move.s2);
            }
            prevEnPassentTargetSquare = enPassentTargetSquare;
            enPassentTargetSquare = 64;
            if (move.isDoublePawnPush)
            {
                enPassentTargetSquare = move.s2 + (move.isWhite ? 8 : -8);
            }
        }

        generateCombinedBitboards();
    }
    public void unMakeMove(Move move)
    {
        if (move == null) return;
        if (move.isRemovingCastlingPrivilege)
        {
            switch (move.movePiece)
            {
                case Piece.whiteKing:
                    CastlePiecesMoved &= ~CastlePiece.whiteKing;
                    break;
                case Piece.blackKing:
                    CastlePiecesMoved &= ~CastlePiece.blackKing;
                    break;
                case Piece.whiteRook:
                    if (move.s1 == 56)
                    {
                        CastlePiecesMoved &= ~CastlePiece.whiteLeftRook;

                    }
                    else if (move.s1 == 63)
                    {
                        CastlePiecesMoved &= ~CastlePiece.whiteRightRook;

                    }
                    break;
                case Piece.blackRook:
                    if (move.s1 == 0)
                    {
                        CastlePiecesMoved &= ~CastlePiece.blackLeftRook;

                    }
                    else if (move.s1 == 7)
                    {
                        CastlePiecesMoved &= ~CastlePiece.blackRightRook;

                    }
                    break;

            }
        }

        if (move.isCastling)
        {
            if (move.isWhite)
            {
                bitBoards[Piece.whiteKing] = 1ul << move.s1;
                if (move.s2 == 58)
                {
                    bitBoards[Piece.whiteRook] |= 0x0100000000000000ul;
                    bitBoards[Piece.whiteRook] &= ~0x0800000000000000ul;
                }
                else
                {
                    bitBoards[Piece.whiteRook] &= ~0x2000000000000000ul;
                    bitBoards[Piece.whiteRook] |= 0x8000000000000000ul;
                }
            }
            else
            {
                bitBoards[Piece.blackKing] = 1ul << move.s1;
                if (move.s2 == 2)
                {
                    bitBoards[Piece.blackRook] |= 1ul;
                    bitBoards[Piece.blackRook] &= ~8ul;
                }
                else
                {
                    bitBoards[Piece.blackRook] &= ~32ul;
                    bitBoards[Piece.blackRook] |= 128ul;
                }
            }
        }
        else
        {

            bitBoards[move.movePiece] |= (1ul << move.s1);
            bitBoards[move.movePiece] &= ~(1ul << move.s2);
            if (move.capturePiece != Piece.none)
            {
                int enPassentOffset = 0;
                if (move.isEnPassent) enPassentOffset += (move.isWhite ? 8 : -8);
                bitBoards[move.capturePiece] |= (1ul << (move.s2 + enPassentOffset));
                if (move.isRemovingCastlingPrivilege)
                {
                    switch (move.capturePiece)
                    {
                        case Piece.whiteRook:
                            if (move.s2 == 56)
                            {
                                CastlePiecesMoved &= ~CastlePiece.whiteLeftRook;

                            }
                            else if (move.s2 == 63)
                            {
                                CastlePiecesMoved &= ~CastlePiece.whiteRightRook;

                            }
                            break;
                        case Piece.blackRook:
                            if (move.s2 == 0)
                            {
                                CastlePiecesMoved &= ~CastlePiece.blackLeftRook;

                            }
                            else if (move.s2 == 7)
                            {
                                CastlePiecesMoved &= ~CastlePiece.blackRightRook;

                            }
                            break;

                    }
                }
            }
            if (move.promotionPiece != Piece.none)
            {
                bitBoards[move.promotionPiece] &= ~(1ul << move.s2);
            }
            enPassentTargetSquare = 64;
            if (prevEnPassentTargetSquare != 64)
            {
                enPassentTargetSquare = prevEnPassentTargetSquare;
                prevEnPassentTargetSquare = 64;
            }
        }
        generateCombinedBitboards();
    }
}
