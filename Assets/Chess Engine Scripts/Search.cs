using EngineCore;
using System;
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

    // Upper bound on ply we could ever reach (nominal search depth + a safety margin
    // for check/promotion extensions). Used to size ply-indexed buffers so extensions
    // can never push us out of bounds.
    private const int MaxPly = 128;

    private int[][] scoreBuffers;
    private const int MaxQuiescenceDepth = 64; // effectively unreachable in practice, but a safe hard ceiling
    private int[][] qScoreBuffers;
    private ushort[,] killerMoves = new ushort[2, MaxPly];
    private int[,] historyMoves = new int[12, 64];
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
        public TTEntry(ushort bestMove, ulong key, int evaluation, int depth, TTFlag flag)
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


    public Search(Board board, MoveGenerator moveGenerator, Evaluation evaluation, float thinkingTime)
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
    private int getMoveOrderScoreGuess(ushort move, bool isWhite)
    {
        int moveScoreGuess = 0;
        int sourceSquare = Move.GetSourceSquare(move);
        int targetSquare = Move.GetTargetSquare(move);
        int movePiece = board.GetPiece(sourceSquare);
        int moveCapturePiece = board.GetPiece(targetSquare);
        int promotionPiece = Move.GetPromotionPiece(move, isWhite);
        bool opponentCanCapture = (moveGenerator.getAttackingSquares(!isWhite) & (1ul << targetSquare)) != 0;
        bool quietMove = true;
        if (opponentCanCapture)
        {
            moveScoreGuess -= Utils.GetPieceValue(movePiece);
        }
        if (moveCapturePiece != Piece.none)
        {
            moveScoreGuess += 10 * Utils.GetPieceValue(moveCapturePiece) - Utils.GetPieceValue(movePiece);
            quietMove = false;

        }
        if (promotionPiece != Piece.none)
        {
            moveScoreGuess += Utils.GetPieceValue(promotionPiece);
            quietMove = false;
        }
        if (quietMove)
        {
            moveScoreGuess += historyMoves[movePiece, targetSquare];
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
        scoreBuffers = new int[MaxPly][];
        for (int i = 0; i < scoreBuffers.Length; i++)
        {
            scoreBuffers[i] = new int[218];
        }
        qScoreBuffers = new int[MaxQuiescenceDepth][];
        for (int i = 0; i < MaxQuiescenceDepth; i++)
            qScoreBuffers[i] = new int[218];
        stopwatch.Restart();
        int searchedDepth = 0;
        for (int currentSearchDepth = 1; currentSearchDepth <= 64; currentSearchDepth++) // With iterative deepening, there should be no max search depth cap
        {
            bestMoveThisIteration = 0;
            bestEvaluationThisIteration = 0;
            MinMaxSearch(currentSearchDepth, 0, currentSearchDepth, -maxEval, maxEval, isWhite, 0, 0);
            searchedDepth = currentSearchDepth;
            if (searchCancelled) break;
            if (bestMoveThisIteration != 0)
            {
                bestMove = bestMoveThisIteration;
                bestEvaluation = bestEvaluationThisIteration;
            }
        }
        //UnityEngine.Debug.Log("Searched " + positionsSearched + " positions in " + searchTime + "ms");
        return bestMove;
    }
    private int probeHash(int depth, int alpha, int beta, ulong zobristHash, ulong zobristIndex, out TTEntry ttEntry)
    {
        ttEntry = null;
        TTEntry entry = transpositionTable[zobristIndex];
        if (entry != null && entry.key == zobristHash)
        {
            ttEntry = entry;
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
        }
        return probeFailed;
    }
    private void ScoreMoves(ushort[] moves, int moveCount, int[] scores, ushort ttMove, int ply, int currentSearchDepth, bool isRoot, bool isWhite)
    {
        for (int i = 0; i < moveCount; i++)
        {
            ushort move = moves[i];
            if (isRoot && move == bestMove)
                scores[i] = int.MaxValue;
            else if (move == ttMove)
                scores[i] = int.MaxValue - 1;
            else if (killerMoves[0, ply] == move)
                scores[i] = 99;
            else if (killerMoves[1, ply] == move)
                scores[i] = 98;
            else
                scores[i] = getMoveOrderScoreGuess(move, isWhite);
        }
    }

    // Selects the best-scoring move among moves and swaps it to currentIndex
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
    private int MinMaxSearch(int depth, int ply, int currentSearchDepth, int alpha, int beta, bool isWhite, int numExtensions, ushort excludedMove)
    {
        if (stopwatch.ElapsedMilliseconds > searchTime) searchCancelled = true;
        if (searchCancelled) return 0; // value irrelevant, caller must never use it
        bool isRoot = ply == 0;
        if (!isRoot && board.GetPositionCount(board.getZobristHash()) >= 2) return 0;
        positionsSearched++;
        ushort[] currentLegalMoves = moveGenerator.generateMoves(isWhite);
        int currentMoveIndex = moveGenerator.getMoveIndex();
        ulong zobristHash = board.getZobristHash();
        ulong zobristIndex = zobristHash & (tableSize - 1);
        // Check TT table
        int hashResult = probeHash(depth, alpha, beta, zobristHash, zobristIndex, out TTEntry ttEntry);
        if (hashResult != probeFailed && !isRoot && excludedMove == 0)
            return hashResult;
        bool inCheck = moveGenerator.isInCheck(isWhite);
        // If there's no legal moves
        if (currentMoveIndex == 0)
        {
            // Checkmate
            if (inCheck)
                return -checkmateEval + ply;
            // Stalemate
            return 0;
        }
        // Quiescence Search
        if (depth == 0)
        {
            int val = QuiescenceSearch(alpha, beta, isWhite);
            if (excludedMove == 0)
                transpositionTable[zobristIndex] = new TTEntry(0, zobristHash, val, depth, TTFlag.EXACT);
            return val;
        }
        int staticEval = evaluation.GetEvaluation(board, isWhite);

        // Reverse Futility Pruning
        if (!inCheck && depth <= 3 && !isRoot
            && beta < checkmateEval - ply && beta > -checkmateEval + ply)
        {
            int margin = 75 * depth + 50;
            if (staticEval - margin >= beta)
                return beta;
        }

        // Null move pruning
        if (!inCheck && depth >= 3 && board.HasNonPawnPieces(isWhite))
        {
            const int R = 2;
            board.makeNullMove();
            int nullEval = -MinMaxSearch(depth - 1 - R, ply + 1, currentSearchDepth, -beta, -beta + 1, !isWhite, numExtensions, excludedMove);
            board.unMakeNullMove();

            if (searchCancelled) return 0;
            if (nullEval >= beta) return beta;
        }

        // Move ordering
        int[] scores = scoreBuffers[ply];
        ScoreMoves(currentLegalMoves, currentMoveIndex, scores, ttEntry == null ? (ushort)0 : ttEntry.bestMove, ply, currentSearchDepth, isRoot, isWhite);

        TTFlag hashFlag = TTFlag.ALPHA;
        ushort bestMoveForNode = 0;

        for (int i = 0; i < currentMoveIndex; i++)
        {
            PickMove(currentLegalMoves, scores, currentMoveIndex, i);
            ushort currentMove = currentLegalMoves[i];
            if (currentMove == excludedMove) continue;
            bool isCapture = board.GetPiece(Move.GetTargetSquare(currentMove)) != Piece.none;
            bool isPromotion = Move.HasPromotion(currentMove);
            // Futility Pruning
            if (!inCheck && !isCapture && !isPromotion && depth == 1)
            {
                int futilityMargin = 100;
                if (staticEval + futilityMargin <= alpha)
                {
                    continue;
                }
            }
            // Singular move extension
            int moveExtension = 0;
            bool isSingularCandidate =
                !isRoot &&
                excludedMove == 0 &&
                ttEntry != null &&
                currentMove == ttEntry.bestMove &&
                depth >= 8 &&
                ttEntry.depth >= depth - 3 &&
                ttEntry.flag != TTFlag.ALPHA &&
                Math.Abs(ttEntry.evaluation) < checkmateEval - MaxPly;

            if (isSingularCandidate)
            {
                int margin = 20 + 2 * depth;
                int singularBeta = ttEntry.evaluation - margin;

                int singularDepth = depth - 3;

                int singularScore = MinMaxSearch(singularDepth, ply, currentSearchDepth,
                    singularBeta - 1, singularBeta, isWhite, numExtensions, currentMove);

                if (searchCancelled) return 0;

                if (singularScore < singularBeta)
                {
                    if (numExtensions < 3)
                        moveExtension = 1;
                }
            }

            board.makeMove(currentMove);
            int targetSquare = Move.GetTargetSquare(currentMove);
            // Late move reduction
            int eval;
            bool givesCheck = moveGenerator.isInCheck(!isWhite);
            bool canReduce = i >= 3 && depth >= 3 && moveExtension == 0 && !isCapture && !isPromotion && !inCheck && !givesCheck;
            if (canReduce)
            {
                int reduceDepth = 1;
                eval = -MinMaxSearch(depth - 1 - reduceDepth, ply + 1, currentSearchDepth, -alpha - 1, -alpha, !isWhite, numExtensions, excludedMove);
                if (eval > alpha && !searchCancelled)
                    eval = -MinMaxSearch(depth - 1, ply + 1, currentSearchDepth, -beta, -alpha, !isWhite, numExtensions, excludedMove);
            }
            else
            {
                eval = -MinMaxSearch(depth - 1 + moveExtension, ply + 1, currentSearchDepth, -beta, -alpha, !isWhite, numExtensions + moveExtension, excludedMove);
            }
            board.unMakeMove(currentMove);

            if (searchCancelled) return 0;
            // Alpha cutoff
            if (eval > alpha)
            {
                alpha = eval;
                hashFlag = TTFlag.EXACT;
                bestMoveForNode = currentMove;
                if (isRoot)
                {
                    bestMoveThisIteration = currentMove;
                    bestEvaluationThisIteration = eval;
                }
            }
            // If move is not gonna be a singular move competitor, prune the branch
            if (excludedMove != 0 && eval >= beta)
            {
                return eval;
            }
            if (eval >= beta)
            {
                if (excludedMove == 0)
                    transpositionTable[zobristIndex] = new TTEntry(currentMove, zobristHash, beta, depth, TTFlag.BETA);

                if (!isCapture && !isPromotion)
                {
                    // Store killer move
                    if (killerMoves[0, ply] != currentMove)
                    {
                        killerMoves[1, ply] = killerMoves[0, ply];
                        killerMoves[0, ply] = currentMove;
                    }

                    int movePiece = board.GetPiece(Move.GetSourceSquare(currentMove));
                    historyMoves[movePiece, targetSquare] += depth * depth;
                }
                return beta;
            }
        }
        if (excludedMove == 0)
            transpositionTable[zobristIndex] = new TTEntry(bestMoveForNode, zobristHash, alpha, depth, hashFlag);
        return alpha;
    }
    private int QuiescenceSearch(int alpha, int beta, bool isWhite, int qPly = 0)
    {
        if (stopwatch.ElapsedMilliseconds > searchTime) searchCancelled = true;
        if (searchCancelled) return alpha;
        int standPat = evaluation.GetEvaluation(board, isWhite);
        if (standPat >= beta) return beta;
        if (standPat > alpha) alpha = standPat;

        ushort[] captures = moveGenerator.generateCaptures(isWhite);
        int captureCount = moveGenerator.getMoveIndex();

        // Safety valve: if we somehow blow past the cap, fall back to a local array rather than corrupting/overrunning a shared buffer.
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