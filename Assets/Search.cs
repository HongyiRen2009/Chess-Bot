using EngineCore;
using System.Collections.Generic;
using UnityEngine;
public class Search
{
    [SerializeField] private MoveGenerator moveGenerator;
    [SerializeField] private Board board;
    private Dictionary<string, int> movesAfterMove = new Dictionary<string, int>();

    public int outputDepth;
    public Dictionary<string, int> getMovesAfterMove()
    {
        return movesAfterMove;
    }
    public Search(Board board, MoveGenerator moveGenerator)
    {
        this.moveGenerator = moveGenerator;
        this.board = board;
    }

    public int SearchMoves(int currentDepth, bool isWhite)
    {

        Move[] currentPsuedoLegalMoves = moveGenerator.generateMoves(isWhite);
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
            if (currentPsuedoLegalMoves[i].isCastling && currentDepth == outputDepth)
            {
                Debug.Log("HI");
            }
            board.makeMove(currentPsuedoLegalMoves[i]);

            int currentNumberOfMoves = currentDepth == 1
                ? 1
                : SearchMoves(currentDepth - 1, !isWhite);

            if (currentDepth == outputDepth)
            {
                string key = utils.convertBoardIndexToChessNotation(currentPsuedoLegalMoves[i].s1) + utils.convertBoardIndexToChessNotation(currentPsuedoLegalMoves[i].s2) + utils.pieceToString[currentPsuedoLegalMoves[i].promotionPiece];
                movesAfterMove[key] = currentNumberOfMoves;
            }

            numberOfMoves += currentNumberOfMoves;
            board.unMakeMove(currentPsuedoLegalMoves[i]);
        }
        return numberOfMoves;
    }
}
