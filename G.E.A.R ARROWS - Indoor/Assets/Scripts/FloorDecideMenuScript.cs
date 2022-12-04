using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class FloorDecideMenuScript : MonoBehaviour
{
   public void FirstFloor()
   {
       SceneManager.LoadScene(1);

   }

   public void SecondFloor()
   {
       SceneManager.LoadScene(3);
       
   }
}
