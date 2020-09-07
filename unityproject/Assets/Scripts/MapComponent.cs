using UnityEngine;

public class MapComponent : MonoBehaviour
{
    private bool isWall;
    private bool isBoxDoor;
    private bool hasPellet;
    private bool hasPowerPellet;

    public bool IsWall
    {
        get => isWall;
        set => isWall = value;
    }

    public bool IsBoxDoor
    {
        get => isBoxDoor;
        set => isBoxDoor = value;
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
        return !isWall && !isBoxDoor && !hasPellet && !hasPowerPellet;
    }
}
