using System;
using System.Collections.Generic;
using System.IO;

public class NonUnityPerftTester
{
    private MoveGenerator moveGenerator;
    private Board board;

    public NonUnityPerftTester(Board board, MoveGenerator moveGenerator)
    {
        this.moveGenerator = moveGenerator;
        this.board = board;
    }
    public void PerftTest(int depthToSearchTo, bool isWhiteMove)
    {
        if (depthToSearchTo == 0) return;
        int depth = depthToSearchTo;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        int numMoves;
        numMoves = SearchMoves(depth, isWhiteMove);

        stopwatch.Stop();
        Console.WriteLine($"Searched {numMoves} moves in {stopwatch.ElapsedMilliseconds}ms");

    }
    public int SearchMoves(int currentDepth, bool isWhite)
    {

        ushort[] currentLegalMoves = moveGenerator.generateMoves(isWhite);
        int currentMoveIndex = moveGenerator.getMoveIndex();
        int numberOfMoves = 0;
        for (int i = 0; i < currentMoveIndex; i++)
        {
            board.makeMove(currentLegalMoves[i]);

            int currentNumberOfMoves = currentDepth == 1
                ? 1
                : SearchMoves(currentDepth - 1, !isWhite);

            //if (currentDepth == outputDepth)
            //{
            //    string key = Utils.convertBoardIndexToChessNotation(Move.GetSourceSquare(currentLegalMoves[i])) + Utils.convertBoardIndexToChessNotation(Move.GetTargetSquare(currentLegalMoves[i])) + Utils.pieceToString[Move.GetPromotionPiece(currentLegalMoves[i])];
            //    movesAfterMove[key] = currentNumberOfMoves;
            //}

            numberOfMoves += currentNumberOfMoves;
            board.unMakeMove(currentLegalMoves[i]);
        }
        return numberOfMoves;
    }
}
