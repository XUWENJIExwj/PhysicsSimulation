using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallPhysics BallPhysic;
    public Guideline Guide;
    public float Power = 15.0f;
    public Vector3 SpinStep = Vector3.zero;
    public Vector3 SpinPower = new Vector3(20.0f, 1.0f, 1.0f); //x: 水平Shot時の横スピン回転値 y: 空中Shot時の縦スピンの段階 z: 空中Shot時の横スピンの回転値
    public float MaxSpinStep = 6.0f;

    // Start is called before the first frame update
    void Start()
    {
        Guide.InitGuideline(Power, ComputeSpinPower());
        BallPhysic.InitPhysicsInfo(Guide.transform);
    }

    // Update is called once per frame
    void Update()
    {
        ChangeSpin();
        Guide.Rot();
        Guide.CreateGuideLine(Power, ComputeSpinPower());

        if (Input.GetKeyDown(KeyCode.J))
        {
            BallPhysic.AddForce(Power, Guide.transform.forward, ComputeSpinPower());
        }

        BallPhysic.UpdatePhysics();
    }

    public void ChangeSpin()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SpinStep.y += 1.0f;
            SpinStep.y = Mathf.Clamp(SpinStep.y, -MaxSpinStep, MaxSpinStep);
            Guide.ChangedGuide();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SpinStep.y -= 1.0f;
            SpinStep.y = Mathf.Clamp(SpinStep.y, -MaxSpinStep, MaxSpinStep);
            Guide.ChangedGuide();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SpinStep.x -= 1.0f;
            SpinStep.x = Mathf.Clamp(SpinStep.x, -MaxSpinStep, MaxSpinStep);
            SpinStep.z = SpinStep.x;
            Guide.ChangedGuide();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SpinStep.x += 1.0f;
            SpinStep.x = Mathf.Clamp(SpinStep.x, -MaxSpinStep, MaxSpinStep);
            SpinStep.z = SpinStep.x;
            Guide.ChangedGuide();
        }
    }

    public Vector3 ComputeSpinPower()
    {
        Vector3 spin = SpinStep;
        spin.x *= SpinPower.x;
        spin.y *= SpinPower.y;
        spin.z *= SpinPower.z;
        return spin;
    }

    private void LateUpdate()
    {
        Guide.SetPostion(BallPhysic.transform.position);
    }
}
