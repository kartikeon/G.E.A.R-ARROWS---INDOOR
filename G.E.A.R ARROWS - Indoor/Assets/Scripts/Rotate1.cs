using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate1 : MonoBehaviour
{

    void Update()
    {
        transform.Rotate(new Vector3(0, 360f, 0), Time.deltaTime * 30, Space.World);
    }
}
