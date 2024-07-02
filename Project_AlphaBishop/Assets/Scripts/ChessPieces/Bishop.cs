using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> arr = new List<Vector2Int>();
        //top diagonal right
        for (int x = currentX + 1, y = currentY + 1; x < tileCountX && y < tileCountY; x++, y++)
        {
            if (board[x, y] == null)
            {
                arr.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    arr.Add(new Vector2Int(x, y));
                }
                break;

            }
        }

        //top diagonal left
        for (int x = currentX - 1, y = currentY + 1; x >= 0 && y < tileCountY; x--, y++)
        {
            if (board[x, y] == null)
            {
                arr.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    arr.Add(new Vector2Int(x, y));
                }
                break;

            }
        }

        //bottom diagonal right
        for (int x = currentX + 1, y = currentY - 1; x < tileCountX && y >= 0; x++, y--)
        {
            if (board[x, y] == null)
            {
                arr.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    arr.Add(new Vector2Int(x, y));
                }
                break;

            }
        }

        //bottom diagonal left
        for (int x = currentX - 1, y = currentY - 1; x >= 0 && y >= 0; x--, y--)
        {
            if (board[x, y] == null)
            {
                arr.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    arr.Add(new Vector2Int(x, y));
                }
                break;

            }
        }

        return arr;
    }
}