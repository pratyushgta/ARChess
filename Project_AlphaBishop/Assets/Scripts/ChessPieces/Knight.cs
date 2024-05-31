using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* RULES:
 * Top right: X +1 , Y +2
 * Top left: X -1, Y +2
 * Bottom right: X +1, Y -2
 * Bottom left: X -1, Y -2
 * Rt Side right: X +2, Y -1
 * Rt Side left: X +2, Y +1
 * Lf Side right: X -2, Y +1
 * Lf Side left: X -2, Y -1
 */
public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> arr = new List<Vector2Int>();

        // top right
        int x = currentX + 1;
        int y = currentY + 2;
        if(x<tileCountX && y < tileCountY)
        {
            if (board[x,y] == null || board[x,y].team != team)
            {
                arr.Add(new Vector2Int(x,y));
            }
        }
        //top left
        x = currentX - 1;
        y = currentY + 2;
        if (x >= 0 && y < tileCountY)
        {
            if (board[x, y] == null || board[x, y].team != team)
            {
                arr.Add(new Vector2Int(x, y));
            }
        }
        //bottom right
        x = currentX + 1;
        y = currentY - 2;
        if (x < tileCountX && y >= 0)
        {
            if (board[x, y] == null || board[x, y].team != team)
            {
                arr.Add(new Vector2Int(x, y));
            }
        }
        //bottom left
        x = currentX - 1;
        y = currentY - 2;
        if (x >= 0 && y >= 0)
        {
            if (board[x, y] == null || board[x, y].team != team)
            {
                arr.Add(new Vector2Int(x, y));
            }
        }
        // right side right
        x = currentX + 2;
        y = currentY - 1;
        if (x < tileCountX && y >= 0)
        {
            if (board[x, y] == null || board[x, y].team != team)
            {
                arr.Add(new Vector2Int(x, y));
            }
        }
        // right side left
        x = currentX + 2;
        y = currentY + 1;
        if (x < tileCountX && y < tileCountY)
        {
            if (board[x, y] == null || board[x, y].team != team)
            {
                arr.Add(new Vector2Int(x, y));
            }
        }
        // left side right
        x = currentX - 2;
        y = currentY + 1;
        if (x >=0 && y < tileCountY)
        {
            if (board[x, y] == null || board[x, y].team != team)
            {
                arr.Add(new Vector2Int(x, y));
            }
        }
        // left side left
        x = currentX - 2;
        y = currentY - 1;
        if (x >= 0 && y >= 0)
        {
            if (board[x, y] == null || board[x, y].team != team)
            {
                arr.Add(new Vector2Int(x, y));
            }
        }
      
        return arr;
    }
}
