using System;
using System.Linq;

using EngineCore;
// Minimal UCI loop. Reads commands from stdin, writes responses to stdout.
// Handles: uci, isready, ucinewgame, position [startpos|fen ...] [moves ...], go, stop, quit
public class UciEngine
{
    private ChessEngine engine;
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Fixed at construction because Search's time/depth are baked in when
    // ChessEngine.InitializeEngine() is called. Adjust to taste, or wire
    // up "go movetime/depth" parsing below if you want per-move control.
    private readonly int defaultDepth = 5;
    private readonly float defaultThinkTimeMs = 1000f;

    public void Run()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            if (!ProcessCommand(line.Trim()))
                break;
        }
    }

    private bool ProcessCommand(string line)
    {
        if (string.IsNullOrEmpty(line)) return true;
        string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        switch (tokens[0])
        {
            case "uci":
                Console.WriteLine("id name EngineCore");
                Console.WriteLine("id author YourName");
                Console.WriteLine("uciok");
                break;

            case "isready":
                Console.WriteLine("readyok");
                break;

            case "ucinewgame":
                engine = null;
                break;

            case "position":
                HandlePosition(tokens);
                break;

            case "go":
                HandleGo(tokens);
                break;

            case "stop":
                // No async/interruptible search in this engine, so nothing to do.
                break;

            case "quit":
                return false;
        }

        return true;
    }

    private void HandlePosition(string[] tokens)
    {
        if (tokens.Length < 2) return;

        string fen;
        if (tokens[1] == "startpos")
        {
            fen = StartFen;
        }
        else if (tokens[1] == "fen")
        {
            fen = string.Join(" ", tokens.Skip(2).TakeWhile(t => t != "moves"));
        }
        else
        {
            return;
        }

        engine = new ChessEngine();
        engine.InitializeEngine(fen, defaultDepth, defaultThinkTimeMs);

        int movesIndex = Array.IndexOf(tokens, "moves");
        if (movesIndex != -1)
        {
            for (int i = movesIndex + 1; i < tokens.Length; i++)
            {
                ApplyUciMove(tokens[i]);
            }
        }
    }

    private void ApplyUciMove(string uciMove)
    {
        if (engine == null || uciMove.Length < 4) return;

        int fromSquare = SquareFromAlgebraic(uciMove.Substring(0, 2));
        int toSquare = SquareFromAlgebraic(uciMove.Substring(2, 2));
        bool hasPromotion = uciMove.Length == 5;
        int promotionUncolored = hasPromotion ? PromotionCharToUncoloredPiece(uciMove[4]) : -1;

        ushort[] legalMoves = engine.GetCurrentLegalMoves();
        foreach (ushort move in legalMoves)
        {
            if (Move.GetSourceSquare(move) != fromSquare) continue;
            if (Move.GetTargetSquare(move) != toSquare) continue;
            bool isWhiteIfPromotion = toSquare < 32;
            int movePromo = Move.GetPromotionPiece(move,isWhiteIfPromotion);
            bool moveHasPromo = movePromo != Piece.none;

            if (hasPromotion != moveHasPromo) continue;
            if (hasPromotion && movePromo % 6 != promotionUncolored) continue;

            engine.makeMove(move);
            return;
        }

        // If we get here, the GUI sent a move we couldn't match against
        // legal moves - usually means a move-generation or encoding bug.
        Console.Error.WriteLine($"info string failed to apply move {uciMove}");
    }
private void HandleGo(string[] tokens)
{
    if (engine == null)
    {
        engine = new ChessEngine();
        engine.InitializeEngine(StartFen, defaultDepth, defaultThinkTimeMs);
    }

    bool sideToMove = engine.isWhiteMove;
    float allocatedMs = ComputeTimeBudget(tokens, sideToMove);
    engine.SetThinkingTime(allocatedMs);

    ushort bestMove = engine.getBestMove(sideToMove);
    Console.WriteLine("info score cp 0");

    Console.WriteLine("bestmove " + MoveToUci(bestMove));
}
private float ComputeTimeBudget(string[] tokens, bool isWhite)
        {
            long? movetime = null;
            long? wtime = null, btime = null, winc = null, binc = null;
            long? movestogo = null;
 
            for (int i = 1; i < tokens.Length; i++)
            {
                switch (tokens[i])
                {
                    case "movetime":
                        if (i + 1 < tokens.Length && long.TryParse(tokens[i + 1], out long mt)) movetime = mt;
                        break;
                    case "wtime":
                        if (i + 1 < tokens.Length && long.TryParse(tokens[i + 1], out long wt)) wtime = wt;
                        break;
                    case "btime":
                        if (i + 1 < tokens.Length && long.TryParse(tokens[i + 1], out long bt)) btime = bt;
                        break;
                    case "winc":
                        if (i + 1 < tokens.Length && long.TryParse(tokens[i + 1], out long wi)) winc = wi;
                        break;
                    case "binc":
                        if (i + 1 < tokens.Length && long.TryParse(tokens[i + 1], out long bi)) binc = bi;
                        break;
                    case "movestogo":
                        if (i + 1 < tokens.Length && long.TryParse(tokens[i + 1], out long mtg)) movestogo = mtg;
                        break;
                }
            }
 
            // "go movetime N" - use almost all of it, keep a small safety margin.
            if (movetime.HasValue)
            {
                return Math.Max(10, movetime.Value - 50);
            }
 
            long? remaining = isWhite ? wtime : btime;
            long increment = (isWhite ? winc : binc) ?? 0;
 
            // No clock info at all (e.g. "go" with nothing, or "go depth N") -
            // fall back to the fixed default so the engine still responds.
            if (!remaining.HasValue)
            {
                return defaultThinkTimeMs;
            }
 
            // Simple time management: assume a fixed horizon of moves left unless
            // the GUI tells us exactly how many (movestogo), spend a fraction of
            // the remaining clock plus most of the increment, and always leave a
            // safety buffer so we never flag on latency/GC pauses.
            int assumedMovesLeft = (int)(movestogo ?? 30);
            if (assumedMovesLeft < 1) assumedMovesLeft = 1;
 
            float budget = (float)remaining.Value / assumedMovesLeft + increment * 0.8f;
 
            const float safetyMarginMs = 50f;
            const float minBudgetMs = 20f;
 
            budget -= safetyMarginMs;
 
            // Never plan to spend more than ~40% of what's left on a single move,
            // so one heavy position can't itself flag a timeout later on.
            float hardCap = remaining.Value * 0.4f;
            if (budget > hardCap) budget = hardCap;
 
            if (budget < minBudgetMs) budget = minBudgetMs;
 
            return budget;
        }
 


    private string MoveToUci(ushort move)
    {
        int from = Move.GetSourceSquare(move);
        int to = Move.GetTargetSquare(move);
        string uci = SquareToAlgebraic(from) + SquareToAlgebraic(to);

        int promo = Move.GetPromotionPiece(move,engine.Board.GetPiece(from)<6);
        if (promo != Piece.none)
        {
            uci += PromotionPieceToChar(promo);
        }
        return uci;
    }

    // index 0 = a8, going left->right along the rank, then down the ranks.
    private int SquareFromAlgebraic(string square)
    {
        int file = square[0] - 'a';
        int rank = square[1] - '0';
        return (8 - rank) * 8 + file;
    }

    private string SquareToAlgebraic(int square)
    {
        return Utils.convertBoardIndexToChessNotation(square);
    }

    private int PromotionCharToUncoloredPiece(char c)
    {
        switch (char.ToLowerInvariant(c))
        {
            case 'q': return Piece.uncoloredQueen;
            case 'r': return Piece.uncoloredRook;
            case 'b': return Piece.uncoloredBishop;
            case 'n': return Piece.uncoloredKnight;
            default: return Piece.uncoloredQueen;
        }
    }

    private char PromotionPieceToChar(int piece)
    {
        switch (piece % 6)
        {
            case Piece.uncoloredQueen: return 'q';
            case Piece.uncoloredRook: return 'r';
            case Piece.uncoloredBishop: return 'b';
            case Piece.uncoloredKnight: return 'n';
            default: return 'q';
        }
    }
}
