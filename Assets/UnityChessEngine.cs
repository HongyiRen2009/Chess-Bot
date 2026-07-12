using EngineCore;
using UnityEngine;

public class UnityChessEngine : MonoBehaviour
{
    [SerializeField] private string fenStartingPosition;

    [SerializeField] private int depthToSearchTo;
    [SerializeField] private bool doPerftTest = false;
    [SerializeField] private float thinkingTimeInMilliseconds = 0.1f;
    private ChessEngine chessEngine;
    private PerftTester perftTester;
    public bool isWhiteMove => chessEngine.isWhiteMove;
    public ushort getBestMove(bool isWhite)
    {
        return chessEngine.getBestMove(isWhite);
    }
    public ushort[] GetCurrentLegalMoves()
    {
        return chessEngine.GetCurrentLegalMoves();
    }
    public ulong getBitboard(int piece, bool isWhite)
    {
        return chessEngine.getBitboard(piece, isWhite);
    }
    public ulong getBitboard(int piece)
    {
        return chessEngine.getBitboard(piece);
    }
    public void makeMove(ushort move, bool flipMoves = true)
    {
        chessEngine.makeMove(move, flipMoves);
    }
    public void unMakeMove(ushort move, bool flipMoves = true)
    {
        chessEngine.unMakeMove(move, flipMoves);
    }
    public GameState GetGameState(bool isWhite)
    {
        return chessEngine.GetGameState(isWhite);
    }
    public int GetPiece(int square)
    {
        return chessEngine.GetPiece(square);
    }
    public int GetEnpassentTargetSquare()
    {
        return chessEngine.GetEnPassentTargetSquare();
    }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        chessEngine = new ChessEngine();
        chessEngine.InitializeEngine(fenStartingPosition, depthToSearchTo, thinkingTimeInMilliseconds);
        perftTester = new PerftTester(chessEngine.Board, chessEngine.MoveGenerator);
        if (doPerftTest) perftTester.PerftTest(depthToSearchTo, true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
