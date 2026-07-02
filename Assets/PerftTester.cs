using EngineCore;
using System.Collections.Generic;
using UnityEngine;
public class PerftTester
{
    [SerializeField] private MoveGenerator moveGenerator;
    [SerializeField] private Board board;
    private Dictionary<string, int> movesAfterMove = new Dictionary<string, int>();

    public int outputDepth;
    public Dictionary<string, int> getMovesAfterMove()
    {
        return movesAfterMove;
    }
    public PerftTester(Board board, MoveGenerator moveGenerator)
    {
        this.moveGenerator = moveGenerator;
        this.board = board;
    }

    public int SearchMoves(int currentDepth, bool isWhite)
    {

        uint[] currentLegalMoves = moveGenerator.generateMoves(isWhite);
        int currentMoveIndex = moveGenerator.getMoveIndex();
        int numberOfMoves = 0;
        for (int i = 0; i < currentMoveIndex; i++)
        {
            //ulong test =board.GetAllPiecesBitboard();

            //board.makeMove(currentPsuedoLegalMoves[i]);
            //board.unMakeMove(currentPsuedoLegalMoves[i]);
            //if (test != board.GetAllPiecesBitboard()) 
            //{
            //    Debug.Log($"ERROR, expected {test} but got {board.GetAllPiecesBitboard()} instead, isCapture:{currentPsuedoLegalMoves[i].capturePiece!=piece.none}, isCastling:{currentPsuedoLegalMoves[i].isCastling}, piece:{currentPsuedoLegalMoves[i].movePiece}, promotionPiece:{currentPsuedoLegalMoves[i].promotionPiece} depth:{currentDepth}");
            //}
            if (Move.IsCastling(currentLegalMoves[i]) && currentDepth == outputDepth)
            {
                Debug.Log("HI");
            }
            board.makeMove(currentLegalMoves[i]);

            int currentNumberOfMoves = currentDepth == 1
                ? 1
                : SearchMoves(currentDepth - 1, !isWhite);

            if (currentDepth == outputDepth)
            {
                string key = Utils.convertBoardIndexToChessNotation(Move.GetSourceSquare(currentLegalMoves[i])) + Utils.convertBoardIndexToChessNotation(Move.GetTargetSquare(currentLegalMoves[i])) + Utils.pieceToString[Move.GetPromotionPiece(currentLegalMoves[i])];
                movesAfterMove[key] = currentNumberOfMoves;
            }

            numberOfMoves += currentNumberOfMoves;
            board.unMakeMove(currentLegalMoves[i]);
        }
        return numberOfMoves;
    }
}
