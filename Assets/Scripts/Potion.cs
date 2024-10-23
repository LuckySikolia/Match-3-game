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

    //move to target ie moving the potion around
    public void MoveToTarget(Vector2 _targetPos)
    {
        StartCoroutine(MoveCoroutine(_targetPos));
    }

    //move coroutine
    public IEnumerator MoveCoroutine(Vector2 _targetPos)
    {
        isMoving = true;
        float duration = 0.2f;

        Vector2 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {

            float t = elapsedTime / duration;

            transform.position = Vector2.Lerp(startPosition, _targetPos, t);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        transform.position = _targetPos;
        isMoving = false;
    }
}

public enum PortionType
{
    Red, 
    Blue,
    Green,
    Purple,
    White
}
