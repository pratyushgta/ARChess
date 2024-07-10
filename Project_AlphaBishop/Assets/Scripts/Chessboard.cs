using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using TMPro;
using System.Collections.Generic;
//using static UnityEngine.InputSystem.HID.HID;
using UnityEngine.UIElements;
using UnityEngine.Rendering.Universal;
using Button = UnityEngine.UI.Button;
using System.Collections;
using UnityEngine.Timeline;
using static Chessboard;
using System.Security.Cryptography;
using static UnityEngine.Rendering.DebugUI;
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
    [SerializeField] private float yOffsetX = 0.2f;
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

    [Header("Sound")]
    [SerializeField] public AudioSource game_start_sound;
    [SerializeField] public AudioSource game_end_sound;
    [SerializeField] public AudioSource piece_pick_sound;
    [SerializeField] public AudioSource piece_drop_sound;
    [SerializeField] public AudioSource castling_sound;
    [SerializeField] public AudioSource promotion_sound;
    [SerializeField] public AudioSource capture_sound;
    [SerializeField] public AudioSource check_sound;


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

    private GameObject displaySpecialMove;
    private GameObject displayMove;
    private TextMeshProUGUI displaySpecialMove_Text;
    private TextMeshProUGUI displayMove_Text;

    private int missCounter = 0;

    private GameObject promotionMenu;
    private bool inputTimeOut = false;

    private GameObject GameModeMenu;
    private bool AutoOpponent = false;
    //private Camera overheadCamera;

    private void Awake()
    {
        game_start_sound.Play();

        isWhiteTurn = true;
        turnScreen.SetActive(true);
        turnScreen.transform.GetChild(0).gameObject.SetActive(true);

        displaySpecialMove = turnScreen.transform.GetChild(2).gameObject;
        displayMove = turnScreen.transform.GetChild(3).gameObject;
        promotionMenu = turnScreen.transform.GetChild(4).gameObject;
        GameModeMenu = turnScreen.transform.GetChild(5).gameObject;
        promotionMenu.SetActive(false);

        displaySpecialMove_Text = displaySpecialMove.GetComponent<TextMeshProUGUI>();
        displayMove_Text = displayMove.GetComponent<TextMeshProUGUI>();

        displayMove.SetActive(true);
        displayMove_Text.text = " ";

        GameModeMenu.SetActive(true);
        inputTimeOut = true;

        // begin board generation
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
        //currentCamera = Camera.main;
        //CreateOverheadCamera();

        //chessPieces[7,7].SetPosition(GetTileCenter(5,5), false);

    }


    // Camera directly on top of spawned chessboard to throw raycast for better piece moving accuracy
    /*private void CreateOverheadCamera()
    {
        GameObject cameraObject = new GameObject("OverheadCamera");
        overheadCamera = cameraObject.AddComponent<Camera>();
        overheadCamera.orthographic = true;
        overheadCamera.orthographicSize = (TILE_COUNT_X * tileSize) / 2;
        overheadCamera.transform.position = new Vector3(transform.position.x, 20f, transform.position.z);
        overheadCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        overheadCamera.enabled = false; 
    }*/


    private void Update()
    {
        Debug.Log("U Input: " + inputTimeOut + " Auto: " + AutoOpponent);
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        /*if (!overheadCamera)
        {
            CreateOverheadCamera();
            return;
        }*/

        if (victoryScreen.activeSelf || GameModeMenu.activeSelf)
        {
            inputTimeOut = true;
            AutoOpponent = false;
        }

        // handle mouse hover / check if there's at least one touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            Vector2 touchPosition = touch.position;
            RaycastHit info;
            //Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // WINDOWS cast a ray from the camera to the mouse position to detect which tile is hovered
            Ray ray = currentCamera.ScreenPointToRay(touchPosition); // ANDROID Cast a ray from the camera to the touch position to detect which tile is hovered
            //Ray ray = overheadCamera.ScreenPointToRay(touchPosition); // ANDROID Cast a ray from the overhead camera to the touch position to detect which tile is hovered
            if (!isWhiteTurn && AutoOpponent)
            {
                inputTimeOut = true;
            }
            // Handle touch begin phase
            if (touch.phase == TouchPhase.Began && inputTimeOut == false)
            {
                if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Kill", "SpecialMove")))
                {
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
                        piece_pick_sound.Play();

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
            }

            // If we are releasing mouse button / Handle touch end phase
            if (touch.phase == TouchPhase.Ended)
            {
                //if (currentlyDragging != null && Input.GetMouseButtonUp(0))
                if (currentlyDragging != null)
                {
                    piece_drop_sound.Play();
                    if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Kill", "SpecialMove")))
                    {
                        Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject);
                        Vector2Int previousPos = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                        bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y); // Check if move is valid
                        if (!validMove)
                        {
                            currentlyDragging.SetPosition(GetTileCenter(previousPos.x, previousPos.y), false);
                            missCounter++;
                        }

                        if (missCounter > 2)
                        {
                            displaySpecialMove.SetActive(true);
                            displaySpecialMove_Text.text = "Drag your finger directly on top of a highlighted tile to move the piece!";
                            missCounter = 0;
                        }
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                        if (!isWhiteTurn && AutoOpponent)
                        {
                            // StartCoroutine(RandomMoveBlack());
                            //Calculate_BestBlackMove();
                            StartCoroutine(Calculate_BestBlackMove());

                        }
                    }
                    else
                    {
                        missCounter++;
                        if (missCounter > 2)
                        {
                            displaySpecialMove.SetActive(true);
                            displaySpecialMove_Text.text = "Drag your finger directly on top of a highlighted tile to move the piece!";
                            missCounter = 0;
                        }
                        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), false);
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                    }
                }

                // Reset hover state
                if (currentHover != -Vector2Int.one)
                {
                    try
                    {
                        tiles[currentHover.x, currentHover.y].layer = GetTileLayer(currentHover);
                        currentHover = -Vector2Int.one;
                    }
                    catch (Exception e)
                    {
                        RemoveHighlightTiles();
                    }
                }
            }

            // Handle dragging phase
            if (currentlyDragging != null && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffsetX);
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
                tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("SpecialMove");
            /*
            else if(i == availableMoves.Count - 1 && specialMove == SpecialMove.Promotion)
                tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("SpecialMove");
            /*
            else if (i == availableMoves.Count - 1 && specialMove == SpecialMove.Castling)
            {
                tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("SpecialMove");
                tiles[availableMoves[i-1].x, availableMoves[i-1].y].layer = LayerMask.NameToLayer("SpecialMove");
            }
            */
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
        displaySpecialMove.SetActive(false);
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)))
        {
            //!!!!Debug.Log("FALSE 1 for Piece: " + currentPiece + " X: " + x + ", " + y);
            return false;
        }

        Vector2Int previousPos = new Vector2Int(currentPiece.currentX, currentPiece.currentY);

        // to handle another pice already on the target tile
        if (chessPieces[x, y] != null)
        {
            ChessPiece otherChessPiece = chessPieces[x, y];
            if (currentPiece.team == otherChessPiece.team)
            {
                //!!!!Debug.Log("FALSE 1 for Piece: " + currentPiece + " X: " + x + ", " + y);
                return false;
            }
            // if moved over enemy piece
            if (otherChessPiece.team == 0)
            {
                capture_sound.Play();

                if (otherChessPiece.type == ChessPieceType.King)
                {
                    turnScreen.SetActive(false);
                    CheckMate(1);
                }
                displaySpecialMove.SetActive(true);
                displaySpecialMove_Text.text = "b: " + currentPiece.type + " " + GenerateTileName(previousPos.x, previousPos.y) + " captured " + " w: " + otherChessPiece.type + " " + GenerateTileName(otherChessPiece.currentX, otherChessPiece.currentY);
                deadWhites.Add(otherChessPiece);
                otherChessPiece.SetScale(Vector3.one * deathSize);
                otherChessPiece.SetPosition(Vector3.up * 0.015f + // for increasing y position because of the raised board edges
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds //to set center of board
                    + new Vector3(tileSize / 2, 0, tileSize / 2) //center of square
                    + transform.position
                    + (Vector3.forward * deathSpacing) * deadWhites.Count); //direction where it goes
            }
            else
            {
                capture_sound.Play();

                if (otherChessPiece.type == ChessPieceType.King)
                {
                    turnScreen.SetActive(false);
                    CheckMate(0);
                }
                displaySpecialMove.SetActive(true);
                displaySpecialMove_Text.text = "w: " + currentPiece.type + " " + GenerateTileName(previousPos.x, previousPos.y) + " captured " + " b: " + otherChessPiece.type + " " + GenerateTileName(otherChessPiece.currentX, otherChessPiece.currentY);
                deadBlacks.Add(otherChessPiece);
                otherChessPiece.SetScale(Vector3.one * deathSize);
                otherChessPiece.SetPosition(Vector3.up * 0.015f + // for increasing y position because of the raised board edges (set 0.15 for board scale 1)
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds //to set center of board
                    + new Vector3(tileSize / 2, 0, tileSize / 2) //center of square
                    + transform.position
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

        String team = currentPiece.team == 0 ? "w" : "b";
        displayMove_Text.text = team + ": " + GenerateTileName(previousPos.x, previousPos.y) + " > " + GenerateTileName(x, y);
        ProcessSpecialMove();
        switch (CheckForCheckmate())
        {
            default:
                break;
            case 1:
                Debug.Log("Checkmate");
                CheckMate(currentPiece.team);
                break;
            case 2:
                Debug.Log("Stalemate");
                CheckMate(2);
                break;
        }
        return true;
    }

    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            displaySpecialMove.SetActive(true);

            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPos = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPos[1].x, targetPawnPos[1].y];

            if (myPawn.currentX == enemyPawn.currentX)
            {
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    capture_sound.Play();

                    if (enemyPawn.team == 0)
                    {
                        displaySpecialMove_Text.text = "b: EnPassant by " + GenerateTileName(myPawn.currentX, myPawn.currentY) + " for " + GenerateTileName(enemyPawn.currentX, enemyPawn.currentY);
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(Vector3.up * 0.015f +
                            new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + transform.position
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        capture_sound.Play();

                        displaySpecialMove_Text.text = "w: EnPassant by " + GenerateTileName(myPawn.currentX, myPawn.currentY) + " for " + GenerateTileName(enemyPawn.currentX, enemyPawn.currentY);
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(Vector3.up * 0.015f + // for increasing y position because of the raised board edges
                            new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                            - bounds //to set center of board
                            + new Vector3(tileSize / 2, 0, tileSize / 2) //center of square
                            + transform.position
                            + (Vector3.back * deathSpacing) * deadBlacks.Count); //direction where it goes
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }
        else if (specialMove == SpecialMove.Castling)
        {
            displaySpecialMove.SetActive(true);

            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            // left rook
            if (lastMove[1].x == 2)
            {
                castling_sound.Play();
                // white side
                if (lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                    displaySpecialMove_Text.text = "w: Left Castling";
                }
                // black side
                else if (lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                    displaySpecialMove_Text.text = "b: Left Castling";
                }
            }
            else if (lastMove[1].x == 6)
            {
                castling_sound.Play();
                // white side
                if (lastMove[1].y == 0)
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                    displaySpecialMove_Text.text = "w: Right Castling";
                }
                // black side
                else if (lastMove[1].y == 7)
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                    displaySpecialMove_Text.text = "b: Right Castling";
                }
            }
        }
        else if (specialMove == SpecialMove.Promotion)
        {
            Debug.Log("SPECIAL ACTIVE");
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];
            if (AutoOpponent && targetPawn.team == 1 && lastMove[1].y == 0)
            {
                Debug.Log("S1");
                if (targetPawn.type == ChessPieceType.Pawn)
                {
                    Debug.Log("S2");
                    List<ChessPieceType> blackPieces = new List<ChessPieceType>();
                    blackPieces.Add(ChessPieceType.Queen);
                    blackPieces.Add(ChessPieceType.Bishop);
                    blackPieces.Add(ChessPieceType.Rook);
                    blackPieces.Add(ChessPieceType.Knight);

                    if (blackPieces.Count > 0)
                    {
                        Debug.Log("S3");
                        Shuffle(blackPieces);
                        handlePawnPromotion(blackPieces[0]);
                    }
                    else
                    {
                        Debug.Log("S4");
                        handlePawnPromotion(ChessPieceType.Queen);
                    }

                }
            }
            else
            {
                Debug.Log("S5");
                promotionMenu.SetActive(true);
                displaySpecialMove.SetActive(true);
                inputTimeOut = true;
            }
            /*
            displaySpecialMove.SetActive(true);

            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if (targetPawn.type == ChessPieceType.Pawn)
            {
                if (targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    promotion_sound.Play();
                    displaySpecialMove_Text.text = "w: " + GenerateTileName(targetPawn.currentX, targetPawn.currentY) + " promoted to Queen";
                    
                    ChessPiece newQueen = SpawnSingleWhitePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                    
                }
                if (targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    promotion_sound.Play();
                    displaySpecialMove_Text.text = "b: " + GenerateTileName(targetPawn.currentX, targetPawn.currentY) + " promoted to Queen"; 
                    
                    ChessPiece newQueen = SpawnSingleBlackPiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                    
                }
            }
            */
        }
    }

    public void handlePawnPromotion(ChessPieceType replacement)
    {
        inputTimeOut = false;
        promotionMenu.SetActive(false);
        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        if (targetPawn.type == ChessPieceType.Pawn)
        {
            if (targetPawn.team == 0 && lastMove[1].y == 7)
            {
                promotion_sound.Play();
                displaySpecialMove_Text.text = "w: " + GenerateTileName(targetPawn.currentX, targetPawn.currentY) + " promoted to " + replacement;

                ChessPiece newPiece = SpawnSingleWhitePiece(replacement, 0);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);

            }
            if (targetPawn.team == 1 && lastMove[1].y == 0)
            {
                promotion_sound.Play();
                displaySpecialMove_Text.text = "b: " + GenerateTileName(targetPawn.currentX, targetPawn.currentY) + " promoted to " + replacement;

                ChessPiece newPiece = SpawnSingleBlackPiece(replacement, 1);
                newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);

            }
        }
    }


    // check for checkmate before a move
    private void PreventCheck()
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    if (chessPieces[x, y].type == ChessPieceType.King)
                        if (chessPieces[x, y].team == currentlyDragging.team)
                            targetKing = chessPieces[x, y];
        // sending ref of available moves to delete check moves
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }

    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        // Save the current values to reset after function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // Simulating and checking dangerous moves
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            // if we simulate king's move
            if (cp.type == ChessPieceType.King)
                kingPositionThisSim = new Vector2Int(simX, simY);

            // copy 2D array and not ref
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            // list of chesspieces that can attack next round
            List<ChessPiece> simulateAttackingPieces = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if (simulation[x, y].team != cp.team)
                            simulateAttackingPieces.Add(simulation[x, y]);
                    }
                }
            }

            // simulate that move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            // did one of the piece got taken down during simulation
            var deadPiece = simulateAttackingPieces.Find(chessPc => chessPc.currentX == simX && chessPc.currentY == simY);
            if (deadPiece != null)
                simulateAttackingPieces.Remove(deadPiece);
            // Get all simulated attacking pieces move
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simulateAttackingPieces.Count; a++)
            {
                var pieceMoves = simulateAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                    simMoves.Add(pieceMoves[b]);
            }

            // remove move if king in trouble
            if (ContainsValidMove(ref simMoves, kingPositionThisSim))
                movesToRemove.Add(moves[i]);

            // restore actual CP data
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        if (movesToRemove.Count > 0)
        {
            displaySpecialMove.SetActive(true);
            displaySpecialMove_Text.text = "Check prevention active";
            check_sound.Play();
        }

        // Remove check moves from current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
            moves.Remove(movesToRemove[i]);
    }

    // checking for checkmate after a move 
    private int CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }

                }
        // is the king attacked right now
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int b = 0; b < pieceMoves.Count; b++)
                currentAvailableMoves.Add(pieceMoves[b]);
        }
        // if we are in check right now. PLAY CHECKMATE SOUND HERE
        bool displayWin = false;
        try
        {
            if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
            {
                // king is under attack, can we move something to help king
                for (int i = 0; i < defendingPieces.Count; i++)
                {
                    // moves allowed to do
                    List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);
                    if (defendingMoves.Count != 0)
                        return 0;
                }
                displayWin = true;
                return 1; // checkmate exit
            }
            // to handle stalemate condition
            else
            {
                // king is under attack, can we move something to help king
                for (int i = 0; i < defendingPieces.Count; i++)
                {
                    // moves allowed to do
                    List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);
                    if (defendingMoves.Count != 0)
                        return 0;
                }
                displayWin = true;
                return 2; // stalemate exit
            }
        }
        catch (Exception e)
        {
            bool win0, win1, win2;
            win0 = victoryScreen.transform.GetChild(0).gameObject.activeSelf;
            win1 = victoryScreen.transform.GetChild(1).gameObject.activeSelf;
            win2 = victoryScreen.transform.GetChild(2).gameObject.activeSelf;

            if (win0 || win1 || win2)
                return 0;
            else
                return 2;
        }

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
        yOffsetX += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter; // how far is extent of chessboard

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

        tileObject.transform.parent = transform;

        //tileObject.transform.localPosition = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
        //!!!!Debug.Log("YYYY Yoffset: " + yOffset + " bounds: " + bounds + " transform pos" + transform.position);

        //to render a triangle
        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + transform.position;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds + transform.position;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds + transform.position;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds + transform.position;

        int[] triangle = new[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangle;
        //recalculate normarls for lighting
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<MeshCollider>();

        //Debug.Log("YYYYYYY Tile scale: " + tileObject.transform.localScale);
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

    // get center of tile where chesspiece is to be placed.
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + transform.position;
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
        Debug.Log("X INPUT: " + inputTimeOut);
        Debug.Log("X AUTO: " + AutoOpponent);
        game_end_sound.Play();
        inputTimeOut = true;
        AutoOpponent = false;
        DisplayVictory(team);
    }

    private void DisplayVictory(int winningTeam)
    {
        Debug.Log("Y INPUT: " + inputTimeOut);
        Debug.Log("Y AUTO: " + AutoOpponent);
        turnScreen.SetActive(false);
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void onResetButton()
    {
        game_start_sound.Play();
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(2).gameObject.SetActive(false);
        victoryScreen.SetActive(false);
        inputTimeOut = false;
        promotionMenu.SetActive(false);
        RemoveHighlightTiles();

        displayMove_Text.text = " ";
        displaySpecialMove_Text.text = " ";

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
        turnScreen.SetActive(true);
        turnScreen.transform.GetChild(0).gameObject.SetActive(true);
        turnScreen.transform.GetChild(1).gameObject.SetActive(false);

        GameModeMenu.SetActive(true);
    }

    public void onExitButton()
    {
        Application.Quit();
    }

    public void onSurrender()
    {
        if (turnScreen.transform.GetChild(0).gameObject.activeSelf)
            CheckMate(1);
        else
            CheckMate(0);
    }



    /*Auto-Algorithm [ALPHA]*/
    int turnCount = 0;
    public void setAutoOpponent(bool value)
    {
        GameModeMenu.SetActive(false);
        inputTimeOut = false;
        AutoOpponent = value;
    }

    // fail-safe random move
    private IEnumerator RandomMoveBlack()
    {
        yield return new WaitForSeconds(1f);
        List<ChessPiece> blackPieces = new List<ChessPiece>();
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null && chessPieces[x, y].team == 1)
                {
                    blackPieces.Add(chessPieces[x, y]);
                }
            }
        }
        bool moveMade = false;
        while (!moveMade && blackPieces.Count > 0)
        {
            ChessPiece piece = blackPieces[Random.Range(0, blackPieces.Count)];
            availableMoves = piece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

            if (availableMoves.Count > 0)
            {
                Vector2Int move = availableMoves[Random.Range(0, availableMoves.Count)];
                bool res = MoveTo(piece, move.x, move.y);
                if (res)
                {
                    piece.SetPosition(GetTileCenter(move.x, move.y), false);
                    moveMade = true;
                }
                else
                {
                    blackPieces.Remove(piece);
                    continue;
                }
            }
            else
            {
                blackPieces.Remove(piece);
            }
        }
        if (moveMade == false && blackPieces.Count < 1)
        {
            CheckMate(0);
        }
    }

    // calculate best move that maximizes win
    private IEnumerator Calculate_BestBlackMove()
    {
        yield return new WaitForSeconds(1f);
        inputTimeOut = false;
        List<ChessPiece> blackPieces = new List<ChessPiece>();
        List<(ChessPiece, Vector2Int)> bestMoves = new List<(ChessPiece, Vector2Int)>();
        int bestScore = int.MinValue;

        // Gather all black pieces
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null && chessPieces[x, y].team == 1)
                {
                    blackPieces.Add(chessPieces[x, y]);
                }
            }
        }

        // Iterate over each black piece and evaluate moves
        foreach (var piece in blackPieces)
        {
            var possibleMoves = piece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            foreach (var move in possibleMoves)
            {
                int score = EvaluateMove(piece, move);

                if (score > bestScore)
                {
                    bestMoves.Clear();
                    bestMoves.Add((piece, move));
                    bestScore = score;
                }
                else if (score == bestScore)
                {
                    bestMoves.Add((piece, move));
                }
            }
        }

        bool moveMade = false;
        // Pick a random move from the best moves
        if (bestMoves.Count > 0)
        {
            // randomize openings
            /*
            if (turnCount == 0)
            {
                Shuffle(bestMoves);
                turnCount++;
            }*/
            Shuffle(bestMoves);
            foreach (var bestMove in bestMoves)
            {
                availableMoves = bestMove.Item1.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                specialMove = bestMove.Item1.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                if (ContainsValidMove(ref availableMoves, bestMove.Item2))
                {
                    bool res = MoveTo(bestMove.Item1, bestMove.Item2.x, bestMove.Item2.y);
                    if (res)
                    {
                        bestMove.Item1.SetPosition(GetTileCenter(bestMove.Item2.x, bestMove.Item2.y), false);
                        moveMade = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
        // if above algorithm fails to make a move, activate randomisation before declaring win
        if (bestMoves.Count < 1 || moveMade == false)
        {
            StartCoroutine(RandomMoveBlack());
        }
    }

    private int EvaluateMove(ChessPiece piece, Vector2Int targetPosition)
    {
        int score = 0;
        ChessPiece targetPiece = chessPieces[targetPosition.x, targetPosition.y];

        if (targetPiece != null)
        {
            score += GetPieceValue(targetPiece.type);
        }
        return score;
    }

    private int GetPieceValue(ChessPieceType type)
    {
        switch (type)
        {
            case ChessPieceType.Pawn: return 1;
            case ChessPieceType.Knight: return 3;
            case ChessPieceType.Bishop: return 3;
            case ChessPieceType.Rook: return 5;
            case ChessPieceType.Queen: return 9;
            case ChessPieceType.King: return 1000;
            default: return 0;
        }
    }

    public static void Shuffle<T>(IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
