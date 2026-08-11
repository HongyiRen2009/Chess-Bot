
using EngineCore;
public class ChessEngine
{
    private Board board;
    private MoveGenerator moveGenerator;
    private Evaluation evaluation;
    private Search search;
    private NonUnityPerftTester nonUnityPerftTester;
    public bool isWhiteMove = true;
    private int depthToSearchTo;
    private float thinkingTimeInMilliseconds = 1000f;
    public Board Board => board;
    public MoveGenerator MoveGenerator => moveGenerator;
    public ushort getBestMove(bool isWhite)
    {
        return search.GetBestMove(depthToSearchTo*2, isWhite);
    }
    public ushort[] GetCurrentLegalMoves()
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
    public void makeMove(ushort move,bool flipMoves = true)
    {
        board.makeMove(move);
        if (flipMoves) isWhiteMove = !isWhiteMove;
        moveGenerator.generateMoves(isWhiteMove);
    }
    public void SetThinkingTime(float ms)
{
    search.SetSearchTime(ms);
}
    public void unMakeMove(ushort move,bool flipMoves = true)
    {
        board.unMakeMove(move);
        if (flipMoves) isWhiteMove = !isWhiteMove;
        moveGenerator.generateMoves(isWhiteMove);
    }
    public int GetPiece(int square)
    {
        return board.GetPiece(square);
    }
    public void InitializeEngine(string fenStartingPosition, int depth, float thinkingTime)
    {
        depthToSearchTo = depth;
        this.thinkingTimeInMilliseconds = thinkingTime;
        evaluation = new Evaluation();
        board = new Board(evaluation);
        moveGenerator = new MoveGenerator(board);
        search = new Search(board, moveGenerator, evaluation, thinkingTimeInMilliseconds);
        nonUnityPerftTester = new NonUnityPerftTester(board, MoveGenerator);
        isWhiteMove = board.convertFenStringToBitBoard(fenStartingPosition);
        moveGenerator.generateMoves(isWhiteMove);

    }
    public void DoPerftTest(int depth)
    {
        nonUnityPerftTester.PerftTest(depth, isWhiteMove);
    }
    public GameState GetGameState(bool isWhite)
    {
        ushort[] currentLegalMoves = moveGenerator.generateMoves(isWhite);
        int currentMoveIndex = moveGenerator.getMoveIndex();
        if (currentMoveIndex == 0)
        {
            if (moveGenerator.isInCheck(isWhite))
            {
                return isWhite ? GameState.BlackWins : GameState.WhiteWins;
            }
            return GameState.Stalemate;
        }
        return GameState.Ongoing;
    }
    public int GetEnPassentTargetSquare()
    {
        return board.EnPassentTargetSquare;
    }


}
