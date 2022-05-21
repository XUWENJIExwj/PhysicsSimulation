using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallPhysics BallPhysic;
    public Guideline Guide;
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
            var angle = Guide.transform.localEulerAngles;
            angle.y -= 1.0f;
            Guide.transform.localEulerAngles = angle;
        }
        if (Input.GetKey(KeyCode.D))
        {
            var angle = Guide.transform.localEulerAngles;
            angle.y += 1.0f;
            Guide.transform.localEulerAngles = angle;
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            BallPhysic.AddForce(Power, Guide.transform.forward);
        }
    }

    private void LateUpdate()
    {
        Guide.SetPostion(BallPhysic.transform.position);
    }
}
