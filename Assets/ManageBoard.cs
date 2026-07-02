using EngineCore;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManageBoard : MonoBehaviour
{
    [SerializeField] private GameObject boardSquare;
    [SerializeField] private GameObject chessPiece;
    [SerializeField] private GameObject boardCircles;
    [Header("Board Transform Containers")]
    [SerializeField] private Transform boardSquaresTransform;
    [SerializeField] private Transform whiteBoardPiecesTransform;
    [SerializeField] private Transform blackBoardPiecesTransform;
    [SerializeField] private Transform boardCirclesTransform;
    [Header("Sprites")]
    [SerializeField] private Sprite circle;
    [SerializeField] private Sprite hollowCircle;

    [SerializeField] private ChessEngine chessEngine;
    [SerializeField] private TextMeshProUGUI result;
    private uint[] moves;
    private ManageChessPiece[] pieces = new ManageChessPiece[64];
    private uint lastMove;
    public bool isGameOver;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Create the board
        for(int i = 0; i < 64; i++)
        {
            GameObject currBoardSquare = Instantiate(boardSquare, boardSquaresTransform);
            int fileRankSum = (i % 8) + (int)Mathf.Floor(i / 8);
            currBoardSquare.GetComponent<Image>().color = fileRankSum % 2 == 0 ? new Color32(242, 225, 192, 255):new Color32(112, 73, 47, 255) ;
        }
        for(int piece = 0; piece < 12; piece++)
        {
            ulong bitBoard = chessEngine.getBitboard(piece);
            while (bitBoard > 0)
            {
                int pieceSquare = BitScanner.BitScanForward(bitBoard);
                GameObject currPiece = Instantiate(chessPiece, piece<6 ? whiteBoardPiecesTransform:blackBoardPiecesTransform);
                pieces[pieceSquare] = currPiece.GetComponent<ManageChessPiece>();
                pieces[pieceSquare].Initialize(pieceSquare, piece,this);

                bitBoard &= ~(1ul << pieceSquare);
            }
        }
        moves = chessEngine.GetCurrentLegalMoves();
    }
    private void clearCircles()
    {
        GameObject[] previousCircles = GameObject.FindGameObjectsWithTag("BoardCircle");
        foreach (GameObject circle in previousCircles)
        {
            Destroy(circle);
        }
    }
    public void AddCircles(int startSquare)
    {
        clearCircles();
        for(int i = 0; i < moves.Length; i++)
        {
            if (moves[i].Equals(default)) break;
            if (Move.GetSourceSquare(moves[i]) == startSquare)
            {
                GameObject currBoardCircle = Instantiate(boardCircles, boardCirclesTransform);
                currBoardCircle.GetComponent<RectTransform>().anchoredPosition = new Vector2((Move.GetTargetSquare(moves[i]) % 8) * 133, Mathf.Floor(Move.GetTargetSquare(moves[i]) / 8) * -133);
                currBoardCircle.GetComponent<Image>().sprite = Move.GetCapturedPiece(moves[i]) == Piece.none || Move.IsEnPassent(moves[i]) ? circle : hollowCircle;
            }
        }
        boardCirclesTransform.SetAsLastSibling();
        (chessEngine.isWhiteMove ? whiteBoardPiecesTransform : blackBoardPiecesTransform).SetAsLastSibling();
        
    }
    public void getMoveCount()
    {
        Debug.Log(moves.Length);
        string content = "";
        string filePath = Path.Combine(Application.dataPath, "moves.txt");
        foreach (uint move in moves)
        {
            content += Move.GetSourceSquare(move) + " " + Move.GetTargetSquare(move) + Utils.pieceToString[Move.GetPromotionPiece(move)] +"\n";
        }
        File.WriteAllText(filePath, content);
    }
    public void makeMove(uint move)
    {
        int startSquare = Move.GetSourceSquare(move);
        int endSquare = Move.GetTargetSquare(move);

        lastMove = move;
        if (pieces[endSquare] != null)
        {
            Destroy(pieces[endSquare].gameObject);
        }
        pieces[endSquare] = pieces[startSquare];
        pieces[startSquare] = null;
        if (Move.GetPromotionPiece(move) != Piece.none)
        {
            pieces[endSquare].SetPiece(Move.GetPromotionPiece(move));
        }
        if (Move.IsCastling(move))
        {
            if (Move.IsWhite(move))
            {
                if (endSquare == 58)
                {
                    pieces[59] = pieces[56];
                    pieces[59].setPosition(59);
                    pieces[56] = null;
                }
                else
                {
                    pieces[61] = pieces[63];
                    pieces[61].setPosition(61);
                    pieces[63] = null;
                }
            }
            else
            {
                if (endSquare == 2)
                {
                    pieces[3] = pieces[0];
                    pieces[3].setPosition(3);
                    pieces[0] = null;
                }
                else
                {
                    pieces[5] = pieces[7];
                    pieces[5].setPosition(5);
                    pieces[7] = null;
                }
            }
        }
        if (Move.IsEnPassent(move))
        {
            Destroy(pieces[endSquare + (Move.IsWhite(move) ? 8 : -8)].gameObject);
        }
        pieces[endSquare].setPosition(endSquare);
        chessEngine.makeMove(move);
        GameState gameState = chessEngine.getGameState(chessEngine.isWhiteMove);
        if (gameState != GameState.Ongoing)
        {
            switch(gameState) {
                case GameState.WhiteWins:
                    result.text = "Checkmate: White Wins";
                    break;
                case GameState.BlackWins:
                    result.text = "Checkmate: Black Wins";
                    break;
                case GameState.Stalemate:
                    result.text = "Stalemate: draw";
                    break;

            }
            isGameOver = true;
        }
    }
    public void unMakeLastMove()
    {
        if (lastMove.Equals(null)) return;
        int sourceSquare = Move.GetSourceSquare(lastMove);
        int targetSquare = Move.GetTargetSquare(lastMove);
        int movePiece = Move.GetPiece(lastMove);
        int capturePiece = Move.GetCapturedPiece(lastMove);
        int promotionPiece = Move.GetPromotionPiece(lastMove);
        bool isWhite = Move.IsWhite(lastMove);
        bool isEnPassent = Move.IsEnPassent(lastMove);
        bool isCastling = Move.IsCastling(lastMove);
        pieces[sourceSquare] = pieces[targetSquare];
        pieces[targetSquare] = null;
        pieces[sourceSquare].setPosition(sourceSquare);
        if (capturePiece != Piece.none)
        {
            GameObject currPiece = Instantiate(chessPiece, capturePiece < 6 ? whiteBoardPiecesTransform : blackBoardPiecesTransform);
            int EnPassentOffset = 0;
            if (isEnPassent) EnPassentOffset = +(isWhite ? 8 : -8);
            int pieceSquare = targetSquare + EnPassentOffset;
            pieces[pieceSquare] = currPiece.GetComponent<ManageChessPiece>();
            pieces[pieceSquare].Initialize(pieceSquare, capturePiece, this);
        }
        if (promotionPiece != Piece.none)
        {
            pieces[sourceSquare].SetPiece(movePiece);
        }
        if (isCastling)
        {
            if (isWhite)
            {
                if (targetSquare == 58)
                {
                    pieces[59].setPosition(56);
                }
                else
                {
                    pieces[61].setPosition(63);
                }
            }
            else
            {
                if (targetSquare == 2)
                {
                    pieces[3].setPosition(0);
                }
                else
                {
                    pieces[5].setPosition(7);
                }
            }
        }
        chessEngine.unMakeMove(lastMove);
        moves = chessEngine.GetCurrentLegalMoves();
        lastMove = default;
    }
    public void FinishedDraggingChessPiece(int startSquare,int endSquare)
    {
        clearCircles();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].Equals(default)) break;
            if (Move.GetSourceSquare(moves[i]) != startSquare || Move.GetTargetSquare(moves[i]) != endSquare) continue;
            makeMove(moves[i]);
            if (!isGameOver)
            {
                engineMakeMove();
            }
            //string content = "";
            //foreach(Move move in moves)
            //{
            //    string key = utils.convertBoardIndexToChessNotation(move.s1) + utils.convertBoardIndexToChessNotation(move.s2) + utils.pieceToString[move.promotionPiece];
            //    content += key + "\n";
            //}
            //string filePath = Path.Combine(Application.dataPath, "moves.txt");
            //File.WriteAllText(filePath, content);
            //Debug.Log("NUMBER OF MOVES " + moves.Length);
            return;
            
        }
        pieces[startSquare].setPosition(startSquare);
    }
    private void engineMakeMove()
    {
        uint bestMove = chessEngine.getBestMove(chessEngine.isWhiteMove);
        makeMove(bestMove);
        if (!isGameOver)
        {
            moves = chessEngine.GetCurrentLegalMoves();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
