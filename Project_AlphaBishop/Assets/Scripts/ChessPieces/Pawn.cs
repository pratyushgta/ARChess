using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*RULES:
1. 2 Tiles Up if starting move
2. Diagonal move if enemy is diagonal
3. SPECIAL MOVE:
*/
public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> arr = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        // One in front
        if (board[currentX, currentY + direction] == null)
            arr.Add(new Vector2Int(currentX, currentY + direction));

        // two in front
        if (board[currentX, currentY + direction] == null)
        {
            if (team == 0 && currentY == 1 && board[currentX, currentY + direction * 2] == null)
            {
                arr.Add(new Vector2Int(currentX, currentY + direction * 2));
            }
            if (team == 1 && currentY == 6 && board[currentX, currentY + direction * 2] == null)
            {
                arr.Add(new Vector2Int(currentX, currentY + direction * 2));
            }
        }

        // diagonal kill move
        if (currentX != tileCountX - 1)
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
            {
                arr.Add(new Vector2Int(currentX + 1, currentY + direction));
            }
        if (currentX != 0)
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team) { 
                arr.Add(new Vector2Int(currentX - 1, currentY + direction));
                }


        return arr;
    }

}
