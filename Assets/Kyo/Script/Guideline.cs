using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guideline : MonoBehaviour
{
    public float DebugLineLen = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DebugLine();
    }

    public void SetPostion(Vector3 Position)
    {
        transform.position = Position;
    }

    private void DebugLine()
    {
        // transform.localRotationÇÕQuaternion
        // Quaternion * VecÇ≈ÅAVecÇâÒì]Ç≥ÇπÇÈ
        Vector3 dir = transform.localRotation * Vector3.forward;
        // Forwad
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.blue);
      
        dir = transform.localRotation * Vector3.right;
        // Right
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.green);

        dir = transform.localRotation * Vector3.up;
        // Up
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.yellow);
    }
}
