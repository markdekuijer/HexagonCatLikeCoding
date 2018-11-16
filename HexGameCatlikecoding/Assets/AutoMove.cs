using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoMove : MonoBehaviour
{

    public float minX, maxX;
    public float minY, maxY;
    public float speed;

    private Vector2 gotoXY;

	void Start ()
    {
        GetNewCoords();
	}

    public void GetNewCoords()
    {
        print("got new coords");
        gotoXY.x = Random.Range(minX, maxX);
        gotoXY.y = Random.Range(minY, maxY);
    }

	void Update ()
    {
        print(gotoXY + " ||| " + transform.position);
        transform.position = Vector3.MoveTowards(transform.position, gotoXY, Time.deltaTime * speed);

        if (transform.position.x == gotoXY.x || transform.position.y == gotoXY.y)
            GetNewCoords();
	}
}
