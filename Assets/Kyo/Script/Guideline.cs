using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guideline : MonoBehaviour
{
    public enum ShotType
    {
        Horizontal,
        Vertical,
    }

    public ShotType Shot = ShotType.Horizontal;
    public BallPhysics GuideCreater;
    public GameObject GuideBallsParent;
    public GameObject GuideBall;
    public GameObject[] GuideBalls;
    public int MaxBall = 30;
    public int Interval = 15;
    public Vector3 Rotation = Vector3.zero;
    public float RotSpeed = 60.0f;
    public float RotXMin = -60.0f;
    public float RotXMax = 0.0f;
    public bool GuideChanged = true;
    public float DebugLineLen = 20.0f;


    void LateUpdate()
    {
        DebugLine();
    }

    // Start()での初期化
    public void InitGuideline(float power, Vector3 spin)
    {
        Rotation = transform.rotation.eulerAngles;
        GuideBalls = new GameObject[MaxBall];
        for (int i = 0; i < GuideBalls.Length; ++i)
        {
            GuideBalls[i] = Instantiate(GuideBall, GuideBallsParent.transform);
        }

        CreateGuideLine(power, spin);
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
            Quaternion rotY = Quaternion.AngleAxis(Rotation.y, Vector3.up);
            transform.rotation = rotX * rotY;
            //transform.rotation = rotX * transform.rotation;
        }

        if (Rotation.x < 0.0f)
        {
            Shot = ShotType.Vertical;
        }
        else
        {
            Shot = ShotType.Horizontal;
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

    public void CreateGuideLine(float power, Vector3 spin)
    {
        // Guidelineの向きが変わっていないなら
        // 新しいGuidelineの生成は不要
        if (!GuideChanged)
        {
            return;
        }

        GuideChanged = false;

        GuideCreater.InitPhysicsInfo(transform);
        GuideCreater.AddForceGuideline(power, transform.forward, spin);

        for (int i = 0; i < GuideBalls.Length; ++i)
        {
            for (int j = 0; j < Interval; ++j)
            {
                GuideBalls[i].transform.position = GuideCreater.UpdatePhysics();
            }
        }
    }

    public void ChangedGuide()
    {
        GuideChanged = true;
    }

    private void DebugLine()
    {
        // transform.localRotationはQuaternion
        // Quaternion * Vecで、Vecを回転させる
        Vector3 dir = transform.localRotation * Vector3.forward;
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.black);
    }
}
