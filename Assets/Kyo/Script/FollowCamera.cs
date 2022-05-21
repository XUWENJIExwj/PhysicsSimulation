using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public GameObject FollowObject;
    public Vector3 LookAt;
    public float LookAtDistance;
    public float ViewAngle;

    // Start is called before the first frame update
    void Start()
    {
        Follow();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Follow();
    }

    public void Follow()
    {
        Quaternion rot = Quaternion.AngleAxis(ViewAngle, transform.right);
        LookAt = rot * Vector3.forward;
        LookAt = LookAt.normalized * LookAtDistance;
        transform.position = FollowObject.transform.position - LookAt.normalized * LookAtDistance;
        transform.LookAt(FollowObject.transform, Vector3.up);
    }
}
