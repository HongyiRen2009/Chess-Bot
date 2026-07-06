using EngineCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
public class Search
{
    private MoveGenerator moveGenerator;
    private Board board;
    private Evaluation evaluation;
    private uint bestMove;
    private uint bestMoveThisIteration;
    private int bestEvaluation;
    private int bestEvaluationThisIteration;
    private int positionsSearched;
    private int maxEval = 9999999;
    private int checkmateEval = 99999;
    private int probeFailed = 67696769;
    private float searchTime;
    private bool searchCancelled;
    private Stopwatch stopwatch = new Stopwatch();
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


    public Search(Board board, MoveGenerator moveGenerator, Evaluation evaluation,float thinkingTime)
    {
        this.moveGenerator = moveGenerator;
        this.board = board;
        this.evaluation = evaluation;
        searchTime = thinkingTime;
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
        stopwatch.Start();
        bestMove = 0;
        bestEvaluation = 0;
        positionsSearched = 0;
        searchCancelled = false;
        stopwatch.Restart();
        int searchedDepth = 0;
        for (int currentSearchDepth = 1; currentSearchDepth <= depth; currentSearchDepth++)
        {
            bestMoveThisIteration = 0;
            bestEvaluationThisIteration = 0;
            MinMaxSearch(currentSearchDepth, currentSearchDepth, -maxEval, maxEval, isWhite);
            searchedDepth = currentSearchDepth;
            if (searchCancelled) break;
            if (bestMoveThisIteration != 0)
            {
                bestMove = bestMoveThisIteration;
                bestEvaluation = bestEvaluationThisIteration;
            }
        }
        UnityEngine.Debug.Log($"Searched to a depth of {searchedDepth}, positions searched: {positionsSearched}, Time taken: {stopwatch.ElapsedMilliseconds}ms, Engine best evaluation {bestEvaluation}");
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
    private void OrderMoves(uint[] moves, int moveCount, uint ttMove, int depth, int currentSearchDepth)
    {
        Array.Sort(moves, 0, moveCount, Comparer<uint>.Create((a, b) =>
        {
            if (depth == currentSearchDepth)
            {
                if (a == bestMove) return -1;
                if (b == bestMove) return 1;
            }
            if (a == ttMove) return -1;
            if (b == ttMove) return 1;

            return getMoveOrderScoreGuess(b).CompareTo(getMoveOrderScoreGuess(a));
        }));
    }
    private int MinMaxSearch(int depth, int currentSearchDepth, int alpha, int beta, bool isWhite)
    {
        if (stopwatch.ElapsedMilliseconds > searchTime) searchCancelled = true;
        if (searchCancelled) return 0; // value irrelevant, caller must never use it

        positionsSearched++;
        uint[] currentLegalMoves = moveGenerator.generateMoves(isWhite);
        int currentMoveIndex = moveGenerator.getMoveIndex();
        ulong zobristHash = board.getZobristHash();
        ulong zobristIndex = zobristHash & (tableSize - 1);

        int hashResult = probeHash(depth, alpha, beta, zobristHash, zobristIndex, out uint ttMove);
        if (hashResult != probeFailed && depth != currentSearchDepth)
            return hashResult;

        if (currentMoveIndex == 0)
        {
            if (moveGenerator.isInCheck(isWhite))
                return -checkmateEval * depth;
            return 0;
        }

        if (depth == 0)
        {
            int val = QuiescenceSearch(alpha, beta, isWhite);
            transpositionTable[zobristIndex] = new TTEntry(0, zobristHash, val, depth, TTFlag.EXACT);
            return val;
        }

        OrderMoves(currentLegalMoves, currentMoveIndex, ttMove,depth,currentSearchDepth);

        TTFlag hashFlag = TTFlag.ALPHA;
        uint bestMoveForNode = 0;

        for (int i = 0; i < currentMoveIndex; i++)
        {
            uint currentMove = currentLegalMoves[i];
            board.makeMove(currentMove);
            int eval = -MinMaxSearch(depth - 1, currentSearchDepth, -beta, -alpha, !isWhite);
            board.unMakeMove(currentMove);

            if (searchCancelled) return 0;

            if (eval > alpha)
            {
                alpha = eval;
                hashFlag = TTFlag.EXACT;
                bestMoveForNode = currentMove;
                if (depth == currentSearchDepth)
                {
                    bestMoveThisIteration = currentMove;
                    bestEvaluationThisIteration = eval;
                }
            }
            if (eval >= beta)
            {
                transpositionTable[zobristIndex] = new TTEntry(currentMove, zobristHash, beta, depth, TTFlag.BETA);
                return beta;
            }
        }

        transpositionTable[zobristIndex] = new TTEntry(bestMoveForNode, zobristHash, alpha, depth, hashFlag);
        return alpha;
    }
    private int QuiescenceSearch(int alpha, int beta, bool isWhite)
    {
        int standPat = evaluation.GetEvaluation(board, isWhite);
        if (standPat >= beta) return beta;
        if (standPat > alpha) alpha = standPat;

        uint[] captures = moveGenerator.generateCaptures(isWhite); // only captures (+ promotions)
        int captureCount = moveGenerator.getMoveIndex();

        // order captures too (MVV-LVA is enough here)
        Array.Sort(captures, 0, captureCount, Comparer<uint>.Create((a, b) =>
            getMoveOrderScoreGuess(b).CompareTo(getMoveOrderScoreGuess(a))));

        for (int i = 0; i < captureCount; i++)
        {
            if (Move.GetCapturedPiece(captures[i]) == Piece.none) continue;
            board.makeMove(captures[i]);
            int score = -QuiescenceSearch(-beta, -alpha, !isWhite);
            board.unMakeMove(captures[i]);

            if (searchCancelled) return alpha;

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }
}
