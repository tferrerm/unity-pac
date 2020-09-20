using System;
using System.Collections.Generic;

public enum Direction  
{  
    Up = 0, Down = 1, Left = 2, Right = 3,
};

public static class DirectionHelper
{
    private static readonly List<Direction> OppositeXDirections = new List<Direction>(
        new [] {Direction.Left, Direction.Right});
    private static readonly List<Direction> OppositeYDirections = new List<Direction>(
        new [] {Direction.Up, Direction.Down});
    
    public static Direction GetOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return Direction.Down;
            case Direction.Down:
                return Direction.Up;
            case Direction.Left:
                return Direction.Right;
            case Direction.Right:
                return Direction.Left;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
    
    public static bool DirectionsAreOpposite(Direction direction1, Direction direction2)
    {
        if (direction1 == direction2) return false;
        
        return (OppositeXDirections.Contains(direction1) && OppositeXDirections.Contains(direction2)) ||
               (OppositeYDirections.Contains(direction1) && OppositeYDirections.Contains(direction2));
    }
}
