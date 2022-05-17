using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public MyPhysics MyPhysic;
    public BallPhysics BallPhysic;
    public float Power;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            var angle = transform.localEulerAngles;
            angle.y -= 1.0f;
            transform.localEulerAngles = angle;
        }
        if (Input.GetKey(KeyCode.D))
        {
            var angle = transform.localEulerAngles;
            angle.y += 1.0f;
            transform.localEulerAngles = angle;
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            BallPhysic.AddForce(Power, transform.forward);
        }
    }
}
