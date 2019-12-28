using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bot : MonoBehaviour
{
    public static GameObject[] points;

    void Start()
    {
        if (points.Length == 0)
            points = GameObject.FindGameObjectsWithTag("Points");
    }

    void Update()
    {
        
    }

    void GetClosestPointData()
    {

    }
}
