using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public GameObject upNeighbor;
    public GameObject downNeighbor;
    public GameObject leftNeighbor;
    public GameObject rightNeighbor;
    private bool hasUpNeighbor;
    private bool hasDownNeighbor;
    private bool hasLeftNeighbor;
    private bool hasRightNeighbor;
    
    // Start is called before the first frame update
    void Start()
    {
        hasUpNeighbor = upNeighbor != null;
        hasDownNeighbor = downNeighbor != null;
        hasLeftNeighbor = leftNeighbor != null;
        hasRightNeighbor = rightNeighbor != null;
    }
    
    public bool HasUpNeighbor => hasUpNeighbor;

    public bool HasDownNeighbor => hasDownNeighbor;

    public bool HasLeftNeighbor => hasLeftNeighbor;

    public bool HasRightNeighbor => hasRightNeighbor;

    public override string ToString()
    {
        return $"{transform.name}: Up ({hasUpNeighbor}), Down ({hasDownNeighbor}), Left ({hasLeftNeighbor}), Right ({hasRightNeighbor})";
    }
}
