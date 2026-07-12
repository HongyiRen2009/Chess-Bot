using EngineCore;
using System;
using System.Collections.Generic;
using System.IO;
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
    public void PerftTest(int depthToSearchTo,bool isWhiteMove)
    {
        if (depthToSearchTo == 0) return;
        int depth = depthToSearchTo;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        int numMoves;
        numMoves = SearchMoves(depth, isWhiteMove);
        
        stopwatch.Stop();
        Dictionary<string, int> movesAfterMove = getMovesAfterMove();
        int dictNumMoves = 0;
        int fishNumMoves = 0;
        string content = "";
        string filePath = Path.Combine(Application.dataPath, "moves.txt");
        foreach (KeyValuePair<string, int> kvp in movesAfterMove)
        {
            content += kvp.Key + " " + kvp.Value.ToString() + "\n";
            dictNumMoves += kvp.Value;
        }
        File.WriteAllText(filePath, content);
        Debug.Log($"Number of moves after {depth} is {numMoves}");
        string fishFilePath = Path.Combine(Application.dataPath, "stockfishNumMoves.txt");
        string[] lines = File.ReadAllLines(fishFilePath);

        Dictionary<string, int> dataDict = new Dictionary<string, int>();

        foreach (string line in lines)
        {
            // Split by the colon and space
            string[] parts = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                string key = parts[0].Trim();

                // Parse the value as an integer (use int.TryParse to prevent errors on bad data)
                if (int.TryParse(parts[1].Trim(), out int value))
                {
                    dataDict[key] = value;
                    fishNumMoves += value;
                }
            }
        }
        foreach (KeyValuePair<string, int> kvp in movesAfterMove)
        {
            if (dataDict[kvp.Key] != kvp.Value)
            {
                Debug.Log("ERROR, MOVES DIDN'T MATCH. EXPECTED: " + kvp.Key + " " + dataDict[kvp.Key] + ", BUT GOT: " + kvp.Key + " " + kvp.Value);
            }
        }
        foreach (KeyValuePair<string, int> kvp in dataDict)
        {
            if (!movesAfterMove.ContainsKey(kvp.Key))
            {
                Debug.Log("ERROR, MOVE NOT FOUND: " + kvp.Key);
            }
        }
        Debug.Log(isWhiteMove);
        Debug.Log($"Time taken: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");

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
