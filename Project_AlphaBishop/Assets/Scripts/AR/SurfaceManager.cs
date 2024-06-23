using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SurfaceManager : MonoBehaviour
{
    public ARPlaneManager PlaneManager;
    public ARRaycastManager RaycastManager;
    public ARPlane LockedPlane;

    public GameObject Prompt_ScanSurfaces;
    public GameObject Prompt_TapToPlace;

    private bool firstPlaneDetected = false;

    public void LockPlane(ARPlane keepPlane)
    {
        // Disable all planes except the one we want to keep
        var arPlane = keepPlane.GetComponent<ARPlane>();
        foreach (var plane in PlaneManager.trackables)
        {
            if (plane != arPlane)
            {
                plane.gameObject.SetActive(false);
            }
        }

        LockedPlane = arPlane;
        PlaneManager.planesChanged += DisableNewPlanes;
    }

    private void Start()
    {
        PlaneManager = GetComponent<ARPlaneManager>();
        Prompt_ScanSurfaces.SetActive(true);
    }

    private void Update()
    {
        if (!firstPlaneDetected && PlaneManager.trackables.count > 0)
        {
            firstPlaneDetected = true;
            Prompt_ScanSurfaces.SetActive(false);
            Prompt_TapToPlace.SetActive(true);
        }

        if (LockedPlane?.subsumedBy != null)
        {
            LockedPlane = LockedPlane.subsumedBy;
        }
    }

    private void DisableNewPlanes(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            plane.gameObject.SetActive(false);
        }
    }
    public void DisablePlaneDetection()
    {
        /*PlaneManager.enabled = false;
        foreach (var plane in PlaneManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }*/
        if (PlaneManager != null)
        {
            PlaneManager.enabled = false;
            RaycastManager.enabled = false;
            foreach (var plane in PlaneManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
        Prompt_ScanSurfaces.SetActive(false);
        Prompt_TapToPlace.SetActive(false);
    }
}
