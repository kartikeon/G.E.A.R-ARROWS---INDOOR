using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;



public class MainmenuScript : MonoBehaviour
{
	public TextMeshProUGUI txt;
    public LinkTransform link; 
     public GameObject linktransform;

   
    public int y;
   

	public TMP_Dropdown DropdownBox1 , DropdownBox2 , DropdownBox3; 
    public int i,j,k,x;
   

   

    public void Start()
    {
     
    }

    
    public void Dropdown1()
    {
        i = DropdownBox1.value;
    }

    public void Dropdown2()
    {
        j = DropdownBox2.value;
    }

    public void Dropdown3()
    {
        k = DropdownBox3.value;
    }

    public void MatrixValue()
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

   public void bckbtn()
   {
       linktransform = GameObject.FindGameObjectsWithTag("LinkTransform")[0] as GameObject;
       SceneManager.MoveGameObjectToScene(linktransform, SceneManager.GetActiveScene());
       SceneManager.LoadScene(0);
   }

    public void NavigateBtn()
    {
       
        print("playing....Done");

      
         SceneManager.LoadScene(2);
          
         y = x;
        link.A = y;
        link.D = k;
    }
    
	void Update () 
    {

    }
   


}
