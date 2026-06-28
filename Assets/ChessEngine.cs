
using EngineCore;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
public class ChessEngine : MonoBehaviour
{
    private Search search;
    private Board board;
    private MoveGenerator moveGenerator;
    public bool isWhiteMove = true;
    [SerializeField] private string fenStartingPosition;
    [SerializeField] private int depthToSearchTo;
    public Move[] GetCurrentLegalMoves()
    {
        return moveGenerator.getCurrentLegalMoves();
    }
    public ulong getBitboard(int piece, bool isWhite)
    {
        return board.GetBitboard(piece,isWhite);
    }
    public ulong getBitboard(int piece)
    {
        return board.GetBitboard(piece);
    }
    public void makeMove(Move move,bool flipMoves = true)
    {
        board.makeMove(move);
        if (flipMoves) isWhiteMove = !isWhiteMove;
        moveGenerator.generateMoves(isWhiteMove);
    }
    public void unMakeMove(Move move,bool flipMoves = true)
    {
        board.unMakeMove(move);
        if (flipMoves) isWhiteMove = !isWhiteMove;
        moveGenerator.generateMoves(isWhiteMove);
    }
    void Awake()
    {
        board = new Board();
        moveGenerator = new MoveGenerator(board);
        search = new Search(board, moveGenerator);
        search.outputDepth = depthToSearchTo;
        isWhiteMove = board.convertFenStringToBitBoard(fenStartingPosition);
        moveGenerator.generateMoves(isWhiteMove);
        if (depthToSearchTo == 0) return;
        int depth = depthToSearchTo;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        int numMoves = search.SearchMoves(depth, isWhiteMove);
        stopwatch.Stop();
        Dictionary<string, int> movesAfterMove = search.getMovesAfterMove();
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
                Debug.Log("ERROR, MOVES DIDN'T MATCH. EXPECTED: "+ kvp.Key +" "+ dataDict[kvp.Key]+", BUT GOT: " + kvp.Key +" "+ kvp.Value);
            }
        }
        foreach (KeyValuePair<string, int> kvp in dataDict)
        {
            if (!movesAfterMove.ContainsKey(kvp.Key))
            {
                Debug.Log("ERROR, MOVE NOT FOUND: "+kvp.Key);
            }
        }
        Debug.Log(isWhiteMove);
        Debug.Log($"Time taken: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks)");



    }

}
