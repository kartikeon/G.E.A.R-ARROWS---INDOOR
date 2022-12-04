using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class MainMenu2 : MonoBehaviour
{
   public TextMeshProUGUI txt;
    public LinkTransform2 linkk; 
    public GameObject linktransform2;

 
    public int y;
   

	public TMP_Dropdown DropdownBox3 , DropdownBox4 , DropdownBox5; 
    public int i,j,k,x;
   

   

    public void Start()
    {
     
    }

   

    public void Dropdown1()
    {
        i = DropdownBox3.value;
    }

    public void Dropdown2()
    {
        j = DropdownBox4.value;
    }

    public void Dropdown3()
    {
        k = DropdownBox5.value;
    }

    public void MatrixValue2()
    {
        if(i == 0 || j == 0)
        {
            txt.text = "\n  Please Select the Route";

        }
        
     else
     {
       if (i==j)
        {
            txt.text = "\n INVALID ROUTE ";
        }

        else 
        {
             x = (i*10) + j; 
             txt.text = "\n CORRECT ROUTE \n Value: " + x;
             NavigateBtn();
        }

    
     }

    }
    public void BackBtn()
    {
        linktransform2 = GameObject.FindGameObjectsWithTag("LinkTransform2")[0] as GameObject;
        SceneManager.MoveGameObjectToScene(linktransform2, SceneManager.GetActiveScene());
        SceneManager.LoadScene(0);
    }

   

    public void NavigateBtn()
    {
       
        print("playing....Done");

      
         SceneManager.LoadScene(4);
          
         y = x;
        linkk.B = y;
        linkk.D = k;
    }
    
	void Update () 
    {

    }
    
}
