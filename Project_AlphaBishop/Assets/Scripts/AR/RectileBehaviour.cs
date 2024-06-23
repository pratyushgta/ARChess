using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ReticleBehaviour : MonoBehaviour
{
    public GameObject Child;
    public SurfaceManager SurfaceManager;
    public GameObject ChessboardPrefab;

    public ARPlane CurrentPlane;

    //private bool chessboardSpawned = false;

    private void Start()
    {
        if (Child == null)
        {
            Child = transform.GetChild(0).gameObject;
        }
       // if (chessboardSpawned)
        //    GameObject.Destroy(Child);
        if (SurfaceManager == null)
        {
            Debug.LogError("XXXXX Surface Manager is not assigned XXXXX");
            return;
        }

        if (ChessboardPrefab == null)
        {
            Debug.LogError("XXXXX Chessboard Prefab is not assigned XXXXX");
            return;
        }
    }

    private void Update()
    {
     //   if(chessboardSpawned)
       //     GameObject.Destroy(Child);
        if (SurfaceManager == null || SurfaceManager.RaycastManager == null)
        {
            Debug.LogError("XXXXX Check Surface Manager or Raycast XXXXX");
            return;
        }

        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        SurfaceManager.RaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinBounds);

        CurrentPlane = null;
        ARRaycastHit? hit = null;
        if (hits.Count > 0)
        {
            var lockedPlane = SurfaceManager.LockedPlane;
            hit = lockedPlane == null
                ? hits[0]
                : hits.SingleOrDefault(x => x.trackableId == lockedPlane.trackableId);
        }

        if (hit.HasValue)
        {
            CurrentPlane = SurfaceManager.PlaneManager.GetPlane(hit.Value.trackableId);
            transform.position = hit.Value.pose.position;
        }
        Child.SetActive(CurrentPlane != null);
        
        /*
        if (!chessboardSpawned && CurrentPlane != null && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Child.SetActive(false);
            SurfaceManager.DisablePlaneDetection();
            //ChessboardPrefab.SetActive(true);
            //Instantiate(ChessboardPrefab, transform.position, transform.rotation);
            var chessboardInstance = Instantiate(ChessboardPrefab, transform.position, Quaternion.identity);
            chessboardInstance.SetActive(true);
            chessboardSpawned = true;
        }
        */
    }

    public void DisableReticle()
    {
        if(Child != null) Child.SetActive(false);
        //Child.SetActive(false);
    }
}
