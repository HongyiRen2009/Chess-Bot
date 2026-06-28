using EngineCore;
using System;
using System.Collections.Generic;
using System.IO;
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
    private Action<int> startMove;
    private Action<int,int> endMove;
    private Move[] moves;
    private ManageChessPiece[] pieces = new ManageChessPiece[64];
    private Move lastMove;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startMove += AddCircles;
        endMove += FinishedDraggingChessPiece;
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
                pieces[pieceSquare].Initialize(pieceSquare, piece,startMove,endMove);

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
            if (moves[i] == null) break;
            if (moves[i].s1 == startSquare)
            {
                GameObject currBoardCircle = Instantiate(boardCircles, boardCirclesTransform);
                currBoardCircle.GetComponent<RectTransform>().anchoredPosition = new Vector2((moves[i].s2 % 8) * 133, Mathf.Floor(moves[i].s2 / 8) * -133);
                currBoardCircle.GetComponent<Image>().sprite = moves[i].capturePiece == Piece.none || moves[i].isEnPassent ? circle : hollowCircle;
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
        foreach (Move move in moves)
        {
            content += move.s1 + " " + move.s2 + utils.pieceToString[move.promotionPiece] +"\n";
        }
        File.WriteAllText(filePath, content);
    }
    public void unMakeLastMove()
    {
        if (lastMove == null) return;

        pieces[lastMove.s1] = pieces[lastMove.s2];
        pieces[lastMove.s2] = null;
        pieces[lastMove.s1].setPosition(lastMove.s1);
        if (lastMove.capturePiece != Piece.none)
        {
            GameObject currPiece = Instantiate(chessPiece, lastMove.capturePiece < 6 ? whiteBoardPiecesTransform : blackBoardPiecesTransform);
            int EnPassentOffset = 0;
            if (lastMove.isEnPassent) EnPassentOffset = +(lastMove.isWhite ? 8 : -8);
            int pieceSquare = lastMove.s2 + EnPassentOffset;
            pieces[pieceSquare] = currPiece.GetComponent<ManageChessPiece>();
            pieces[pieceSquare].Initialize(pieceSquare, lastMove.capturePiece, startMove, endMove);
        }
        if (lastMove.promotionPiece != Piece.none)
        {
            pieces[lastMove.s1].SetPiece(lastMove.movePiece);
        }
        if (lastMove.isCastling)
        {
            if (lastMove.isWhite)
            {
                if (lastMove.s2 == 58)
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
                if (lastMove.s2 == 2)
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
        lastMove = null;
    }
    public void FinishedDraggingChessPiece(int startSquare,int endSquare)
    {
        clearCircles();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i] == null) break;
            if (moves[i].s1 != startSquare || moves[i].s2 != endSquare) continue;
            lastMove = moves[i];
            if (pieces[endSquare] != null)
            {
                Destroy(pieces[endSquare].gameObject);
            }
            pieces[endSquare] = pieces[startSquare];
            pieces[startSquare] = null;
            if (moves[i].promotionPiece != Piece.none)
            {
                pieces[endSquare].SetPiece(moves[i].promotionPiece);
            }
            if (moves[i].isCastling)
            {
                if (moves[i].isWhite)
                {
                    if (endSquare == 58)
                    {
                        pieces[56].setPosition(59);
                    }
                    else
                    {
                        pieces[63].setPosition(61);
                    }
                }
                else
                {
                    if (endSquare == 2)
                    {
                        pieces[0].setPosition(3);
                    }
                    else
                    {
                        pieces[7].setPosition(5);
                    }
                }
            }
            if (moves[i].isEnPassent)
            {
                Destroy(pieces[endSquare+(moves[i].isWhite ? 8:-8)].gameObject);
            }
            pieces[endSquare].setPosition(endSquare);
            chessEngine.makeMove(moves[i]);
            moves = chessEngine.GetCurrentLegalMoves();
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
