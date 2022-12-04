using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LinkTransform : MonoBehaviour
{

 public int A,D;


    void Awake()
 {  
     
     GameObject[] objs = GameObject.FindGameObjectsWithTag("LinkTransform");
     
     if (objs.Length > 1)
     {
       Destroy(this.gameObject);
         
     }
     DontDestroyOnLoad(this.gameObject);  

 }



 
}


 /* GameObject[] objs = GameObject.FindGameObjectsWithTag("LinkTransform");
     
     if (objs.Length > 1)
     {
       Destroy(this.gameObject);
         
     }
     DontDestroyOnLoad(this); */ 
