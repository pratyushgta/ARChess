using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Chessboard : MonoBehaviour
{
    [Header("Board Char")]
    [SerializeField] private Material tileMaterial; //initialize material to render tiles
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.6f;
    [SerializeField] private float deathSpacing = 0.8f;
    [SerializeField] private float dragOffset = 1.2f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] White_prefabs;
    [SerializeField] private GameObject[] Black_prefabs;
    //[SerializeField] private Material[] teamMaterials;

    // constants for amount of tiles on 8x8 chessboard
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;

    // 2D array to store each generated tile on board
    public GameObject[,] tiles;
    // reference to main camera
    private Camera currentCamera;
    // track the currently hovered tile position
    private Vector2Int currentHover;
    private Vector3 bounds;

    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>();

    private void Awake()
    {
        // begin board generation
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        //SpawnSingleWhitePiece(ChessPieceType.King, 0);
        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        // TEMPORARY CODE to handle mouse hover
        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // cast a ray from the camera to the mouse position to detect which tile is hovered
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Kill")))
        {
            // get indexes of tile that have been hit
            Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject);
            // if we are hovering a tile for the first time
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            //if we were already hovering a tile, change the previous one
            if (currentHover != hitPosition)
            {
                //tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                tiles[currentHover.x, currentHover.y].layer = GetTileLayer(currentHover);
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // if we press down on the mouse
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // if it is our turn
                    if (true)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        //get a list of where the pieces can move by highlighting the tiles
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        HighlightTiles();
                    }

                }
            }
            // if we are releasing the mouse button
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPos = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);


                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y); //check if move is valid
                if (!validMove)
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPos.x, previousPos.y), false);
                    //currentlyDragging.transform.position = GetTileCenter(previousPos.x, previousPos.y);
                }

                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                //tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                tiles[currentHover.x, currentHover.y].layer = GetTileLayer(currentHover);
                currentHover = -Vector2Int.one;
            }
            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), false);
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        // if we're dragging a piece
        if (currentlyDragging)
        {
            //create a new plane where we will cast the ray
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }

    // to highlight tiles where the piece can possibly move
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            if (chessPieces[availableMoves[i].x, availableMoves[i].y] != null && chessPieces[availableMoves[i].x, availableMoves[i].y].team != currentlyDragging.team)

                tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Kill");
            else
                tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

   
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    private int GetTileLayer(Vector2Int position)
    {
        if (ContainsValidMove(ref availableMoves, position))
        {
            if (chessPieces[position.x, position.y] != null && chessPieces[position.x, position.y].team != currentlyDragging.team)
            {
                return LayerMask.NameToLayer("Kill");
            }
            return LayerMask.NameToLayer("Highlight");
        }
        return LayerMask.NameToLayer("Tile");
    }


    private bool MoveTo(ChessPiece currentPiece, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            return false;
        }

        Vector2Int previousPos = new Vector2Int(currentPiece.currentX, currentPiece.currentY);

        // to handle another pice already on the target tile
        if (chessPieces[x, y] != null)
        {
            ChessPiece otherChessPiece = chessPieces[x, y];
            if (currentPiece.team == otherChessPiece.team)
            {
                return false;
            }
            // if moved over enemy piece
            if (otherChessPiece.team == 0)
            {
                deadWhites.Add(otherChessPiece);
                otherChessPiece.SetScale(Vector3.one * deathSize);
                otherChessPiece.SetPosition(Vector3.up * 0.15f + // for increasing y position because of the raised board edges
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds //to set center of board
                    + new Vector3(tileSize / 2, 0, tileSize / 2) //center of square
                    + (Vector3.forward * deathSpacing) * deadWhites.Count); //direction where it goes
            }
            else
            {
                deadBlacks.Add(otherChessPiece);
                otherChessPiece.SetScale(Vector3.one * deathSize);
                otherChessPiece.SetPosition(Vector3.up * 0.15f + // for increasing y position because of the raised board edges
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds //to set center of board
                    + new Vector3(tileSize / 2, 0, tileSize / 2) //center of square
                    + (Vector3.back * deathSpacing) * deadBlacks.Count); //direction where it goes
            }
        }

        chessPieces[x, y] = currentPiece;
        chessPieces[previousPos.x, previousPos.y] = null;

        PositionSinglePiece(x, y);
        return true;
    }

    //generate board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int i = 0; i < tileCountX; i++)
        {
            for (int j = 0; j < tileCountY; j++)
            {
                tiles[i, j] = GenerateSingleTile(tileSize, i, j);
            }
        }
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        //to render a triangle
        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] triangle = new[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangle;
        //recalculate normarls for lighting
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // spawning of pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        //white team
        chessPieces[0, 0] = SpawnSingleWhitePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSingleWhitePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSingleWhitePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSingleWhitePiece(ChessPieceType.King, whiteTeam);
        chessPieces[4, 0] = SpawnSingleWhitePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[5, 0] = SpawnSingleWhitePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSingleWhitePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSingleWhitePiece(ChessPieceType.Rook, whiteTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1] = SpawnSingleWhitePiece(ChessPieceType.Pawn, whiteTeam);

        //black team
        chessPieces[0, 7] = SpawnSingleBlackPiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSingleBlackPiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSingleBlackPiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSingleBlackPiece(ChessPieceType.King, blackTeam);
        chessPieces[4, 7] = SpawnSingleBlackPiece(ChessPieceType.Queen, blackTeam);
        chessPieces[5, 7] = SpawnSingleBlackPiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSingleBlackPiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSingleBlackPiece(ChessPieceType.Rook, blackTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 6] = SpawnSingleBlackPiece(ChessPieceType.Pawn, blackTeam);
    }

    private ChessPiece SpawnSingleWhitePiece(ChessPieceType type, int team)
    {
        ChessPiece piece = Instantiate(White_prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        piece.type = type;
        piece.team = team;
        return piece;
    }

    private ChessPiece SpawnSingleBlackPiece(ChessPieceType type, int team)
    {
        ChessPiece piece = Instantiate(Black_prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        piece.type = type;
        piece.team = team;
        return piece;
    }

    //positioning the pieces
    private void PositionAllPieces()
    {
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (chessPieces[i, j] != null)
                    PositionSinglePiece(i, j, true);
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false) // force positions smoothly(F) or instatly (T)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), false);
        //chessPieces[x,y].transform.position = GetTileCenter(x,y);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    // operations/ helper function
    private Vector2Int LookUpTileIndex(GameObject hitInfo)
    {
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Y; j++)
            {
                if (tiles[i, j] == hitInfo)
                    return new Vector2Int(i, j);
            }
        }
        return -Vector2Int.one;
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y) return true;
        }
        return false;
    }
}
