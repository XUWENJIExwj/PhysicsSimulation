using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guideline : MonoBehaviour
{
    public float DebugLineLen = 20.0f;
    public int MaxBall = 20;
    public int Interval = 2;
    public BallPhysics GuideCreater;
    public GameObject GuideBallsParent;
    public GameObject GuideBall;
    public GameObject[] GuideBalls;

    void LateUpdate()
    {
        DebugLine();
    }

    // Start()Ç≈ÇÃèâä˙âª
    public void InitGuideline(float power)
    {
        GuideBalls = new GameObject[MaxBall];
        for (int i = 0; i < GuideBalls.Length; ++i)
        {
            GuideBalls[i] = Instantiate(GuideBall, GuideBallsParent.transform);
        }

        CreateGuideLine(power);
    }

    // GuidelineÇ™BallÇÃà íuÇ…í«è]Ç≥ÇπÇÈ
    public void SetPostion(Vector3 Position)
    {
        transform.position = Position;
    }

    public void CreateGuideLine(float power)
    {
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
        // transform.localRotationÇÕQuaternion
        // Quaternion * VecÇ≈ÅAVecÇâÒì]Ç≥ÇπÇÈ
        Vector3 dir = transform.localRotation * Vector3.forward;
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.black);
    }
}
