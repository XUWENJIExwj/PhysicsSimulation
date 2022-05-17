using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float Mass = 1.0f; // 質量
    public float SlopeLimit = 45.0f; // 登れる斜面の最大角度
    public float PotentialLostRatio = 0.1f; // 何かにぶつかる時、変形などの原因で損失するエネルギーの比率
    public float Gravity = 0.098f;
    public float Friction = 0.0f;
    public float FrictionRatio = 0.1f; // 摩擦係数
    public float RollingFrictionRatio = 0.025f; // 転がる時の摩擦力は通常の摩擦力の1/60～1/40になると言われている
    public float AirFriction = 0.01f; // 空気抵抗力
    public bool OnGround = false; // 接地フラグ
    public Transform CurrentGround; // 現在接地している平面
    public Vector3 CurrGroundNormal = Vector3.up; // 現在接地している平面の法線
    public Vector3 Acceleration = Vector3.zero; // 重力などの移動を促す外力による加速度
    public Vector3 AccelerationFriction = Vector3.zero; // 摩擦力、空気抵抗力などの移動を邪魔する外力による加速度
    public float AccelerationLen = 0.0f; // 加速度の大きさ
    public float AccelerationDeadZone = 0.00001f; // 加速度のDeadZone
    public Vector3 Velocity = Vector3.zero; // 速度
    public float VelocityLen = 0.0f; // 速度の大きさ
    public float VelocityDeadZone = 0.00001f; // 速度のDeadZone
    public Vector3 PrevPosition;
    public Ray RayMoveDir;
    public RaycastHit RayHitInfoMoveDir;
    public Ray RayOnGround;
    public RaycastHit RayHitInfoOnGround;
    public float RayLen = 100.0f;
    public SphereCollider Collider;
    public Vector3 StartPosition;
    public float DebugLineLen = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        PrevPosition = transform.position;

        RayOnGround.direction = Vector3.down;
        RayOnGround.origin = transform.position;

        // 周りのすべての平面をチェックするべき
        // 現在は真下の平面しかチェックしていない
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
    }

    // Update is called once per frame
    void Update()
    {
        DebugLine();
    }

    public void AddForce(float power, Vector3 direction)
    {
        Acceleration = direction * power / Mass;
        Velocity += Acceleration;
    }

    public void UpdatePhysics()
    {
        // 静止状態だと更新不要
        if (IsStop())
        {
            return;
        }

        // 加速度と速度の更新
        UpdateAccelerationAndVelocity();
    }

    // 加速度と速度の更新
    public void UpdateAccelerationAndVelocity()
    {
        // 加速度はマイフレーム更新するので、0に初期化
        Acceleration = Vector3.zero;
        AccelerationFriction = Vector3.zero;

        // 接地している場合
        if (OnGround)
        {
            // 現在の平面と水平面の角度のCosを求める
            float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);

            // 重力による斜面から滑る力
            // 水平面の場合、Sin = 0なので、影響を受けない
            // 斜面の場合、0 < Sin < 1、部分的に受ける
            // 垂直面の場合、 Sin = 1、影響を全部受ける
            // 方向は該当平面と水平面の法線との外積を求めたベクトルを軸に
            // 90度回転したベクトルになる
            float sin = 0.0f;
            Vector3 travelling = Vector3.zero;
            // Cos = 1の場合、水平面上にいるので、重力による滑る力がない
            // 滑る力を計算する必要がなくなる
            if (cos < 1.0f)
            {
                sin = Mathf.Sqrt(1.0f - cos * cos);
                Vector3 cross = Vector3.Cross(CurrGroundNormal, Vector3.up);
                travelling = Quaternion.AngleAxis(90.0f, cross.normalized) * CurrGroundNormal;
                travelling *= Gravity * sin;
            }
            Acceleration += travelling;

            // 重力と重力による平面からの支える力
            // その二つの力が基本相殺する、つまり、地面にめりこむような力がなくなる
            // 水平面の場合、Cos = 1なので、影響を全部受ける
            // 斜面の場合、0 < Cos < 1、部分的に受ける
            // 垂直面の場合、 Cos = 0、影響を受けない
            // その力は平面上の摩擦力に影響を与える
            // 方向は平面を移動する速度の逆方向になる
            // 現在のフレームでの速度の方向を決めてから、
            // 平面に平行する速度の方向の逆方向にかけるといい
            Friction = Gravity * cos * FrictionRatio * RollingFrictionRatio;

            // 速度を更新して摩擦力、空気抵抗力が作用する方向を取得する
            float mass = 1.0f / Mass;
            Acceleration *= mass;
            Velocity += Acceleration;

            // 空気抵抗力
            // 方向は速度の逆方向
            Vector3 airFrictionVec = -Velocity.normalized;

            // AccelerationFrictionを更新
            AccelerationFriction += airFrictionVec * AirFriction;

            // 摩擦力
            // 方向は平面と平行する
            // ボールの速度の方向は2パターン考えられる
            // 1: 平面に沿って移動する。つまり、平面と平行している
            // 2: 前のフレームでは上から平面とぶつかって、
            //    平面の法線を元に反射し、平面の上に向いている
            // どのパターンでも、速度の方向と法線との内積を求めたCosの角度をThetaに、
            // 法線と速度の方向との外積を求めたベクトルをAxisにして、
            // Axisを軸に速度の方向を(90.0f - Theta)度回転すれば、
            // 平面に平行する速度の方向を求められる
            float dot = Vector3.Dot(Velocity.normalized, CurrGroundNormal);
            float theta = 90.0f - Mathf.Acos(dot) * Mathf.Rad2Deg;
            Vector3 axis = Vector3.Cross(CurrGroundNormal, Velocity.normalized);
            Vector3 frictionVec = Quaternion.AngleAxis(theta, axis.normalized) * Velocity.normalized;

            // AccelerationFrictionを更新
            AccelerationFriction += frictionVec * Friction;
            AccelerationFriction *= mass;

            // AccelerationFrictionを更新できたら、速度を更新する
            Velocity += AccelerationFriction;

            // 全ての力が合成する加速度を保存しておく
            // 停止状態に利用される
            Acceleration += AccelerationFriction;
        }
        // 接地していない場合
        else
        {

        }
    }

    // 静止条件：速度が速度のDeadZone以下、前フレームの加速度が加速度のDeadZone以下
    public bool IsStop()
    {
        return VelocityLen <= VelocityDeadZone && AccelerationLen <= AccelerationDeadZone;
    }

    private void DebugLine()
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
}
