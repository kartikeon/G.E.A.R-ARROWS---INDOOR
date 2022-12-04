using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Android;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;


public class MappingController : MonoBehaviour
{
    
    #region INSPECTOR_VARIABLES   
    public  GameObject ChooseArrows, waypoint, spherePrefab, forwardPrefab, diamond, Arrow, Arrow_Bar, RedBlack_Arrow, Bat, BlackPanther, Deadpool, Ironman, Joker, Spidey; 
    public GameObject linktransform;
    public AudioClip a1,a2;
    public AudioSource audioSource;
    public MessageBehavior messageBehavior;
    public TextMeshProUGUI driftText, informationText, distanceText, stxt, Dirtxt;
    [Tooltip("Divides the screen along the height and width by that much times for array raycasting")]
    public int screenSplitCount = 5;
    // [Tooltip("Enable this to instantiate objects along distance, only initial anchor will be present; Disable to use array raycasting for finding feature points to anchor objects along the way")]
    [Tooltip("Range in meters for array raycasting")]
    public float raycastRange = 1.5f;
    public float gpsTolerance = 0.00005f;
    public Toggle toggleInstantiationTypeButton;
    public Toggle toggleARFoundationButton;
    #endregion

    #region PRIVATE_VARIABLES
    ARSessionOrigin arSessionOrigin;
    ARRaycastManager arRaycastManager;
    ARPlaneManager arPlaneManager;
    ARPointCloudManager arPointCloudManager;


    List<ARRaycastHit> hits = new List<ARRaycastHit>();
    List<ARRaycastHit> wayHits = new List<ARRaycastHit>();
    List<GameObject> instantiatedObjects = new List<GameObject>();

    GameObject origin;
    bool isPlaying;
    bool isOriginFound;
    bool isVisible;
    bool shouldInstantiateWithDistance = false;
    bool isARFoundationEnabled;
    bool isGPSEnabled;
    bool isAtOriginPoint;

     Vector3 initialPos, currentPos, previousPos;

    LocationService locationService;
    Coordinates initialCoordinates;
    Coordinates finalCoordinates;

     public float lat1 ,long1 ,lat2 ,long2, distance1;
     public string label;

     public LinkTransform link;
    
     public int y,k;

    List<Vector2> screenPointsForRaycasting = new List<Vector2>();
    #endregion
   
    void Awake()
    {
        ValueY();
        print("SYS ValueY :" + y);

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            Permission.RequestUserPermission(Permission.FineLocation);
            
            
    }

    public void ValueY()
    {
        linktransform = GameObject.FindGameObjectsWithTag("LinkTransform")[0] as GameObject;
        link = linktransform.GetComponent<LinkTransform>();
        y = link.A;
        k = link.D;

         switch (k)
         {
             case 0:
             ChooseArrows = RedBlack_Arrow;
             break;

             case 1:
             ChooseArrows = Arrow;
             break;

             case 2:
             ChooseArrows = Arrow_Bar;
             break;

             case 3:
             ChooseArrows = forwardPrefab;
             break;

             case 4:
             ChooseArrows = Spidey;
             break;

             case 5:
             ChooseArrows = Ironman;
             break;

             case 6:
             ChooseArrows = Bat;
             break;

             case 7:
             ChooseArrows = Deadpool;
             break;

             case 8:
             ChooseArrows = BlackPanther;
             break;

             case 9:
             ChooseArrows = Joker;
             break;

             default:
             break;
         }
    }

        void Start()
    {
        

        isPlaying = false;
        isOriginFound = false;
        isVisible = true;
        isGPSEnabled = false;
        isAtOriginPoint = false;

        arRaycastManager = FindObjectOfType<ARRaycastManager>();
        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        arPointCloudManager = FindObjectOfType<ARPointCloudManager>();

         GetScreenPoints();

         CoordinatesManager();
         
         //RouteManager();

        initialCoordinates.gpsLat = lat1;   
        initialCoordinates.gpsLong = long1;                     
        finalCoordinates.gpsLat = lat2;      
        finalCoordinates.gpsLong =long2;
        
        

        StartCoroutine(GenerateRoutePrefabs());

    }

    #region PUBLIC_METHODS

    public void CreateSpheresOnTheGo()
    {
        shouldInstantiateWithDistance = toggleInstantiationTypeButton.isOn;
        toggleInstantiationTypeButton.interactable = false;

        isARFoundationEnabled = toggleARFoundationButton.isOn;
        toggleARFoundationButton.interactable = false;

        isPlaying = true;
        if (shouldInstantiateWithDistance)
            StartCoroutine(InitiateMultiWorldPlacement());
        else
            StartCoroutine(InstantiateAtNearestFeaturePoint());
    }

    public void StopSpheres()
    {
        toggleInstantiationTypeButton.interactable = true;
        toggleARFoundationButton.interactable = true;

        isPlaying = false;
        isOriginFound = false;
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;

        foreach (GameObject go in instantiatedObjects)
        {
            go.transform.GetComponent<Renderer>().enabled = isVisible;
        }

    }

    public void StartRouting()
    {
        isAtOriginPoint = true;
    }

    void GetScreenPoints()
    {
        float incrementalWidth = Screen.width / screenSplitCount;
        float incrementalHeight = Screen.height / screenSplitCount;

        screenPointsForRaycasting.Clear();
        for (Vector2 currentPoint = Vector2.zero; currentPoint.y <= Screen.height; currentPoint.y += incrementalHeight)
        {
            for (currentPoint.x = 0; currentPoint.x <= Screen.width; currentPoint.x += incrementalWidth)
            {
                screenPointsForRaycasting.Add(currentPoint);
                Debug.Log("POINTS : " + currentPoint);
            }
        }

    }

    public void ClearAllSpheres()
    {
        foreach (GameObject go in instantiatedObjects)
            Destroy(go.gameObject);

        instantiatedObjects.Clear();
    }

   
  


    public void BackButton()
    {  

        SceneManager.MoveGameObjectToScene(linktransform, SceneManager.GetActiveScene());
        SceneManager.LoadScene(1);

    }

    

    public void SetOrigin()
    {
        if (isARFoundationEnabled)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (arRaycastManager.Raycast(touch.position, hits, TrackableType.FeaturePoint))
                {
                    origin = Instantiate(waypoint, hits[0].pose.position, Quaternion.identity);
                    instantiatedObjects.Add(origin);
                    previousPos = Camera.main.transform.position;
                    isOriginFound = true;
                }
            }
        }
        else
        {
            origin = Instantiate(waypoint, Camera.main.transform.position, Quaternion.identity);
            instantiatedObjects.Add(origin);
            previousPos = Camera.main.transform.position;
            isOriginFound = true;
        }
    }




    public Vector3 GeneratePathInDirection(Vector3 currentPosition,int distanceInMeters,DIRECTION turnDirection)
    {
        currentPos = currentPosition;
        float angle;

        switch (turnDirection) 
        {
           case DIRECTION.FORWARD:
                angle = 0;
                break;
                
            case DIRECTION.RIGHT:     
                angle = 90;
                break;

           case DIRECTION.LEFT:     
                angle = -90;
                break;

           case DIRECTION.RIGHT45:
                angle = 45;
                break;

            case DIRECTION.LEFT45:
                angle = -45;
                break;

            case DIRECTION.BACKWARD:
                angle = 180;
                break;

               default:
                angle = 0;
                break;
        }

        for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }

        for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }

       /* for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(Arrow, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }

        for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(Arrow_Bar, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }*/

       

        return currentPos;
    }


    #endregion



    #region COROUTINES
    public IEnumerator InstantiateAtNearestFeaturePoint()
    {

        yield return new WaitUntil(() => isPlaying);

        while (!isOriginFound)
        {
            SetOrigin();
            yield return new WaitForEndOfFrame();
        }

        while (isPlaying)
        {
            currentPos = Camera.main.transform.position;
            //  Vector3 instantiatePos = new Vector3(currentPos.x, .5f, currentPos.z);
            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                foreach (Vector2 screenPoint in screenPointsForRaycasting)
                {
                    wayHits.Clear();
                    if (arRaycastManager.Raycast(screenPoint, wayHits, TrackableType.FeaturePoint))
                    {
                        if (Vector3.Distance(currentPos, wayHits[0].pose.position) < raycastRange)
                        {
                            GameObject go = Instantiate(ChooseArrows, wayHits[0].pose.position, wayHits[0].pose.rotation);
                            instantiatedObjects.Add(go);
                            previousPos = currentPos;
                            break;

                        }

                         if (Vector3.Distance(currentPos, wayHits[0].pose.position) < raycastRange)
                        {
                            GameObject go = Instantiate(diamond, wayHits[0].pose.position, wayHits[0].pose.rotation);
                            instantiatedObjects.Add(go);
                            previousPos = currentPos;
                            break;

                        }
                        
                    }
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator InitiateMultiWorldPlacement()
    {
        yield return new WaitUntil(() => isPlaying);

        while (!isOriginFound)
        {
            SetOrigin();
            yield return new WaitForEndOfFrame();
        }

        while (isPlaying)
        {
            currentPos = Camera.main.transform.position;
            //  Vector3 instantiatePos = new Vector3(currentPos.x, .5f, currentPos.z);
            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(ChooseArrows, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }

            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(diamond, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }

            /*if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(Arrow, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }

            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(Arrow_Bar, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }*/

            
            yield return new WaitForEndOfFrame();
        }
    }
    







   public int dir;
    public void RouteManager()
    {
         
           // ValueY();
            dir = y;
            print("SYS Dir: " + dir);

                switch (dir)
                {

                    case 12:
                    Direction12();
                    print("SYS dir 12: " + dir);
                    break;

                    case 21:
                    Direction21();
                    print("SYS dir 21: " + dir);
                    break;

                    case 13:
                    Direction13();
                    print("SYS dir 13: " + dir);
                    break;

                    case 31:
                    Direction31();
                    print("SYS dir 31: " + dir);
                    break; 
                    
                    case 14:
                    Direction14();
                    print("SYS dir 14: " + dir);
                    break;

                    case 41:
                    Direction41();
                    print("SYS dir 41: " + dir);
                    break;

                    case 23:
                    Direction23();
                    print("SYS dir 23: " + dir);
                    break;

                    case 32:
                    Direction32();
                    print("SYS dir 32: " + dir);
                    break;

                     case 24:
                    Direction24();
                    print("SYS dir 24: " + dir);
                    break;

                    case 42:
                    Direction42();
                    print("SYS dir 42: " + dir);
                    break; 
                    
     
                    case 34:
                    Direction34();
                    print("SYS dir 34: " + dir);
                    break; 
                    
                    case 43:
                    Direction43();
                    print("SYS dir 43: " + dir);
                    break;

                  
                    default:
                    break;

                }

    }

    public void CoordinatesManager()
    {
        int gps;
         //ValueY();
         gps = y;
         print("SYS gps: " + gps);

         switch (gps)
         {
             case 12:

             lat1 = 13.05580f;    // I  13.055806, 80.226323
             long2 = 80.22632f;                      //  II     13.055902, 80.226304   
             lat2 = 13.05590f;                           //III    13.055995, 80.226557   
             long2 = 80.22630f;                                //IV         13.056076, 80.226469
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
             case 21:

             lat1 = 13.05580f;    //  I  13.055795, 80.226476
             long2 = 80.22630f;                         // II  13.055806, 80.226323
             lat2 = 13.05580f;                              //  III     13.055902, 80.226304
             long2 = 80.22632f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              case 13:

             lat1 = 13.05580f;    // I  13.055806, 80.226323
             long2 = 80.22632f;                        // II  13.055806, 80.226323
             lat2 = 13.05599f;                              //  III     13.055902, 80.226304
             long2 = 80.22655f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              case 31:

             lat1 = 13.05599f;    //  I  13.055795, 80.226476
             long2 = 80.22655f;                         // II  13.055806, 80.226323
             lat2 = 13.05580f;                              //  III     13.055902, 80.226304
             long2 = 80.22632f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              case 14:

             lat1 = 13.05580f;    // I  13.055806, 80.226323
             long2 = 80.22632f;                        // II  13.055806, 80.226323
             lat2 = 13.05615f;                              //  III     13.055902, 80.226304
             long2 = 80.22660f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              case 41:

             lat1 = 13.05615f;    //  I  13.055795, 80.226476
             long2 = 80.22660f;                         // II  13.055806, 80.226323
             lat2 = 13.05580f;                              //  III     13.055902, 80.226304
             long2 = 80.22632f;                                 //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              
              case 23:

             lat1 = 13.05580f;    //  I  13.055795, 80.226476
             long2 = 80.22630f;                          // II  13.055806, 80.226323
             lat2 = 13.05599f;                              //  III     13.055902, 80.226304
             long2 = 80.22655f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              case 32:

            lat1 = 13.05599f;    //  I  13.055795, 80.226476
             long2 = 80.22655f;                          // II  13.055806, 80.226323
             lat2 = 13.05590f;                           //III    13.055995, 80.226557   
             long2 = 80.22630f;                                   //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
             case 24:

             lat1 = 13.05580f;    //  I  13.055795, 80.226476
             long2 = 80.22630f;                        // II  13.055806, 80.226323
            lat2 = 13.05615f;                              //  III     13.055902, 80.226304
             long2 = 80.22660f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              case 42:

             lat1 = 13.05615f;    //  I  13.055795, 80.226476
             long2 = 80.22660f;                         // II  13.055806, 80.226323
             lat2 = 13.05590f;                           //III    13.055995, 80.226557   
             long2 = 80.22630f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
             
              case 34:

             lat1 = 13.05599f;    //  I  13.055795, 80.226476
             long2 = 80.22655f;                        // II  13.055806, 80.226323
             lat2 = 13.05615f;                              //  III     13.055902, 80.226304
             long2 = 80.22660f;                                 //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
              case 43:

             lat1 = 13.05615f;    //  I  13.055795, 80.226476
             long2 = 80.22660f;                      // II  13.055806, 80.226323
             lat2 = 13.05599f;                              //  III     13.055902, 80.226304
             long2 = 80.22655f;                                //  IV   13.056158, 80.226604
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

            

             default:
             break;
         }
    }


  
    public IEnumerator GenerateRoutePrefabs()
    {
        driftText.text = "Please enable GPS";
        yield return new WaitUntil(() => Input.location.isEnabledByUser);
        Input.location.Start();
        

        while (true)
        {

            driftText.text = "LAT : " + Input.location.lastData.latitude + ", LONG : " + Input.location.lastData.longitude;

            if ((Mathf.Abs(Input.location.lastData.latitude - initialCoordinates.gpsLat) < gpsTolerance) && (Mathf.Abs(Input.location.lastData.longitude - initialCoordinates.gpsLong) < gpsTolerance))
            {
                informationText.text = "INITIAL POSITION";
            }

            else if ((Mathf.Abs(Input.location.lastData.latitude - finalCoordinates.gpsLat) < gpsTolerance) && (Mathf.Abs(Input.location.lastData.longitude - finalCoordinates.gpsLong) < gpsTolerance))
            {
                informationText.text = "FINAL DESINATION";
            }

            else
                informationText.text = "FIND A NODAL POINT.";

            if (isAtOriginPoint)
            {
                Vector3 currentPos = Camera.main.transform.position;    //////////value (2)////////////////////////////////////////////////////////////////////////////////////////////////////////// value---------(2)
                

                RouteManager();//////////////////////////////////////////////////////////////////////
            
               
                isAtOriginPoint = false;

            }



            if (instantiatedObjects.Count != 0)// +(instantiatedObjects.Count - 1) - 
            {
                float distance = Vector3.Distance(instantiatedObjects[instantiatedObjects.Count - 1].transform.position, Camera.main.transform.position);
                distanceText.text = "Distance covered : " + distance + " Meters";

               // int distanceA =  Convert.ToInt32(distanceValue).value;

            }

            else
            {
                distanceText.text = "Journey not started";
            }

            
            

         if (instantiatedObjects.Count != 0)//(instantiatedObjects.Count - 1) +
           {
              distance1 =  Vector3.Distance(instantiatedObjects[instantiatedObjects.Count - 1].transform.position, Camera.main.transform.position);

                StartCoroutine(MeterStone());
             
            }

        
            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator MeterStone()
    {
        yield return new WaitUntil(() => Input.location.isEnabledByUser);


        /* if(distance1 >= 2 && distance1 < 3) 
                 {
                    label = "1";
                    //audioSource.clip = Resources.Load<AudioClip>("AudioClips/1");
                    audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                    audioSource.Play();
                    yield return new WaitForSeconds(audioSource.clip.length);
                    audioSource.clip = a1;
                    label = "AHEAD";
                    audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                    audioSource.Play ();
                    messageBehavior.ShowMessage(label);

                 }*/

                   
                    if( 149 <= distance1 && distance1 < 150) 
                    {
                        label = "WALK 150 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                   
                    if( 99 <= distance1 && distance1 < 100) 
                    {
                        label = "WALK 100 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                      if( 89 <= distance1 && distance1 < 90) 
                    {
                        label = "WALK 90 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if( 79 <= distance1 && distance1 < 80) 
                    {
                        label = "WALK 80 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if( 69 <= distance1 && distance1 < 70) 
                    {
                        label = "WALK 70 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                     if( 59 <= distance1 && distance1 < 60) 
                    {
                        label = "WALK 60 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                     if( 49 <= distance1 && distance1 < 50) 
                    {
                        label = "WALK 50 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }
                    
                    if( 29 <= distance1 && distance1 < 30) 
                    {
                        label = "WALK 30 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if( 19 <= distance1 && distance1 < 20) 
                    {
                        label = "WALK 20 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if( 14 <= distance1 && distance1 < 15) 
                    {
                        label = "WALK 15 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                     if( 4 <= distance1 && distance1 < 5) 
                    {
                        label = "WALK 5 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if(0 <= distance1 && distance1 < 1) 
                    {
                        label = "REACHED DESTINATION";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

 
    }
  
   

    #endregion

    
    void Update()
    {
       
    }

    #region STRUCTS,ENUMS AND CLASSES

   public struct Coordinates
    {
        public float gpsLat { get; set; }
        public float gpsLong { get; set; }
    }

   public enum DIRECTION
    {
        FORWARD=0,RIGHT=1,LEFT=2,RIGHT45=3,LEFT45=4,BACKWARD=5  
    }

#endregion

#region DIRECTION_VALUES

/*              for (int goStaight = 0; goStaight < 25; goStaight++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 2; goRight++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 17; goLeft++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 17; goBack++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                

                */








public void Direction12()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 10; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                /*
                for (int goStaight = 0; goStaight < 2; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }



                
                
                for (int goLeft = 0; goLeft < 2; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction21()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 10; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                /*
                for (int goLeft = 0; goLeft < 18; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 2; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }       
                
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction13()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                
                for (int goLeft = 0; goLeft < 14; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                
                for (int goStaight = 0; goStaight <16; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 5; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 12; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }                


/*
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction31()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 12; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 5; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 16; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                 for (int goBack = 0; goBack < 12; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }


/*

                for (int goRight = 0; goRight < 2; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction14()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 14; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goStaight = 0; goStaight <16; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 5 ; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 5 ; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                
                for (int goLeft = 0; goLeft < 12; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                /*
                for (int goLeft = 0; goLeft < 2; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction41()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1 ; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 13 ; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goBack = 0; goBack < 5 ; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 5 ; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                 for (int goBack = 0; goBack < 16 ; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 17 ; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 1 ; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                /*
                for (int goLeft = 0; goLeft < 18; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }



                
                
                for (int goLeft = 0; goLeft < 2; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction23()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 4; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 15; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 5 ; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 12 ; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1 ; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

/*

                for (int goRight = 0; goRight < 2; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 2; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction32()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1 ; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 12; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 5; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 16; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                 for (int goBack = 0; goBack < 4; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

/*
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction24()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 4; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goStaight = 0; goStaight <15; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 5; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 5 ; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                
                for (int goLeft = 0; goLeft < 12; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

/*


                
                
                for (int goLeft = 0; goLeft < 2; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction42()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1 ; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight <13; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goBack = 0; goBack < 5; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 5; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                 for (int goBack = 0; goBack < 16; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 4; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                /*
                for (int goStaight = 0; goStaight < 3; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 18; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 18; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                
                
                for (int goRight = 0; goRight < 3; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 2; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction34()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 7; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 13; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                /*
                for (int goRight = 0; goRight < 2; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }
     public void Direction43()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 13; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 7; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                /*
                for (int goStaight = 0; goStaight < 5; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }



                for (int goRight = 0; goRight < 2; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                
                for (int goBack = 0; goBack < 2; goBack++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y -180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/
            
     }
#endregion


}



