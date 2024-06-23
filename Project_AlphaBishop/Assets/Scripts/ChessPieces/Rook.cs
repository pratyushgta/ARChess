using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Rook : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> arr = new List<Vector2Int>();

        // down direction
        for (int i = currentY - 1; i >= 0; i--)
        {
            if (board[currentX, i] == null)
            {
                arr.Add(new Vector2Int(currentX, i));
            }

            if (board[currentX, i] != null)
            {
                if (board[currentX, i].team != team)
                {
                    arr.Add(new Vector2Int(currentX, i));
                }
                break;
            }
        }

        // up direction
        for (int i = currentY + 1; i < tileCountY; i++)
        {
            if (board[currentX, i] == null)
            {
                arr.Add(new Vector2Int(currentX, i));
            }

            if (board[currentX, i] != null)
            {
                if (board[currentX, i].team != team)
                {
                    arr.Add(new Vector2Int(currentX, i));
                }
                break;
            }
        }

        // left direction
        for (int i = currentX - 1; i >= 0; i--)
        {
            if (board[i,currentY] == null)
            {
                arr.Add(new Vector2Int(i,currentY));
            }

            if (board[i, currentY] != null)
            {
                if (board[i,currentY].team != team)
                {
                    arr.Add(new Vector2Int(i,currentY));
                }
                break;
            }
        }

        // right direction
        for (int i = currentX + 1; i < tileCountX; i++)
        {
            if (board[i, currentY] == null)
            {
                arr.Add(new Vector2Int(i, currentY));
            }

            if (board[i, currentY] != null)
            {
                if (board[i, currentY].team != team)
                {
                    arr.Add(new Vector2Int(i, currentY));
                }
                break;
            }
        }

        return arr;

    }
}