using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guideline : MonoBehaviour
{
    public BallPhysics GuideCreater;
    public GameObject GuideBallsParent;
    public GameObject GuideBall;
    public GameObject[] GuideBalls;
    public int MaxBall = 20;
    public int Interval = 2;
    public Vector3 Rotation = Vector3.zero;
    public float RotSpeed = 1.0f;
    public float RotXMin = -60.0f;
    public float RotXMax = 0.0f;
    public bool GuideChanged = true;
    public float DebugLineLen = 20.0f;

    void LateUpdate()
    {
        DebugLine();
    }

    // Start()での初期化
    public void InitGuideline(float power)
    {
        GuideBalls = new GameObject[MaxBall];
        for (int i = 0; i < GuideBalls.Length; ++i)
        {
            GuideBalls[i] = Instantiate(GuideBall, GuideBallsParent.transform);
        }

        CreateGuideLine(power);
    }

    public void Rot()
    {
        if (Input.GetKey(KeyCode.W))
        {
            RotX(-1.0f);
        }
        if (Input.GetKey(KeyCode.S))
        {
            RotX(1.0f);
        }

        if (Input.GetKey(KeyCode.A))
        {
            RotY(-1.0f);
        }
        if (Input.GetKey(KeyCode.D))
        {
            RotY(1.0f);
        }

        if (GuideChanged)
        {
            Quaternion rotX = Quaternion.AngleAxis(Rotation.x, transform.right);
            Quaternion rotY = Quaternion.AngleAxis(Rotation.y, transform.up);
            transform.rotation = rotX * rotY;
        }
    }

    public void RotY(float sign)
    {
        GuideChanged = true;
        Rotation.y += sign * RotSpeed * Time.deltaTime;
    }

    public void RotX(float sign)
    {
        GuideChanged = true;
        Rotation.x += sign * RotSpeed * Time.deltaTime;
        Rotation.x = Mathf.Clamp(Rotation.x, RotXMin, RotXMax);
    }

    // GuidelineがBallの位置に追従させる
    public void SetPostion(Vector3 Position)
    {
        transform.position = Position;
    }

    public void CreateGuideLine(float power)
    {
        // Guidelineの向きが変わっていないなら
        // 新しいGuidelineの生成は不要
        if (!GuideChanged)
        {
            return;
        }

        GuideChanged = false;

        GuideCreater.InitPhysicsInfo(transform);
        GuideCreater.AddForceGuideline(power, transform.forward);

        for (int i = 0; i < GuideBalls.Length; ++i)
        {
            for (int j = 0; j < Interval; ++j)
            {
                GuideBalls[i].transform.position = GuideCreater.UpdatePhysics();
            }
        }
    }

    private void DebugLine()
    {
        // transform.localRotationはQuaternion
        // Quaternion * Vecで、Vecを回転させる
        Vector3 dir = transform.localRotation * Vector3.forward;
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.black);
    }
}
