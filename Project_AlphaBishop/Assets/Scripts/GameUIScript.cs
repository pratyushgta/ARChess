using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIScript : MonoBehaviour
{
    public GameObject GameUI;
    public GameObject ControlUI;
    public GameObject PauseMenu;
    public GameObject VictoryScreen;
    public GameObject TurnScreen;
    public GameObject CoachingUI;
    public GameObject NotSupportedUI;
    public GameObject WaitingUI;
    public GameObject GreetingUI;

    public GameObject PlacementManager;
    public GameObject XROrigin;
    public GameObject ARSession;
    public GameObject MainCamera;

    public GameObject ChessboardPrefab;
    private GameObject chessboardInstance;
    private Chessboard chessboardScript;

    private GameObject PlacementManagerInstance;
    private BoardSpawnManager pm_script;

    private bool isAR;

    private GameObject promotionMenu;

    public static GameUIScript Instance { set; get; }

    void Awake()
    {
        Instance = this;
    }

    // Buttons
    public void OnARGameButton()
    {
        if (PlacementManagerInstance != null)
        {
            Destroy(PlacementManagerInstance);
        }

        GameUI.SetActive(false);
        MainCamera.SetActive(false);
        PlacementManagerInstance = Instantiate(PlacementManager);
        pm_script = PlacementManagerInstance.GetComponent<BoardSpawnManager>();
        PlacementManagerInstance.SetActive(true);
        XROrigin.SetActive(true);
        ARSession.SetActive(true);
        isAR = true;
    }

    public void On3DGameButton()
    {
        if (chessboardInstance != null)
        {
            Destroy(chessboardInstance);
        }
        GameUI.SetActive(false);
        ControlUI.SetActive(true);

        chessboardInstance = Instantiate(ChessboardPrefab);
        chessboardScript = chessboardInstance.GetComponent<Chessboard>();

        chessboardInstance.SetActive(true);
        isAR = false;
    }

    public void onPauseButton()
    {
        PauseMenu.SetActive(true);
    }

    public void onResumeButton()
    {
        PauseMenu.SetActive(false);
    }

    public void onRestartButton()
    {
        PauseMenu.SetActive(false);
        if (isAR)
        {
            pm_script.onRestart();
        }
        else if (chessboardScript != null)
        {
            chessboardScript.onResetButton();
        }
    }

    public void onQuitButton()
    {
        /*
        PauseMenu.SetActive(false);
        NotSupportedUI.SetActive(false);
        CoachingUI.SetActive(false);
        GreetingUI.SetActive(false);
        if (isAR)
        {
            pm_script.Destroy();
            Destroy(PlacementManagerInstance);
            XROrigin.SetActive(false);
            ARSession.SetActive(false);
        }
        if (chessboardInstance != null)
        {
            Destroy(chessboardInstance);
        }
        MainCamera.SetActive(true);
        GameUI.SetActive(true);
        ControlUI.SetActive(false);
        VictoryScreen.SetActive(false);
        TurnScreen.SetActive(false);
        */
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void onQueenPromotion()
    {
        if(isAR)
            pm_script.onARQueenPromotion();
        else
            chessboardScript.handlePawnPromotion(ChessPieceType.Queen);
    }
    public void onBishopPromotion()
    {
        if (isAR)
            pm_script.onARQueenPromotion();
        else
            chessboardScript.handlePawnPromotion(ChessPieceType.Bishop);
    }
    public void onRookPromotion()
    {
        if (isAR)
            pm_script.onARQueenPromotion();
        else
            chessboardScript.handlePawnPromotion(ChessPieceType.Rook);
    }
    public void onKnightPromotion()
    {
        if (isAR)
            pm_script.onARQueenPromotion();
        else
            chessboardScript.handlePawnPromotion(ChessPieceType.Knight);
    }

    public void onGameMode_PvP()
    {
        if (isAR)
            pm_script.onAR_GameMode_PvP();
        else
            chessboardScript.setAutoOpponent(false);
    }
    public void onGameMode_PvAlgo()
    {
        if (isAR)
            pm_script.onAR_GameMode_PvAlgo();
        else
            chessboardScript.setAutoOpponent(true);
    }


}
