using EngineCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
public class Search
{
    private MoveGenerator moveGenerator;
    private Board board;
    private Evaluation evaluation;
    private ushort bestMove;
    private ushort bestMoveThisIteration;
    private int bestEvaluation;
    private int bestEvaluationThisIteration;
    private int positionsSearched;
    private int maxEval = 9999999;
    private int checkmateEval = 99999;
    private int probeFailed = 67696769;
    private float searchTime;
    private bool searchCancelled;
    private Stopwatch stopwatch = new Stopwatch();
    private int[][] scoreBuffers;
    private const int MaxQuiescenceDepth = 64; // effectively unreachable in practice, but a safe hard ceiling
    private int[][] qScoreBuffers;
    private ushort[,] killerMoves = new ushort[2,16]; // Max Search depth, probably never going to get there
    private int[,] historyMoves = new int[12, 64];
    const int million = 1000000;
    private enum TTFlag
    {
        EXACT,
        ALPHA,
        BETA
    }
    private class TTEntry
    {
        public ushort bestMove;
        public ulong key;
        public int evaluation;
        public int depth;
        public TTFlag flag;
        public TTEntry(ushort bestMove, ulong key, int evaluation, int depth,TTFlag flag)
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
public void SetSearchTime(float ms)
{
    searchTime = ms;
}
    private int getMoveOrderScoreGuess(ushort move,bool isWhite)
    {
        int moveScoreGuess = 0;
        int sourceSquare = Move.GetSourceSquare(move);
        int targetSquare = Move.GetTargetSquare(move);
        int movePiece = board.GetPiece(sourceSquare);
        int moveCapturePiece = board.GetPiece(targetSquare);
        int promotionPiece = Move.GetPromotionPiece(move, isWhite);
        bool opponentCanCapture = (moveGenerator.getAttackingSquares(!isWhite) & (1ul << targetSquare)) != 0;
        bool quietMove = true;
        if( opponentCanCapture)
        {
            moveScoreGuess -= Utils.GetPieceValue(movePiece);
        }
        if (moveCapturePiece != Piece.none)
        {
            moveScoreGuess += 10*Utils.GetPieceValue(moveCapturePiece) - Utils.GetPieceValue(movePiece);
            quietMove = false;

        }
        if (promotionPiece != Piece.none)
        {
            moveScoreGuess += Utils.GetPieceValue(promotionPiece);
            quietMove = false;
        }
        if (quietMove)
        {
            moveScoreGuess += historyMoves[movePiece,targetSquare];
        }
        return moveScoreGuess;
    }

    public ushort GetBestMove(int depth, bool isWhite)
    {
        stopwatch.Start();
        bestMove = 0;
        bestEvaluation = 0;
        positionsSearched = 0;
        searchCancelled = false;
        scoreBuffers = new int[depth+1][];
        for(int i = 0; i < scoreBuffers.Length; i++)
        {
            scoreBuffers[i] = new int[218];
        }
        qScoreBuffers = new int[MaxQuiescenceDepth][];
        for (int i = 0; i < MaxQuiescenceDepth; i++)
            qScoreBuffers[i] = new int[218];
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
        UnityEngine.Debug.Log("Searched " + positionsSearched + " positions in " + searchTime + "ms");
        return bestMove;
    }
    private int probeHash(int depth, int alpha, int beta,ulong zobristHash,ulong zobristIndex, out ushort ttMove)
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
    private void ScoreMoves(ushort[] moves, int moveCount, int[] scores, ushort ttMove, int depth, int currentSearchDepth, bool isWhite)
    {
        bool isRoot = depth == currentSearchDepth;
        for (int i = 0; i < moveCount; i++)
        {
            ushort move = moves[i];
            if (isRoot && move == bestMove)
                scores[i] = int.MaxValue;
            else if (move == ttMove)
                scores[i] = int.MaxValue - 1;
            else if (killerMoves[0,depth] == move)
                scores[i] = 99;
            else if (killerMoves[0,depth] == move)
                scores[i] = 98;
            else
                scores[i] = getMoveOrderScoreGuess(move, isWhite);
        }
    }

    // Selects the best-scoring move among moves[currentIndex..moveCount) and swaps it to currentIndex
    private void PickMove(ushort[] moves, int[] scores, int moveCount, int currentIndex)
    {
        int bestIdx = currentIndex;
        int bestScore = scores[currentIndex];
        for (int i = currentIndex + 1; i < moveCount; i++)
        {
            if (scores[i] > bestScore)
            {
                bestScore = scores[i];
                bestIdx = i;
            }
        }
        if (bestIdx != currentIndex)
        {
            (moves[currentIndex], moves[bestIdx]) = (moves[bestIdx], moves[currentIndex]);
            (scores[currentIndex], scores[bestIdx]) = (scores[bestIdx], scores[currentIndex]);
        }
    }
    private int MinMaxSearch(int depth, int currentSearchDepth, int alpha, int beta, bool isWhite)
    {
        if (stopwatch.ElapsedMilliseconds > searchTime) searchCancelled = true;
        if (searchCancelled) return 0; // value irrelevant, caller must never use it

        positionsSearched++;
        ushort[] currentLegalMoves = moveGenerator.generateMoves(isWhite);
        int currentMoveIndex = moveGenerator.getMoveIndex();
        ulong zobristHash = board.getZobristHash();
        ulong zobristIndex = zobristHash & (tableSize - 1);

        int hashResult = probeHash(depth, alpha, beta, zobristHash, zobristIndex, out ushort ttMove);
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

        int[] scores = scoreBuffers[depth];
        ScoreMoves(currentLegalMoves, currentMoveIndex, scores, ttMove, depth, currentSearchDepth, isWhite);

        TTFlag hashFlag = TTFlag.ALPHA;
        ushort bestMoveForNode = 0;

        for (int i = 0; i < currentMoveIndex; i++)
        {
            PickMove(currentLegalMoves, scores, currentMoveIndex, i);
            ushort currentMove = currentLegalMoves[i];

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
                bool isCapture = board.GetPiece(Move.GetTargetSquare(currentMove)) != Piece.none;
                bool isPromotion = Move.HasPromotion(currentMove);
                if (!isCapture && !isPromotion)
                {
                    if (killerMoves[0, depth] != currentMove)
                    {
                        killerMoves[1, depth] = killerMoves[0, depth];
                        killerMoves[0, depth] = currentMove;
                    }

                    int movePiece = board.GetPiece(Move.GetSourceSquare(currentMove));
                    int targetSquare = Move.GetTargetSquare(currentMove);
                    historyMoves[movePiece, targetSquare] += depth * depth;
                }
                return beta;
            }
        }

        transpositionTable[zobristIndex] = new TTEntry(bestMoveForNode, zobristHash, alpha, depth, hashFlag);
        return alpha;
    }
    private int QuiescenceSearch(int alpha, int beta, bool isWhite, int qPly = 0)
    {
        int standPat = evaluation.GetEvaluation(board, isWhite);
        if (standPat >= beta) return beta;
        if (standPat > alpha) alpha = standPat;

        ushort[] captures = moveGenerator.generateCaptures(isWhite);
        int captureCount = moveGenerator.getMoveIndex();

        // Safety valve: if we somehow blow past the cap, fall back to a local array
        // rather than corrupting/overrunning a shared buffer.
        int[] scores = qPly < MaxQuiescenceDepth ? qScoreBuffers[qPly] : new int[captureCount];

        for (int i = 0; i < captureCount; i++)
            scores[i] = getMoveOrderScoreGuess(captures[i], isWhite);

        for (int i = 0; i < captureCount; i++)
        {
            PickMove(captures, scores, captureCount, i);

            if (board.GetPiece(Move.GetTargetSquare(captures[i])) == Piece.none) continue;

            board.makeMove(captures[i]);
            int score = -QuiescenceSearch(-beta, -alpha, !isWhite, qPly + 1);
            board.unMakeMove(captures[i]);

            if (searchCancelled) return alpha;

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }
}
