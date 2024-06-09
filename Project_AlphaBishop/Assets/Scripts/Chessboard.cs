using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//using CrossPlatformInput;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}
public class Chessboard : MonoBehaviour
{
    public LayerMask tileLayerMask;

    [Header("Board Char")]
    [SerializeField] private Material tileMaterial; //initialize material to render tiles
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.6f;
    [SerializeField] private float deathSpacing = 0.8f;
    [SerializeField] private float dragOffset = 1.2f;

    [Header("UI")]
    [SerializeField] GameObject victoryScreen;
    [SerializeField] GameObject turnScreen;

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

    private bool isWhiteTurn;

    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

    private void Awake()
    {
        isWhiteTurn = true;
        turnScreen.SetActive(true);
        turnScreen.transform.GetChild(0).gameObject.SetActive(true);

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

        // handle mouse hover / check if there's at least one touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            Vector2 touchPosition = touch.position;
            //touchPosition.x /= Screen.width;
            //touchPosition.y /= Screen.height;
            //!!!!!Debug.Log($"Touch detected: {touch.phase} at position {touchPosition}");
            RaycastHit info;
            //Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // cast a ray from the camera to the mouse position to detect which tile is hovered
            Ray ray = currentCamera.ScreenPointToRay(touchPosition); // Cast a ray from the camera to the touch position to detect which tile is hovered
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red); // visualize the ray in the scene
            //!!!!Debug.Log($"Ray origin: {ray.origin}, direction: {ray.direction}");

            // Handle touch begin phase
            if (touch.phase == TouchPhase.Began)
            {
                //!!!!Debug.Log("Touch phase began");
                //!!!!Debug.Log($"LayerMask: {tileLayerMask.value}");

                if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Kill")))
                {
                    //!!!!Debug.Log($"Raycast hit: {info.transform.gameObject.name}");
                    // Get indexes of tile that have been hit
                    Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject);

                    // If we are hovering a tile for the first time
                    if (currentHover == -Vector2Int.one)
                    {
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }

                    // If we were already hovering a tile, change the previous one
                    if (currentHover != hitPosition)
                    {
                        tiles[currentHover.x, currentHover.y].layer = GetTileLayer(currentHover);
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }

                    // If there's a chess piece on the touched tile
                    if (chessPieces[hitPosition.x, hitPosition.y] != null)
                    {
                        // If it is our turn
                        if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn))
                        {
                            currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                            //Debug.Log($"Started dragging: {currentlyDragging.name}");

                            // Get a list of where the pieces can move by highlighting the tiles
                            availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                            // Get a list of special moves
                            specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                            PreventCheck();
                            HighlightTiles();
                        }
                    }
                }
                else
                {
                    //!!!!Debug.Log("XXXXX Raycast did not hit any objects XXXXX");
                    //!!!!Debug.Log($"Ray origin: {ray.origin}, direction: {ray.direction}");
                    //!!!!Debug.DrawRay(ray.origin, ray.direction * 100, Color.red); // visualize the ray in the scene


                }
            }

            // If we are releasing mouse button / Handle touch end phase
            if (touch.phase == TouchPhase.Ended)
            {
                //!!!!Debug.Log("Touch phase ended");

                //if (currentlyDragging != null && Input.GetMouseButtonUp(0))
                if (currentlyDragging != null)
                {
                    if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Kill")))
                    {
                        Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject);
                        Vector2Int previousPos = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                        bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y); // Check if move is valid
                        if (!validMove)
                        {
                            currentlyDragging.SetPosition(GetTileCenter(previousPos.x, previousPos.y), false);
                        }

                        //!!!!Debug.Log($"Move valid: {validMove}");

                        currentlyDragging = null;
                        RemoveHighlightTiles();
                    }

                    else
                    {
                        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), false);
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                    }
                }

                // Reset hover state
                if (currentHover != -Vector2Int.one)
                {
                    tiles[currentHover.x, currentHover.y].layer = GetTileLayer(currentHover);
                    currentHover = -Vector2Int.one;
                }
            }

            // Handle dragging phase
            if (currentlyDragging != null && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                float distance = 0.0f;
                if (horizontalPlane.Raycast(ray, out distance))
                {
                    currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
                }
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
            else if (i == availableMoves.Count - 1 && specialMove == SpecialMove.EnPassant)
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
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)))
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
                if (otherChessPiece.type == ChessPieceType.King)
                {
                    turnScreen.SetActive(false);
                    CheckMate(1);
                }

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
                if (otherChessPiece.type == ChessPieceType.King)
                {
                    turnScreen.SetActive(false);
                    CheckMate(0);
                }

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

        isWhiteTurn = !isWhiteTurn;
        if (isWhiteTurn)
        {
            turnScreen.transform.GetChild(1).gameObject.SetActive(false);
            turnScreen.transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            turnScreen.transform.GetChild(0).gameObject.SetActive(false);
            turnScreen.transform.GetChild(1).gameObject.SetActive(true);
        }

        moveList.Add(new Vector2Int[] { previousPos, new Vector2Int(x, y) });
        ProcessSpecialMove();

        return true;
    }

    private void ProcessSpecialMove()
    {
        GameObject displayMove = turnScreen.transform.GetChild(2).gameObject;
        if (specialMove == SpecialMove.EnPassant)
        {
            displayMove.SetActive(true);
            TextMeshProUGUI textComponent = displayMove.GetComponent<TextMeshProUGUI>();

            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPos = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPos[1].x, targetPawnPos[1].y];
            textComponent.text = "EnPassant move at " + GenerateTileName(myPawn.currentX, myPawn.currentY) + " for pawn at " + GenerateTileName(enemyPawn.currentX, enemyPawn.currentY);
            if (myPawn.currentX == enemyPawn.currentX)
            {
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if (enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(Vector3.up * 0.15f +
                            new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(Vector3.up * 0.15f + // for increasing y position because of the raised board edges
                            new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                            - bounds //to set center of board
                            + new Vector3(tileSize / 2, 0, tileSize / 2) //center of square
                            + (Vector3.back * deathSpacing) * deadBlacks.Count); //direction where it goes
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        } else if(specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            // left rook
            if (lastMove[1].x == 2)
            {
                // white side
                if (lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                } 
                // black side
                else if (lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            } else if (lastMove[1].x == 6)
            {
                // white side
                if (lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                // black side
                else if (lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        } else if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if(targetPawn.type == ChessPieceType.Pawn)
            {
                if(targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = SpawnSingleWhitePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
                if (targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = SpawnSingleBlackPiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }
        else
        {
            displayMove.SetActive(false);
        }
    }

    private void PreventCheck()
    {
        ChessPiece targetKing = null;
        for(int x = 0; x<TILE_COUNT_X; x++)
            for(int y = 0; y<TILE_COUNT_Y; y++)
                if (chessPieces[x,y].type == ChessPieceType.King)
                    if (chessPieces[x,y].team == currentlyDragging.team)
                        targetKing = chessPieces[x,y];
        // sending ref of available moves therefore deleting check moves
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        // Save the current values to reset after function call

        // Simulating and checking dangerous moves

        // Remove check moves from current available move list
    }

    private string GenerateTileName(int x, int y)
    {
        // Convert x and y to chess board notation
        char column = (char)('a' + x);
        int row = y + 1;
        return string.Format("{0}{1}", column, row);
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
        char column = (char)('a' + x);
        int row = y + 1;
        string tileName = string.Format("{0}{1}", column, row);
        GameObject tileObject = new GameObject(tileName);

        //GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
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
        tileObject.AddComponent<MeshCollider>();

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
        chessPieces[4, 0] = SpawnSingleWhitePiece(ChessPieceType.King, whiteTeam);
        chessPieces[3, 0] = SpawnSingleWhitePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[5, 0] = SpawnSingleWhitePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSingleWhitePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSingleWhitePiece(ChessPieceType.Rook, whiteTeam);

        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1] = SpawnSingleWhitePiece(ChessPieceType.Pawn, whiteTeam);

        //black team
        chessPieces[0, 7] = SpawnSingleBlackPiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSingleBlackPiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSingleBlackPiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[4, 7] = SpawnSingleBlackPiece(ChessPieceType.King, blackTeam);
        chessPieces[3, 7] = SpawnSingleBlackPiece(ChessPieceType.Queen, blackTeam);
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

    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }

    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void onResetButton()
    {
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);
                chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
        {
            Destroy(deadWhites[i].gameObject);
        }
        for (int i = 0; i < deadBlacks.Count; i++)
        {
            Destroy(deadBlacks[i].gameObject);
        }

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();

        isWhiteTurn = true;
    }

    public void onExitButton()
    {
        Application.Quit();
    }
}
