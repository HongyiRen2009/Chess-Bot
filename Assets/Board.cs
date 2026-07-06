using EngineCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Board
{
    private ulong[] bitBoards = { 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul, 0ul };
    private int[] piecesBoard = new int[64];
    private ulong whitePiecesBitboard;
    private ulong blackPiecesBitboard;
    private uint CastlePiecesMoved = 0;
    private int enPassentTargetSquare = 64;
    private ulong[,] zobristTable = new ulong[64, 12];
    private ulong zobristSideToMove;
    private ulong[] zobristCastling = new ulong[16];
    private ulong[] zobristEnPassantFile = new ulong[8];
    private bool whiteToMove = true;
    private Stack<int> enPassantHistory = new Stack<int>();
    private Stack<uint> castleRightsHistory = new Stack<uint>();

    private ulong zobristHash;
    private Evaluation evaluation;
    public ulong GetRandomULong()
    {
        System.Random random = new System.Random();
        byte[] buffer = new byte[8];

        random.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }
    public Board(Evaluation evaluation)
    {
        this.evaluation = evaluation;
        for (int i = 0; i < 64; i++)
            for (int piece = 0; piece < 12; piece++)
                zobristTable[i, piece] = GetRandomULong();

        zobristSideToMove = GetRandomULong();
        for (int i = 0; i < 16; i++) zobristCastling[i] = GetRandomULong();
        for (int i = 0; i < 8; i++) zobristEnPassantFile[i] = GetRandomULong();
    }

    private void XorEnPassant(int square)
    {
        if (square != 64) zobristHash ^= zobristEnPassantFile[square % 8];
    }
    public int GetKingSquare(bool isWhite)
    {
        return BitScanner.BitScanForward(GetBitboard(Piece.uncoloredKing, isWhite));
    }
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
    public ulong[] GetBitboards()
    {
        return bitBoards;
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
    public ulong getZobristHash()
    {
        return zobristHash;
    }

    private void AddPiece(int piece, int square)
    {
        bitBoards[piece] |= 1ul << square;
        if (piece < 6) whitePiecesBitboard |= 1ul << square; else blackPiecesBitboard |= 1ul << square;
        zobristHash ^= zobristTable[square, piece];
        piecesBoard[square] = piece;
        evaluation.UpdateEvaluation(this, piece, true, square);
    }
    private void RemovePiece(int piece, int square)
    {
        bitBoards[piece] &= ~(1ul << square);
        if (piece < 6) whitePiecesBitboard &= ~(1ul << square); else blackPiecesBitboard &= ~(1ul << square);
        zobristHash ^= zobristTable[square, piece];
        if (piece != piecesBoard[square])
        {
            Debug.LogError("NOT MATCHING");
        }
        piecesBoard[square] = Piece.none;
        evaluation.UpdateEvaluation(this, piece, false, square);
    }
    private int addFenCharToBitBoard(int index, char fenChar)
    {
        switch (fenChar)
        {
            case 'r':
                AddPiece(Piece.blackRook, index);
                break;
            case 'n':
                AddPiece(Piece.blackKnight, index);

                break;

            case 'b':
                AddPiece(Piece.blackBishop, index);


                break;

            case 'q':
                AddPiece(Piece.blackQueen, index);

                break;

            case 'p':
                AddPiece(Piece.blackPawn, index);

                break;
            case 'k':
                AddPiece(Piece.blackKing, index);

                break;
            case 'R':
                AddPiece(Piece.whiteRook, index);

                break;

            case 'N':
                AddPiece(Piece.whiteKnight, index);

                break;

            case 'B':
                AddPiece(Piece.whiteBishop, index);

                break;

            case 'Q':
                AddPiece(Piece.whiteQueen, index);

                break;

            case 'P':
                AddPiece(Piece.whitePawn, index);

                break;
            case 'K':
                AddPiece(Piece.whiteKing, index);

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
                CastlePiecesMoved &= ~CastlePiece.whiteKingside;
                break;
            case 'Q':
                CastlePiecesMoved &= ~CastlePiece.whiteQueenside;
                break;
            case 'k':
                CastlePiecesMoved &= ~CastlePiece.blackKingside;
                break;
            case 'q':
                CastlePiecesMoved &= ~CastlePiece.blackQueenside;
                break;
        }
    }
    public bool convertFenStringToBitBoard(string fenString)
    {
        int index = 0;
        int informationTypeIndex = 0;
        bool isWhiteMove = true;
        whitePiecesBitboard = 0ul;
        blackPiecesBitboard = 0ul;
        Array.Fill(piecesBoard, Piece.none);
        evaluation.ResetEvaluation();
        for (int i = 0; i < fenString.Length; i++)
        {
            if (fenString[i] == ' ')
            {
                informationTypeIndex++;
                if (informationTypeIndex == 2)
                {
                    CastlePiecesMoved = CastlePiece.all;
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
                case 3:
                    if (Array.IndexOf(Utils.letters, fenString[i])!=-1)
                    {
                        enPassentTargetSquare =((int.Parse(fenString[i + 1].ToString())-1)*8+ Array.IndexOf(Utils.letters, fenString[i])) ^ 56;
                        informationTypeIndex++;
                    }
                    break;

            }

        }
        return isWhiteMove;
    }
    public int getCapturePiece(int s2)
    {
        return piecesBoard[s2];
    }

    public void makeMove(uint move)
    {
        zobristHash ^= zobristCastling[CastlePiecesMoved];
        XorEnPassant(enPassentTargetSquare);
        enPassantHistory.Push(enPassentTargetSquare);
        castleRightsHistory.Push(CastlePiecesMoved);
        int sourceSquare = Move.GetSourceSquare(move);
        int targetSquare = Move.GetTargetSquare(move);
        int movePiece = Move.GetPiece(move);
        int capturePiece = Move.GetCapturedPiece(move);
        int promotionPiece = Move.GetPromotionPiece(move);
        bool isWhite = movePiece < 6;
        bool isEnPassent = Move.IsEnPassent(move);
        bool isCastling = Move.IsCastling(move);
        bool isDoublePawnPush = Move.IsDoublePush(move);

        switch (movePiece)
        {
            case Piece.whiteKing:
                CastlePiecesMoved |= CastlePiece.whiteKingside | CastlePiece.whiteQueenside;
                break;
            case Piece.blackKing:
                CastlePiecesMoved |= CastlePiece.blackKingside | CastlePiece.blackQueenside;
                break;
            case Piece.whiteRook:
                if (sourceSquare == 56) CastlePiecesMoved |= CastlePiece.whiteQueenside; // a1 rook
                else if (sourceSquare == 63) CastlePiecesMoved |= CastlePiece.whiteKingside; // h1 rook
                break;
            case Piece.blackRook:
                if (sourceSquare == 0) CastlePiecesMoved |= CastlePiece.blackQueenside; // a8 rook
                else if (sourceSquare == 7) CastlePiecesMoved |= CastlePiece.blackKingside; // h8 rook
                break;
        }
        enPassentTargetSquare = 64;

        if (isCastling)
        {
            if (isWhite)
            {
                RemovePiece(Piece.whiteKing, sourceSquare);
                AddPiece(Piece.whiteKing, targetSquare);

                if (targetSquare == 58)
                {
                    RemovePiece(Piece.whiteRook, 56);
                    AddPiece(Piece.whiteRook, 59);
                }
                else
                {
                    RemovePiece(Piece.whiteRook, 63);
                    AddPiece(Piece.whiteRook, 61);
                }
            }
            else
            {
                RemovePiece(Piece.blackKing, sourceSquare);
                AddPiece(Piece.blackKing, targetSquare);

                if (targetSquare == 2)
                {
                    RemovePiece(Piece.blackRook, 0);
                    AddPiece(Piece.blackRook, 3);
                }
                else
                {
                    RemovePiece(Piece.blackRook, 7);
                    AddPiece(Piece.blackRook, 5);
                }
            }
        }
        else
        {
            RemovePiece(movePiece, sourceSquare);

            if (capturePiece != Piece.none)
            {
                int enPassentOffset = 0;
                if (isEnPassent) enPassentOffset += (isWhite ? 8 : -8);
                RemovePiece(capturePiece, targetSquare + enPassentOffset);

                switch (capturePiece)
                {
                    case Piece.whiteRook:
                        if (targetSquare == 56) CastlePiecesMoved |= CastlePiece.whiteQueenside;
                        else if (targetSquare == 63) CastlePiecesMoved |= CastlePiece.whiteKingside;
                        break;
                    case Piece.blackRook:
                        if (targetSquare == 0) CastlePiecesMoved |= CastlePiece.blackQueenside;
                        else if (targetSquare == 7) CastlePiecesMoved |= CastlePiece.blackKingside;
                        break;
                }
            }

            int pieceToPlace = promotionPiece != Piece.none ? promotionPiece : movePiece;
            AddPiece(pieceToPlace, targetSquare);

            if (isDoublePawnPush)
            {
                enPassentTargetSquare = targetSquare + (isWhite ? 8 : -8);
            }

        }
        zobristHash ^= zobristCastling[CastlePiecesMoved];
        XorEnPassant(enPassentTargetSquare);
        zobristHash ^= zobristSideToMove;
        whiteToMove = !whiteToMove;
    }

    public void unMakeMove(uint move)
    {
        zobristHash ^= zobristCastling[CastlePiecesMoved];
        XorEnPassant(enPassentTargetSquare);
        int sourceSquare = Move.GetSourceSquare(move);
        int targetSquare = Move.GetTargetSquare(move);
        int movePiece = Move.GetPiece(move);
        int capturePiece = Move.GetCapturedPiece(move);
        int promotionPiece = Move.GetPromotionPiece(move);
        bool isWhite = Move.IsWhite(move);
        bool isEnPassent = Move.IsEnPassent(move);
        bool isCastling = Move.IsCastling(move);
        bool isDoublePawnPush = Move.IsDoublePush(move);
        if (isCastling)
        {
            if (isWhite)
            {
                RemovePiece(Piece.whiteKing, targetSquare);
                AddPiece(Piece.whiteKing, sourceSquare);

                if (targetSquare == 58)
                {
                    RemovePiece(Piece.whiteRook, 59);
                    AddPiece(Piece.whiteRook, 56);
                }
                else
                {
                    RemovePiece(Piece.whiteRook, 61);
                    AddPiece(Piece.whiteRook, 63);
                }
            }
            else
            {
                RemovePiece(Piece.blackKing, targetSquare);
                AddPiece(Piece.blackKing, sourceSquare);

                if (targetSquare == 2)
                {
                    RemovePiece(Piece.blackRook, 3);
                    AddPiece(Piece.blackRook, 0);
                }
                else
                {
                    RemovePiece(Piece.blackRook, 5);
                    AddPiece(Piece.blackRook, 7);
                }
            }
        }
        else
        {
            int pieceToRemove = promotionPiece != Piece.none ? promotionPiece : movePiece;
            RemovePiece(pieceToRemove, targetSquare);
            AddPiece(movePiece, sourceSquare);

            if (capturePiece != Piece.none)
            {
                int enPassentOffset = 0;
                if (isEnPassent) enPassentOffset += (isWhite ? 8 : -8);
                AddPiece(capturePiece, targetSquare + enPassentOffset);
            }
            
        }
        enPassentTargetSquare = enPassantHistory.Pop();
        CastlePiecesMoved = castleRightsHistory.Pop();
        zobristHash ^= zobristCastling[CastlePiecesMoved];
        XorEnPassant(enPassentTargetSquare);
        zobristHash ^= zobristSideToMove;
        whiteToMove = !whiteToMove;
    }
}
