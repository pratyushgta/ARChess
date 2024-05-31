using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> arr = new List<Vector2Int>();

        //right
        if (currentX + 1 < tileCountX)
        {
            // right
            if (board[currentX + 1, currentY] == null)
            {
                arr.Add(new Vector2Int(currentX + 1, currentY));
            }
            else if (board[currentX + 1, currentY].team != team)
            {
                arr.Add(new Vector2Int(currentX + 1, currentY));
            }
            //top right
            if (currentY + 1 < tileCountY)
            {
                if (board[currentX + 1, currentY + 1] == null)
                {
                    arr.Add(new Vector2Int(currentX + 1, currentY + 1));
                }
                else if (board[currentX + 1, currentY + 1].team != team)
                {
                    arr.Add(new Vector2Int(currentX + 1, currentY + 1));
                }
            }
            //bottom right
            if (currentY - 1 >= 0)
            {
                if (board[currentX + 1, currentY - 1] == null)
                {
                    arr.Add(new Vector2Int(currentX + 1, currentY - 1));
                }
                else if (board[currentX + 1, currentY - 1].team != team)
                {
                    arr.Add(new Vector2Int(currentX + 1, currentY - 1));
                }
            }
        }

        //left
        //right
        if (currentX - 1 >= 0)
        {
            // left
            if (board[currentX - 1, currentY] == null)
            {
                arr.Add(new Vector2Int(currentX - 1, currentY));
            }
            else if (board[currentX - 1, currentY].team != team)
            {
                arr.Add(new Vector2Int(currentX - 1, currentY));
            }
            //top right
            if (currentY + 1 < tileCountY)
            {
                if (board[currentX - 1, currentY + 1] == null)
                {
                    arr.Add(new Vector2Int(currentX - 1, currentY + 1));
                }
                else if (board[currentX - 1, currentY + 1].team != team)
                {
                    arr.Add(new Vector2Int(currentX - 1, currentY + 1));
                }
            }
            //bottom right
            if (currentY - 1 >= 0)
            {
                if (board[currentX - 1, currentY - 1] == null)
                {
                    arr.Add(new Vector2Int(currentX - 1, currentY - 1));
                }
                else if (board[currentX - 1, currentY - 1].team != team)
                {
                    arr.Add(new Vector2Int(currentX - 1, currentY - 1));
                }
            }
        }

        // up
        if (currentY + 1 < tileCountY)
        {
            if (board[currentX, currentX + 1] == null || board[currentX, currentY + 1].team != team)
            {
                arr.Add(new Vector2Int(currentX, currentY + 1));
            }
        }

        // down
        if (currentY - 1 >= 0)
        {
            if (board[currentX, currentY - 1] == null || board[currentX, currentY - 1].team != team)
            {
                arr.Add(new Vector2Int(currentX, currentY - 1));
            }
        }

        return arr;
    }
}
