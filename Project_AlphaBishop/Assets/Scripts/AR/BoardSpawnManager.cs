using System.Collections;
using TMPro;
using Unity.VisualScripting;
//using UnityEditor.Search;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;


public class BoardSpawnManager : MonoBehaviour
{
    public ReticleBehaviour Reticle;
    public SurfaceManager SurfaceManager;
    public Chessboard chess;

    public GameObject ChessboardPrefab;
    public GameObject ReticleObject;
    public GameObject SurfaceObject;

    public GameObject GreetingUI;
    public GameObject ControlUI;

    public GameObject CoachingUI;
    public GameObject NotSupportedUI;

    public GameObject WaitingUI;

    public GameObject chessboardInstance;
    private Chessboard chessboardScript;


    private bool chessboardSpawned = false;

    public void onContinueButton()
    {
        GreetingUI.SetActive(false);
        SurfaceObject.SetActive(true);
        ReticleObject.SetActive(true);

    }

    public void Destroy()
    {
        if (SurfaceObject != null)
            SurfaceManager.DisablePlaneDetection();
        if (ReticleObject != null)
            Reticle.DisableReticle();
        if (chessboardInstance != null)
        {
            chessboardInstance.SetActive(false);
            Destroy(chessboardInstance);
            Destroy(chessboardScript);
        }
        chessboardSpawned = false;
        return;
    }

    public void onRestart()
    {
        if (chessboardScript != null)
        {
            chessboardScript.onResetButton();
        }
    }

    private void Start()
    {
        CoachingUI.SetActive(true);
        WaitingUI.SetActive(true);
        StartCoroutine(CheckARSupport());
    }

    IEnumerator CheckARSupport()
    {
        yield return new WaitForSeconds(3);

        GameObject displayError = NotSupportedUI.transform.GetChild(3).gameObject;
        TextMeshProUGUI errorText = displayError.GetComponent<TextMeshProUGUI>();

        //GameObject displayError = GreetingUI.transform.GetChild(3).gameObject;
        //TextMeshProUGUI errorText = displayError.GetComponent<TextMeshProUGUI>();

        // Wait for the XR General Settings instance to be ready
        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
        {
            WaitingUI.SetActive(false);
            NotSupportedUI.SetActive(true);
            errorText.text = "[E-BSM-1] Failed to set-up AR";
            Debug.LogError("XX XR General Settings or Manager instance is null.");
            yield break;
        }

        var xrManager = XRGeneralSettings.Instance.Manager;
        var xrLoader = xrManager.activeLoader;

        // Ensure the XR Loader is initialized
        if (xrLoader == null)
        {
            WaitingUI.SetActive(false);
            NotSupportedUI.SetActive(true);
            errorText.text = "[E-BSM-2] Failed to set-up AR";
            Debug.LogError("XX XR Loader is not initialized.");
            yield break;
        }

        // Request information about the availability of AR
        var sessionSubsystem = xrLoader.GetLoadedSubsystem<XRSessionSubsystem>();
        if (sessionSubsystem == null)
        {
            WaitingUI.SetActive(false);
            NotSupportedUI.SetActive(true);
            errorText.text = "[E-BSM-3] Failed to set-up AR";
            Debug.LogError("XX XR Session Subsystem is not loaded.");
            yield break;
        }

        // Check AR availability
        var availabilityPromise = sessionSubsystem.GetAvailabilityAsync();
        yield return availabilityPromise;

        var availability = availabilityPromise.result;
        if (availability.IsSupported() && availability.IsInstalled())
        {
            WaitingUI.SetActive(false);
            GreetingUI.SetActive(true);
            //errorText.text = "TEST TEST TEST";
            Debug.Log("XX AR is supported and installed on this device.");
        }
        else if (availability.IsSupported())
        {
            WaitingUI.SetActive(false);
            NotSupportedUI.SetActive(true);
            errorText.text = "[E-BSM-4] AR is supported but not installed on this device";
            Debug.Log("XX AR is supported but not installed on this device.");
        }
        else
        {
            WaitingUI.SetActive(false);
            NotSupportedUI.SetActive(true);
            errorText.text = "[E-BSM-4] AR is not supported on this device";
            Debug.Log("XX AR is not supported on this device.");
        }
    }
    private void Update()
    {
        if (!chessboardSpawned && Reticle.CurrentPlane != null && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            chessboardSpawned = true;
            ControlUI.SetActive(true);
            CoachingUI.SetActive(false);
            SurfaceManager.DisablePlaneDetection();
            Reticle.DisableReticle();
            SurfaceObject.SetActive(false);
            ReticleObject.SetActive(false);
            chessboardInstance = Instantiate(ChessboardPrefab, Reticle.transform.position, Quaternion.identity);
            chessboardScript = chessboardInstance.GetComponent<Chessboard>();
            chessboardInstance.SetActive(true);
        }

    }

    private bool WasTapped()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        if (Input.touchCount == 0)
        {
            return false;
        }

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
        {
            return false;
        }

        return true;
    }

    public void onARQueenPromotion()
    {
        chessboardScript.handlePawnPromotion(ChessPieceType.Queen);
    }
    public void onARBishopPromotion()
    {
        chessboardScript.handlePawnPromotion(ChessPieceType.Bishop);
    }
    public void onARRookPromotion()
    {
        chessboardScript.handlePawnPromotion(ChessPieceType.Rook);
    }
    public void onARKnightPromotion()
    {
        chessboardScript.handlePawnPromotion(ChessPieceType.Knight);
    }

    public void onAR_GameMode_PvP()
    {
        chessboardScript.setAutoOpponent(false);
    }
    public void onAR_GameMode_PvAlgo()
    {
        chessboardScript.setAutoOpponent(true);
    }
}