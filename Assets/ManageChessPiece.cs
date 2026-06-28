using EngineCore;
using System;
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
    private Action<int> startMove;
    private Action<int,int> endMove;
    private int boardIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Initialize(int boardIndex, int piece, Action<int> startMove, Action<int,int> endMove)
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        this.boardIndex = boardIndex;
        setPosition(boardIndex);
        SetPiece(piece);
        this.startMove = startMove;
        this.endMove = endMove;
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
        startDragAnchoredPosition = rectTransform.anchoredPosition;
        startMove?.Invoke(boardIndex);
        transform.SetAsLastSibling();
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        endMove?.Invoke(boardIndex,calculateBoardIndex());
    }
    private void OnDestroy()
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position-new Vector2(rectTransform.rect.width/4,-rectTransform.rect.height/4);
    }
}
