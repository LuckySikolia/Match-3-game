using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Potion : MonoBehaviour
{
    public PortionType portionType;


    public int xIndex;
    public int yIndex;

    public bool isMatched;
    public bool isMoving;

    private Vector2 currentPos;
    private Vector2 targetPos;

    //a constructor
    public Potion(int _x, int _y)
    {
        xIndex = _x;
        yIndex = _y;    
    }

    public void SetIndicies(int _x, int _y)
    {
        xIndex = _x;
        yIndex = _y;
    }

    //move to target

    //move coroutine

}

public enum PortionType
{
    Red, 
    Blue,
    Green,
    Purple,
    White
}
