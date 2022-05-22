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
        Guide.InitGuideline(Power);
        BallPhysic.InitPhysicsInfo(Guide.transform);
    }

    // Update is called once per frame
    void Update()
    {
        Guide.Rot();

        if (Input.GetKeyDown(KeyCode.J))
        {
            BallPhysic.AddForce(Power, Guide.transform.forward);
        }

        BallPhysic.UpdatePhysics();
    }

    private void LateUpdate()
    {
        Guide.SetPostion(BallPhysic.transform.position);
        Guide.CreateGuideLine(Power);
    }
}
