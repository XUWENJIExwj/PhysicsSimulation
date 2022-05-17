using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPhysics : MonoBehaviour
{
    public float Mass = 1.0f;
    public float SlopeLimit = 45.0f;
    public float PotentialLostRatio = 0.1f; // 何かにぶつかる時、変形などの原因で損失するエネルギーの比率
    public float Gravity = 0.098f;
    public float Friction = 0.1f;
    public float RollingFrictionRatio = 0.025f;
    public float AirFriction = 0.01f;
    public bool OnGround = false;
    public Transform CurrentGround;
    public Vector3 CurrGroundNormal = Vector3.up;
    public Vector3 Acceleration = Vector3.zero;
    public Vector3 AccelerationFriction = Vector3.zero;
    public Vector3 Velocity = Vector3.zero;
    public float VelocityDeadZone = 0.00001f;
    public float VelocityLen = 0.0f;
    public Vector3 PrevPosition;
    public Ray RayMoveDir;
    public Ray RayOnGround;
    public float RayLen = 100.0f;
    public RaycastHit RayHitInfoMoveDir;
    public RaycastHit RayHitInfoOnGround;
    public SphereCollider Collider;
    public Vector3 StartPosition;
    public float DebugLineLen = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        PrevPosition = transform.position;

        RayMoveDir.direction = Vector3.zero;
        RayMoveDir.origin = transform.position;

        RayOnGround.direction = Vector3.down;
        RayOnGround.origin = transform.position;

        if (Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        {
            if (RayHitInfoOnGround.distance <= Collider.radius)
            {
                CurrentGround = RayHitInfoOnGround.transform;
                CurrGroundNormal = RayHitInfoOnGround.normal;
                OnGround = true;
            }
            else
            {
                AddForce(Gravity, Vector3.down);
            }
        }
        

        //RayCastMoveDir();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        //{
        //    Debug.Log("Theta: " + Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(RayHitInfoOnGround.normal, Vector3.up)));
        //}
        DebugLine();
        UpdatePhysics();
    }

    void DebugLine()
    {
        // transform.localRotationはQuaternion
        // Quaternion * Vecで、Vecを回転させる
        Vector3 dir = transform.localRotation * Vector3.forward;
        //dir = Quaternion.FromToRotation(Vector3.up, Quaternion.Euler(-45.0f, 0.0f, 0.0f) * Vector3.up) * Vector3.forward;

        // Forwad
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.blue);
        // Back
        Debug.DrawLine(transform.position, transform.position + -dir * DebugLineLen, Color.red);

        dir = transform.localRotation * Vector3.right;

        //Right
        Debug.DrawLine(transform.position, transform.position + dir * DebugLineLen, Color.green);
        //Left
        Debug.DrawLine(transform.position, transform.position + -dir * DebugLineLen, Color.yellow);
    }

    public void UpdatePhysics()
    {
        UpdateAcceleration(); 
    }

    public void RayCastMoveDir()
    {
        RayMoveDir.direction = Vector3.Normalize(Acceleration);
        RayMoveDir.origin = transform.position;
        Physics.Raycast(RayMoveDir, out RayHitInfoMoveDir, RayLen, LayerMask.GetMask("Stage"));
        StartPosition = transform.position;
    }

    public void AddForce(float power, Vector3 direction)
    {
        Acceleration = direction * power / Mass;
        Velocity += Acceleration;
    }

    public void UpdateAcceleration()
    {
        // 接地している
        if (OnGround)
        {
            // 現在の平面と水平面の角度を求める
            float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);
            Vector3 axis = Vector3.Cross(Vector3.up, CurrGroundNormal);

            var back = -Vector3.Normalize(Velocity);
            // 摩擦力
            AccelerationFriction = back * Gravity * cos * RollingFrictionRatio;

            // 空気抵抗力
            AccelerationFriction += back * AirFriction;

            // 重力加速度
            float sin = 1.0f - cos * cos;
            if (sin > 0.0f)
            {
                sin = Mathf.Sqrt(sin);
                Vector3 gravityVec = Quaternion.AngleAxis(90.0f, axis) * CurrGroundNormal;
                AccelerationFriction += gravityVec * Gravity * sin;
            }
        }
        // 接地していない
        else
        {
            //if (Mathf)
            var back = -Vector3.Normalize(Velocity);

            if (Velocity.magnitude <= VelocityDeadZone)
            {
                back = Vector3.down;
            }

            // 空気抵抗力
            AccelerationFriction = back * AirFriction;

            // 重力加速度
            AccelerationFriction += Vector3.down * Gravity;
        }

        AccelerationFriction *= 1.0f / Mass;

        //Ray ray = new Ray(transform.position, Vector3.down);

        //RaycastHit rayHitInfo;
        //Physics.Raycast(ray, out rayHitInfo, RayLen, LayerMask.GetMask("Stage"));
        //if (rayHitInfo.distance <= Collider.radius && AccelerationFriction.y < 0.0f)
        ////if ((Mathf.Abs(Velocity.y) > 0.0f) && (Mathf.Abs(Velocity.y) < Gravity))
        //{
        //    //AccelerationFriction.y = 0.0f;
        //    //Velocity.y = 0.0f;
        //    var pos = transform.position;
        //    pos = RayHitInfoOnGround.point + RayHitInfoOnGround.normal * Collider.radius;
        //    transform.position = pos;
        //}

        Velocity += AccelerationFriction;
        Move();
    }

    public void Move()
    {
        Vector3 velocity = Velocity;
        VelocityLen = Length2(velocity);
        // 実際の物理であれば、合力の方向が真上で、重力より小さい場合のみ、停止する
        // 今回のゲームでは速度がVelocityDeadZoneより小さく、平面の上にあれば、停止になれる
        if (VelocityLen <= VelocityDeadZone && Vector3.Dot(CurrGroundNormal, Vector3.up) >= 1.0f)
        {
            Velocity = Vector3.zero;
            return;
        }
        PrevPosition = transform.position;
        transform.position += velocity * Time.deltaTime;

        if (CheckOnGroundWhenOnGround())
        {
            var pos = transform.position;
            pos = RayHitInfoOnGround.point + RayHitInfoOnGround.normal * Collider.radius;
            transform.position = pos;
        }

        //float distanceDiff = RayHitInfoMoveDir.distance - Collider.radius;
        //if (CheckHit(distanceDiff))
        //{
        //    transform.position = StartPosition + RayMoveDir.direction * distanceDiff;
        //    Velocity *= -1 * (1.0f - PotentialLost);
        //}
    }

    public bool CheckHit(float distanceDiff)
    {
        Vector3 dir = Vector3.Normalize(Velocity);
        float distance2Move = Distance2(transform.position, StartPosition);

        OnGround = distance2Move >= distanceDiff * distanceDiff;

        return OnGround;
    }

    public bool CheckOnGroundWhenOnGround()
    {
        OnGround = false;

        // 進行先の平面チェック
        VelocityLen = Length(Velocity);
        Vector3 norVelocity = Velocity * (1.0f / VelocityLen);
        RayOnGround.direction = norVelocity;
        RayOnGround.origin = transform.position;
        // 進行の方向でRayCastする
        if (Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        {
            // 進行先に平面がある場合、その平面の法線の逆方向をRayとして、RayCastで衝突目標を取得
            // その目標が先のRayCastでの衝突目標と同じであれば、間もなく衝突と判断できる
            // CurrentGroundとCurrGroundNormalの更新準備に入る
            var targetTransform = RayHitInfoOnGround.transform;
            RayOnGround.direction = -RayHitInfoOnGround.normal;

            // 進行先の平面の法線の逆方向でRayCastする
            Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage"));
            if (targetTransform == RayHitInfoOnGround.transform)
            {
                // 場合によって、Velocityの方向でRayCastをして、
                // 取得した衝突ポイントは自身にとって、
                // 実際の衝突ポイントではなく、その衝突ポイントに到達する前にすでに衝突している
                // 例：Z軸の奥に、法線がVector3(0.0f, 0.7XXXf, 0.7XXXf)平面がある
                // 　　Vector3(1.0f, 0.0f, 1.0f)の方向をそって、進める場合、
                // 　　Vector3(0.0f, 0.0f, 1.0f)の方向でRaycastをした方が、
                // 　　最終的に、実際の衝突ポイントを取得することができる
                // 平面の法線をもとに、Raycastの方向を正しく設定するために、
                // 進行先の平面が現在の接地している平面とのなす角度が90度となるように、
                // 進行先の平面を回転すれば、回転後の平面の法線の逆方向がRaycastの方向である

                // 回転軸を求める
                Vector3 axis = Vector3.Normalize(Vector3.Cross(CurrGroundNormal, RayHitInfoOnGround.normal));
                // 現在の平面間の角度を求める
                float cos = Vector3.Dot(CurrGroundNormal, RayHitInfoOnGround.normal);
                float angle = Mathf.Rad2Deg * Mathf.Acos(cos);
                // 法線をaxisを軸に(90.0f - angle)回転させる
                Vector3 normal = -Vector3.Normalize(Quaternion.AngleAxis(90.0f - angle, axis) * RayHitInfoOnGround.normal);
                Debug.DrawLine(transform.position, transform.position + normal * DebugLineLen, Color.black);

                // 進行先の平面と衝突したら、Velocityの向きをaxisを軸に、angle度回転させる
                if (RayHitInfoOnGround.distance <= Collider.radius)
                {
                    var reflect = Vector3.Reflect(Velocity, RayHitInfoOnGround.normal);
                    var reflectDotNormal = Vector3.Dot(reflect.normalized, RayHitInfoOnGround.normal);
                    var cosSlopeLimit = Mathf.Cos(SlopeLimit * Mathf.Deg2Rad);
                    if (reflectDotNormal > cosSlopeLimit + 0.0001f)
                    {
                        Velocity = reflect;
                        if (Mathf.Abs(Velocity.y) < Gravity)
                        {
                            OnGround = true;
                        }
                    }
                    else
                    {
                        Velocity = Quaternion.AngleAxis(angle, axis) * norVelocity * VelocityLen;
                        CurrentGround = RayHitInfoOnGround.transform;
                        CurrGroundNormal = RayHitInfoOnGround.normal;
                        OnGround = true;
                    }

                    Velocity *= 1.0f - PotentialLostRatio;

                    Debug.DrawLine(transform.position, transform.position + norVelocity * DebugLineLen, Color.gray);

                    return OnGround;
                }
            }
        }

        // 真下にある平面チェック
        RayOnGround.direction = -CurrGroundNormal;
        RayOnGround.origin = transform.position;
        Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage"));
        if (RayHitInfoOnGround.distance <= Collider.radius)
        {
            CurrentGround = RayHitInfoOnGround.transform;
            CurrGroundNormal = RayHitInfoOnGround.normal;

            if (Mathf.Abs(Velocity.y) > 0.0f)
            {
                Velocity.y *= -1;
                Velocity *= 1.0f - PotentialLostRatio;
            }
            OnGround = true;
        }

        return OnGround;
    }

    public float Distance2(Vector3 pointA, Vector3 pointB)
    {
        Vector3 v = pointA - pointB;
        return v.x * v.x + v.y * v.y + v.z * v.z;
    }

    public float Length2(Vector3 vector)
    {
        return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
    }

    public float Length(Vector3 vector)
    {
        return Mathf.Sqrt(Length2(vector));
    }

    // 平面と水平面のなす角度
    public float ThetaBetweenPlanes(Vector3 normal)
    {
        return Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(normal, Vector3.up));
    }

    public bool isStop()
    {
        return OnGround && Length2(Velocity) <= VelocityDeadZone;
    }
}
