using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkTransform2 : MonoBehaviour
{
  
  public int B,D;


    void Awake()
 {  
     
     GameObject[] objs = GameObject.FindGameObjectsWithTag("LinkTransform2");
     
     if (objs.Length > 1)
     {
       Destroy(this.gameObject);
         
     }
     DontDestroyOnLoad(this.gameObject);  

 }

}
