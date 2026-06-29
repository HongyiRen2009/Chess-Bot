using EngineCore;
using System.Collections.Generic;
using UnityEngine;
public class Search
{
    [SerializeField] private MoveGenerator moveGenerator;
    [SerializeField] private Board board;
    public Search(Board board, MoveGenerator moveGenerator)
    {
        this.moveGenerator = moveGenerator;
        this.board = board;
    }
}
