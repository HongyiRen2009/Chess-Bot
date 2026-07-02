using EngineCore;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ManageChessPiece : MonoBehaviour, IBeginDragHandler,IDragHandler, IEndDragHandler
{
    private Image image;
    [SerializeField] private Sprite[] chessPieces;
    private int piece;
    private Vector2 startDragAnchoredPosition;
    private RectTransform rectTransform;
    private int boardIndex;
    private ManageBoard manageBoard;
    private Canvas canvas;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas = GameObject.FindAnyObjectByType<Canvas>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Initialize(int boardIndex, int piece, ManageBoard manageBoard)
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        this.boardIndex = boardIndex;
        setPosition(boardIndex);
        SetPiece(piece);
        this.manageBoard = manageBoard;
    }
    public void setPosition(int boardIndex)
    {
        this.boardIndex = boardIndex;
        rectTransform.anchoredPosition = new Vector2((boardIndex % 8) * 133, Mathf.Floor(boardIndex / 8) * -133); // Flipped to positive y is down

    }
    private int calculateBoardIndex()
    {
        return Mathf.RoundToInt(-rectTransform.anchoredPosition.y / 133) * 8 + Mathf.RoundToInt(rectTransform.anchoredPosition.x / 133);
    }
    public void SetPiece(int piece)
    {
        this.piece = piece;
        image.sprite=chessPieces[piece];
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (manageBoard.isGameOver)
        { 
            eventData.pointerDrag = null;
            return; 
        }
        startDragAnchoredPosition = rectTransform.anchoredPosition;
        manageBoard.AddCircles(boardIndex);
        transform.SetAsLastSibling();
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        manageBoard.FinishedDraggingChessPiece(boardIndex, calculateBoardIndex());
    }
    private void OnDestroy()
    {
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
float scaleFactor = canvas.scaleFactor;

        rectTransform.position = eventData.position+new Vector2(-rectTransform.rect.width, rectTransform.rect.height)/2*scaleFactor;
    }
}
