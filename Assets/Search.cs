using EngineCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;
public class Search
{
    private MoveGenerator moveGenerator;
    private Board board;
    private Evaluation evaluation;
    private uint bestMove;
    private int bestEvaluation;
    private int searchDepth;
    private int positionsSearched;
    private int maxEval = 9999999;
    private int checkmateEval = 99999;
    private int probeFailed = 67696769;
    private enum TTFlag
    {
        EXACT,
        ALPHA,
        BETA
    }
    private class TTEntry
    {
        public uint bestMove;
        public ulong key;
        public int evaluation;
        public int depth;
        public TTFlag flag;
        public TTEntry(uint bestMove, ulong key, int evaluation, int depth,TTFlag flag)
        {
            this.bestMove = bestMove;
            this.key = key;
            this.evaluation = evaluation;
            this.depth = depth;
            this.flag = flag;
        }

    }
    const int tableSize = 2097152;
    TTEntry[] transpositionTable = new TTEntry[tableSize];


    public Search(Board board, MoveGenerator moveGenerator, Evaluation evaluation)
    {
        this.moveGenerator = moveGenerator;
        this.board = board;
        this.evaluation = evaluation;
        
    }

    private void orderMoves(uint[] moves,int moveIndex,uint ttMove)
    {
        Dictionary<uint, int> scoreCache = new Dictionary<uint, int>();
        for(int i = 0; i < moveIndex; i++)
        {
            scoreCache.Add(moves[i], getMoveOrderScoreGuess(moves[i]));
        }
        Array.Sort(moves, 0, moveIndex, Comparer<uint>.Create((x, y) => {
            if (y == ttMove) return 1;
            return scoreCache[y].CompareTo(scoreCache[x]);

        }));
    }
    private int getMoveOrderScoreGuess(uint move)
    {
        int moveScoreGuess = 0;
        int movePiece = Move.GetPiece(move);
        int moveCapturePiece = Move.GetCapturedPiece(move);
        int promotionPiece = Move.GetPromotionPiece(move);
        if (moveCapturePiece != Piece.none)
        {
            moveScoreGuess += 10*Utils.GetPieceValue(moveCapturePiece) - Utils.GetPieceValue(movePiece);
        }
        if (promotionPiece != Piece.none)
        {
            moveScoreGuess += Utils.GetPieceValue(promotionPiece);
        }
        return moveScoreGuess;
    }
    public uint GetBestMove(int depth, bool isWhite)
    {
        bestMove = 0;
        bestEvaluation = -maxEval;
        searchDepth = depth;
        positionsSearched = 0;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        MinMaxSearch(depth, -maxEval, maxEval, isWhite);
        stopwatch.Stop();
        Debug.Log($"Searched to a depth of {depth}, positions searched: {positionsSearched}, time taken: {stopwatch.ElapsedMilliseconds}ms");
        return bestMove;
    }
    private int probeHash(int depth, int alpha, int beta,ulong zobristHash,ulong zobristIndex, out uint ttMove)
    {
        ttMove = 0;
        TTEntry entry = transpositionTable[zobristIndex];
        if (entry != null &&entry.key == zobristHash)
        {
            ttMove = entry.bestMove;
            if (entry.depth >= depth)
            {
                switch (entry.flag)
                {
                    case TTFlag.EXACT:
                        return entry.evaluation;
                    case TTFlag.ALPHA:
                        if (entry.evaluation <= alpha)
                        {
                            return alpha;
                        }
                        break;
                    case TTFlag.BETA:
                        if (entry.evaluation >= beta)
                        {
                            return beta;
                        }
                        break;
                }
            }
            // Remember the best move here for iterative deepening in the future. Currently that doesn't exist yet so we'll ignore it for now
        }
        return probeFailed;
    }
    public int MinMaxSearch(int depth, int alpha, int beta, bool isWhite)
    {
        positionsSearched++;
        uint[] currentLegalMoves = moveGenerator.generateMoves(isWhite);
        int currentMoveIndex = moveGenerator.getMoveIndex();
        ulong zobristHash = board.getZobristHash();
        ulong zobristIndex = (zobristHash & (tableSize - 1));
        int hashResult = probeHash(depth, alpha, beta, zobristHash, zobristIndex, out uint ttMove);
        if (hashResult != probeFailed && depth != searchDepth)
            return hashResult;
        TTFlag hashFlag = TTFlag.ALPHA;
        if (currentMoveIndex == 0)
        {
            if (moveGenerator.isInCheck(isWhite))
            {
                return -checkmateEval * (depth);
            }
            return 0; // Stalemate
        }
        if (depth == 0) {
            int val = evaluation.EvaluatePosition(board, isWhite);
            transpositionTable[zobristIndex] = new TTEntry(bestMove, zobristHash, val, depth, TTFlag.EXACT);
            return val;
        }
        orderMoves(currentLegalMoves, currentMoveIndex,ttMove);
        for (int i = 0; i < currentMoveIndex; i++)
        {
            board.makeMove(currentLegalMoves[i]); 
            int eval = -MinMaxSearch(depth - 1, -beta, -alpha, !isWhite);
            board.unMakeMove(currentLegalMoves[i]);
    
            if (eval > alpha)
            {
                alpha = eval;
                if (depth == searchDepth)
                {
                    hashFlag = TTFlag.EXACT;
                    bestEvaluation = eval;
                    bestMove = currentLegalMoves[i];
                }
            }
            if (eval >= beta)
            {
                transpositionTable[zobristIndex] = new TTEntry(bestMove, zobristHash, beta, depth, TTFlag.BETA);
                return beta;
            }
        }
        transpositionTable[zobristIndex] = new TTEntry(bestMove, zobristHash, alpha, depth, hashFlag);

        return alpha;
    }

}
