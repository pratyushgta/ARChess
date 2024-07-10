using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
            {
                arr.Add(new Vector2Int(currentX - 1, currentY + direction));
            }


        return arr;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;

        // promotion
        // check if we're at the end
        if((team == 0 && currentY == 6) || (team == 1 && currentY == 1))
        {
            return SpecialMove.Promotion;
        }

        // Handling En-Passant
        if (moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            // if last move was a pawn
            if (board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn)
            {
                // if last move was +2 pawn irrespective of team
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2)
                {
                    if (board[lastMove[1].x, lastMove[1].y].team != team)
                    {
                        if (lastMove[1].y == currentY) // if both pawn are on the same y
                        {
                            if (lastMove[1].x == currentX - 1) // landed left
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                            else if (lastMove[1].x == currentX + 1) // landed right
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                        }
                    }
                }
            }
        }
        return SpecialMove.None;
    }

}
