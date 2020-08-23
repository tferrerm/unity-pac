using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapComponent : MonoBehaviour
{
    private bool isWall;
    private bool hasPellet;
    private bool hasPowerPellet;

    public bool IsWall
    {
        get => isWall;
        set => isWall = value;
    }

    public bool HasPellet
    {
        get => hasPellet;
        set => hasPellet = value;
    }

    public bool HasPowerPellet
    {
        get => hasPowerPellet;
        set => hasPowerPellet = value;
    }

    public void EatPellet()
    {
        hasPellet = false;
        hasPowerPellet = false;
    }

    public bool IsBlank()
    {
        return !isWall && !hasPellet && !hasPowerPellet;
    }
}
