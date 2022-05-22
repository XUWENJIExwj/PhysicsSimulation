using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float Mass = 1.0f; // 質量
    public float SlopeLimit = 45.0f; // 登れる斜面の最大角度
    public float PotentialLostRatio = 0.15f; // 何かにぶつかる時、変形などの原因で損失するエネルギーの比率
    public float Gravity = 0.098f;
    public float Friction = 0.0f;
    public float FrictionRatio = 0.1f; // 摩擦係数
    public float RollingFrictionRatio = 0.025f; // 転がる時の摩擦力は通常の摩擦力の1/60～1/40になると言われている
    public float AirFriction = 0.01f; // 空気抵抗力
    public bool OnGround = false; // 接地フラグ
    public bool OnHit = false; // 進行先の平面との衝突フラグ
    public Transform PrevGround; // 前に接地している平面
    public Vector3 PrevGroundNormal = Vector3.up; // 前に接地している平面の法線
    public Transform CurrGround; // 現在接地している平面
    public Vector3 CurrGroundNormal = Vector3.up; // 現在接地している平面の法線
    public Transform NextGround; // 次に接地している平面
    public Vector3 NextGroundNormal = Vector3.up; // 次に接地している平面の法線
    public Vector3 Acceleration = Vector3.zero; // 重力などの移動を促す外力による加速度
    public float AccelerationLen = 0.0f; // 重力などの移動を促す外力による加速度の大きさ
    public Vector3 AccelerationFriction = Vector3.zero; // 摩擦力、空気抵抗力などの移動を邪魔する外力による加速度
    public float AccelerationFrictionLen = 0.0f; // 摩擦力、空気抵抗力などの移動を邪魔する外力による加速度の大きさ
    public float AccelerationDeadZone = 0.0001f; // 加速度のDeadZone
    public Vector3 LinearVelocity = Vector3.zero; // 速度
    public float LinearVelocityLen = 0.0f; // 速度の大きさ
    public float LinearVelocityDeadZone = 0.0001f; // 速度のDeadZone
    public Vector3 AngularVelocityAxis = Vector3.zero; // 角回転の軸
    public Vector3 AngularVelocity = Vector3.zero; // 角速度
    public float AngularVelocityLen = 0.0f; // 角速度の大きさ
    public float AngularVelocityDeadZone = 0.1f; // 角速度のDeadZone
    public Vector3 SpinPower = Vector2.zero; // スピンが働く力
    public bool VerticalShot = false; // 空中を飛ばすShotであるか
    public Quaternion LinearVelocityRotation; // 進行中のRotation
    public Vector3 LinearFoward;
    public Vector3 LinearRight;
    public Vector3 LinearUp;
    public Vector3 PrevPosition;
    public Ray RayMoveDir;
    public RaycastHit RayHitInfoMoveDir;
    public Ray RayOnGround;
    public RaycastHit RayHitInfoOnGround;
    public float RayLen = 10.0f;
    public SphereCollider Collider;
    public Vector3 StartPosition;
    public float Bias = 0.001f;
    public float DebugLineLen = 2.0f;

    private void LateUpdate()
    {
        DebugLine();
    }

    // Guidelineの方向が変わった時、呼び出す必要がある
    public void InitPhysicsInfo(Transform target)
    {
        PrevPosition = target.position;
        transform.position = target.position;

        RayOnGround.direction = Vector3.down;
        RayOnGround.origin = target.position;

        LinearFoward = target.forward;
        LinearRight = target.right;
        LinearUp = target.up;

        // 周りのすべての平面をチェックするべき
        // 現在は真下の平面しかチェックしていない
        if (Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        {
            SetNextGroundInfo(RayHitInfoOnGround.transform, RayHitInfoOnGround.normal);
            if (RayHitInfoOnGround.distance <= Collider.radius + Bias)
            {
                // 接地情報を更新する
                SetGroundInfo(RayHitInfoOnGround.transform, RayHitInfoOnGround.normal);
                OnGround = true;
            }
            else
            {
                AddForce(Gravity, Vector3.down, SpinPower);
            }
        }
    }

    public void AddForce(float power, Vector3 direction, Vector3 spin)
    {
        Acceleration = direction * power / Mass;
        LinearVelocity += Acceleration;
        LinearVelocityLen = LinearVelocity.magnitude;
        SpinPower = spin;
        // 空中に飛ばすShotであるか
        if (Vector3.Dot(Acceleration, Vector3.up) > 0.0f)
        {
            VerticalShot = true;
        }
    }

    public void AddForceGuideline(float power, Vector3 direction, Vector3 spin)
    {
        Acceleration = direction * power / Mass;
        LinearVelocity = Acceleration;
        LinearVelocityLen = LinearVelocity.magnitude;
        SpinPower = spin;
        // 空中に飛ばすShotであるか
        if (Vector3.Dot(Acceleration, Vector3.up) > 0.0f)
        {
            VerticalShot = true;
        }
    }


    public Vector3 UpdatePhysics()
    {
        // 静止状態だと更新不要
        if (IsStop())
        {
            return transform.position;
        }

        // 加速度と速度の更新
        UpdateAccelerationAndVelocity();

        // 回転の更新
        UpdateAngular();

        // 位置情報、接地情報を更新する
        Vector3 pos = UpdatePosition();
        return pos;
    }

    // 加速度と速度の更新
    // 速度は先に移動を促す外力で処理する
    // その後、移動を邪魔する外力で処理する
    // 2段階になっているので、それぞれの値を保存しておくと
    // それぞれ取り出して使うことも可能
    public void UpdateAccelerationAndVelocity()
    {
        // 加速度はマイフレーム更新するので、0に初期化
        Acceleration = Vector3.zero;
        AccelerationFriction = Vector3.zero;
        float mass = 1.0f / Mass;

        // 接地している場合
        if (OnGround)
        {
            // 接地している時、横スピンが常に機能するので、先に横スピンをかける
            Quaternion rot = Quaternion.AngleAxis(SpinPower.x * Time.deltaTime, CurrGroundNormal);
            LinearVelocity = rot * LinearVelocity;
            // 横スピンも減衰させる
            SpinPower.x *= 1 - PotentialLostRatio * Time.deltaTime;

            // 現在の平面と水平面の角度のCosを求める
            float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);

            // 重力による斜面から滑る力
            // 水平面の場合、Sin = 0なので、影響を受けない
            // 斜面の場合、0 < Sin < 1、部分的に受ける
            // 垂直面の場合、 Sin = 1、影響を全部受ける
            float sin = 0.0f;
            Vector3 travelling = Vector3.zero;
            // Cos = 1の場合、水平面上にいるので、重力による滑る力がない
            // 滑る力を計算する必要がなくなる
            if (cos < 1.0f - Bias)
            {
                sin = Mathf.Sqrt(1.0f - cos * cos);
                // 水平面の法線と現在の平面の法線の外積を求める
                travelling = Vector3.Cross(Vector3.up, CurrGroundNormal);
                // 求めた外積と現在の平面の法線の外積を求める
                // 最後に求めた外積は滑る力の方向となる
                travelling = Vector3.Cross(travelling.normalized, CurrGroundNormal);
                travelling *= Gravity * sin;
            }
            Acceleration += travelling * mass;
            AccelerationLen = Acceleration.magnitude;

            // 速度を更新して摩擦力、空気抵抗力が作用する方向を取得する
            LinearVelocity += Acceleration;
            LinearVelocityLen = LinearVelocity.magnitude;

            // 空気抵抗力の計算
            AccelerationFriction += ComputeAirFriction(mass);

            // 水平面にいて、更新後に停止と判断した場合
            // 速度と加速度全部0にして、停止状態にする
            if (CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + AccelerationFriction))
            {
                return;
            }

            // 摩擦力の計算
            AccelerationFriction += ComputeFriction(mass, cos);
            AccelerationFrictionLen = AccelerationFriction.magnitude;

            // 水平面にいて、更新後に停止と判断した場合
            // 速度と加速度全部0にして、停止状態にする
            if (CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + AccelerationFriction))
            {
                return;
            }

            // AccelerationFrictionを更新できたら、速度を更新する
            LinearVelocity += AccelerationFriction;

            // Ballが転がる間も、変形するので、エネルギー損失率をかける
            LinearVelocity *= 1 - PotentialLostRatio * Time.deltaTime;
            LinearVelocityLen = LinearVelocity.magnitude;
        }
        // 接地していない場合
        else
        {
            // 重力
            // 方向は真下
            Acceleration += Vector3.down * Gravity;
            AccelerationLen = Acceleration.magnitude;

            // 速度を更新して空気抵抗力が作用する方向を取得する
            LinearVelocity += Acceleration;
            LinearVelocityLen = LinearVelocity.magnitude;

            // 空気抵抗力
            // 方向は速度の逆方向
            AccelerationFriction += ComputeAirFriction(mass);
            AccelerationFrictionLen = AccelerationFriction.magnitude;

            // AccelerationFrictionを更新できたら、速度を更新する
            LinearVelocity += AccelerationFriction;
            LinearVelocityLen = LinearVelocity.magnitude;
        }
    }

    // 空気抵抗力の計算
    public Vector3 ComputeAirFriction(float mass)
    {
        // 空気抵抗力
        // 方向は速度の逆方向
        Vector3 airFrictionVec = -LinearVelocity.normalized;

        return airFrictionVec * AirFriction * mass;
    }

    public Vector3 ComputeFriction(float mass, float cos)
    {
        // 重力と重力による平面からの支える力
        // その二つの力が基本相殺する、つまり、地面にめりこむような力がなくなる
        // 水平面の場合、Cos = 1なので、影響を全部受ける
        // 斜面の場合、0 < Cos < 1、部分的に受ける
        // 垂直面の場合、 Cos = 0、影響を受けない
        // その力は平面上の摩擦力に影響を与える
        // 方向は平面を移動する速度の逆方向になる
        // 現在のフレームでの速度の方向を決めてから、
        // 平面に平行する速度の方向の逆方向にかけるといい
        // 現在求めたのは最大静止摩擦力
        // 球体なので、上に求めた滑る力が0出ない、つまり斜面にいる場合
        // 必ず動きだすので、Frictionを摩擦力として使える
        // 水平面にいる場合、静止タイミングは速度の大きさがDeadZone以下の時、
        // もしくは、更新後の速度の方向が更新前の速度の方向が逆になる時、
        // 摩擦力、空気抵抗力を適用後、チェックする必要がある
        Friction = Gravity * cos * FrictionRatio * RollingFrictionRatio;

        // 摩擦力
        // 方向は平面と平行する
        // ボールの速度の方向は2パターン考えられる
        // 1: 平面に沿って移動する。つまり、平面と平行している
        // 2: 前のフレームでは上から平面とぶつかって、
        //    平面の法線を元に反射し、平面の上に向いている
        // どのパターンでも、速度の方向と現在の平面の法線との外積を求める
        // 求めた外積と現在の平面の法線との外積を求める
        // 最後に求めた外積は摩擦力の方向となる
        Vector3 frictionVec = Vector3.Cross(LinearVelocity.normalized, CurrGroundNormal);
        frictionVec = Vector3.Cross(frictionVec.normalized, CurrGroundNormal);

        return frictionVec * Friction * mass;
    }

    // 位置情報、接地情報を更新する
    public Vector3 UpdatePosition()
    {
        // 速度や加速度が更新後、静止条件を満たす場合がある
        // 静止状態だと更新不要
        if (IsStop())
        {
            return transform.position;
        }

        // 位置を更新する
        PrevPosition = transform.position;
        transform.position += LinearVelocity * Time.deltaTime;

        CheckOnCurrentGround();
        CheckMoveDirectionOnHit();

        return transform.position;
    }

    // 平面にいるかをチェックする
    // RayCastで当たり判定を取る
    // 衝突後の反射処理も行う
    public bool CheckOnCurrentGround()
    {
        OnGround = false;

        // Raycastの方向を現在持っている平面の法線情報の逆方向にする
        RayOnGround.direction = -CurrGroundNormal;
        RayOnGround.origin = transform.position;

        // 現在持っている平面の法線情報でRaycastして、何も当たらなかったら、
        // この平面を通り過ぎたと判断できるので、以下の処理は不要
        if (!Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        {
            SetGroundInfo(null, Vector3.zero);
            return OnGround;
        }

        // 現在の平面との距離がBallの半径以上だと、当たらなかったと判断できる
        // それ以降の処理が不要
        if (RayHitInfoOnGround.distance > Collider.radius + Bias)
        {
            return OnGround;
        }

        // 現在の平面との距離がBallの半径以下だと、当たったと判断できる
        // 接地情報を更新する
        SetGroundInfo(RayHitInfoOnGround.transform, RayHitInfoOnGround.normal);

        // 縦スピンをかける
        LinearVelocity = ComputeVelocityWhenIsVerticalShot(RayHitInfoOnGround.normal);

        // 速度の方向を平面の法線をもとに反射する
        // 速度の方向が平面と平行している場合、反射しても変わらない
        // 速度の方向が平面と平行していない場合、平面に突入してきたことがわかる
        // そのため、反射後の速度の方向は地面から離れる
        LinearVelocity = Vector3.Reflect(LinearVelocity, CurrGroundNormal);

        // 平面に突入してきた場合のみ、エネルギーの損失を反映する
        // 速度の方向と平面の法線の内積で、損失率を0～PotentialLostRatioにする
        float dot = Vector3.Dot(LinearVelocity.normalized, CurrGroundNormal);
        LinearVelocity *= 1 - PotentialLostRatio * dot;
        LinearVelocityLen = LinearVelocity.magnitude;

        // 速度が反射によって変更されたので、その場で停止状態に満たすかをチェック
        // 現在の平面と水平面の角度のCosを求める
        float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);
        float mass = 1.0f / Mass;

        // 重力を計算し、停止状態をチェック
        Vector3 gravity = Vector3.down * Gravity;
        CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + gravity);

        // 空気抵抗力を計算し、停止状態をチェック
        Vector3 airfriction = ComputeAirFriction(mass);
        CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + airfriction);

        // 摩擦力を計算し、停止状態をチェック
        Vector3 friction = ComputeFriction(mass, cos);
        CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + friction);

        // めり込みを補正する
        var pos = transform.position;
        pos = RayHitInfoOnGround.point + RayHitInfoOnGround.normal * Collider.radius;
        transform.position = pos;

        OnGround = true;

        return OnGround;
    }

    // 進行先のあたりチェック
    public bool CheckMoveDirectionOnHit()
    {
        OnHit = false;

        RayMoveDir.direction = LinearVelocity.normalized;
        RayMoveDir.origin = transform.position;

        // 進行方向と接地チェック時のRaycast方向が同じであるかをチェック
        // 同じの場合、すでにチェック済みなので、再チェックが不要
        float dot = Vector3.Dot(RayMoveDir.direction, RayOnGround.direction);
        if (dot >= 1.0f - Bias)
        {
            OnHit = OnGround;
            return OnHit;
        }

        // Raycastする
        // 進行先に平面などがない場合、それ以降の処理が不要
        if (!Physics.Raycast(RayMoveDir, out RayHitInfoMoveDir, RayLen, LayerMask.GetMask("Stage")))
        {
            SetNextGroundInfo(null, Vector3.zero);
            return OnHit;
        }
        SetNextGroundInfo(RayHitInfoMoveDir.transform, RayHitInfoMoveDir.normal);

        // 進行先に平面がある場合、その平面の法線の逆方向をRayとして、RayCastで衝突する平面を取得
        Transform targetTransform = RayHitInfoMoveDir.transform;
        RayMoveDir.direction = -RayHitInfoMoveDir.normal;

        // 進行先の平面の法線の逆方向でRayCastする
        Physics.Raycast(RayMoveDir, out RayHitInfoMoveDir, RayLen, LayerMask.GetMask("Stage"));

        // 衝突する平面が先のRayCastでの衝突する平面と同じでなければ、これ以降の処理が不要になる
        if (targetTransform != RayHitInfoMoveDir.transform)
        {
            return OnHit;
        }

        // 衝突する平面との距離がBallの半径以上だと、当たらなかったと判断できる
        // それ以降の処理が不要
        if (RayHitInfoMoveDir.distance > Collider.radius + Bias)
        {
            return OnHit;
        }

        // 速度の反射更新
        UpdateVelocityAfterHit();

        // めり込みを補正する
        var pos = transform.position;
        pos = RayHitInfoMoveDir.point + RayHitInfoMoveDir.normal * Collider.radius;
        transform.position = pos;

        OnHit = true;

        return OnHit;
    }

    // 速度の反射更新
    public void UpdateVelocityAfterHit()
    {
        // 縦スピンをかける
        LinearVelocity = ComputeVelocityWhenIsVerticalShot(RayHitInfoMoveDir.normal);

        // 衝突した平面が先のRayCastでの衝突する平面と同じであれば、間もなく衝突と判断できる
        // 登れる平面の最大角度のcosを求める
        // 衝突した平面の法線をもとに、進行方向を反射させる
        // 反射後のベクトルと衝突した平面の法線との内積を求める
        // 求めた内積が求めたcosより小さい場合、平面に登れると判断できる
        // 衝突した平面が水平面の場合、そのまま反射処理をする
        // どちらも、平面と衝突したので、cosをもって損失率を0～PotentialLostRatioにする
        float cos = Mathf.Cos(SlopeLimit * Mathf.Deg2Rad);
        LinearVelocity *= 1 - PotentialLostRatio * cos;
        Vector3 reflectVel = Vector3.Reflect(LinearVelocity, RayHitInfoMoveDir.normal);
        float dot = Vector3.Dot(reflectVel.normalized, RayHitInfoMoveDir.normal);
        // 登れる場合、CurrentGroundとCurrGroundNormalの更新準備に入る
        if (dot <= cos + Bias)
        {
            // 衝突した平面に登る瞬間速度の大きさはそのまま、
            // 方向が平面と平行するように変わる
            // 速度の回転軸は現在の平面と衝突した平面との外積になる
            // 現在の平面と衝突した平面との角度を内積で求める
            // 速度を求めた外積を軸に、求めた角度分回転させると、
            // 平面を登れた後の速度となる
            Vector3 axis = Vector3.Cross(CurrGroundNormal, RayHitInfoMoveDir.normal);
            cos = Vector3.Dot(CurrGroundNormal, RayHitInfoMoveDir.normal);
            float angle = Mathf.Rad2Deg * Mathf.Acos(cos);
            LinearVelocity = Quaternion.AngleAxis(angle, axis.normalized) * LinearVelocity;
            LinearVelocityLen = LinearVelocity.magnitude;

            // 速度が回転によって変更されたので、その場で停止状態に満たすかをチェック
            // 衝突した平面と水平面の角度のCosを求める
            cos = Vector3.Dot(RayHitInfoMoveDir.normal, Vector3.up);
            float mass = 1.0f / Mass;

            // 重力を計算し、停止状態をチェック
            Vector3 gravity = Vector3.down * Gravity;
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + gravity);

            // 空気抵抗力を計算し、停止状態をチェック
            Vector3 airfriction = ComputeAirFriction(mass);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + airfriction);

            // 摩擦力を計算し、停止状態をチェック
            Vector3 friction = ComputeFriction(mass, cos);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + friction);

            // 接地情報を更新する
            SetGroundInfo(RayHitInfoMoveDir.transform, RayHitInfoMoveDir.normal);
            OnGround = true;
        }
        // 登れない場合、反射処理
        else
        {
            // 速度の反射
            LinearVelocity = reflectVel;
            LinearVelocityLen = LinearVelocity.magnitude;

            // 速度が反射によって変更されたので、その場で停止状態に満たすかをチェック
            // 衝突した平面と水平面の角度のCosを求める
            cos = Vector3.Dot(RayHitInfoMoveDir.normal, Vector3.up);
            float mass = 1.0f / Mass;

            // 空気抵抗力を計算し、停止状態をチェック
            Vector3 airfriction = ComputeAirFriction(mass);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + airfriction);

            // 摩擦力を計算し、停止状態をチェック
            Vector3 friction = ComputeFriction(mass, cos);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + friction);

            // 平面が垂直面の場合、反射しても重力をチェックする必要がない
            // 接地情報を更新する必要もない
            // つまり、cos > 0.0fの場合（垂直面ではない）のみ、チェックand更新
            if (cos > 0.0f + Bias)
            {
                // 重力を計算し、停止状態をチェック
                Vector3 gravity = Vector3.down * Gravity;
                CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + gravity);

                // 接地情報を更新する
                SetGroundInfo(RayHitInfoMoveDir.transform, RayHitInfoMoveDir.normal);
                OnGround = false;
            }
        }

        // 衝突したらスピンをなくす
        SpinPower = Vector3.zero;
    }

    // 縦スピンがある時、衝突後の速度更新
    public Vector3 ComputeVelocityWhenIsVerticalShot(Vector3 normal)
    {
        // VerticalShotではなければ、更新不要
        if (!VerticalShot)
        {
            return LinearVelocity;
        }
        // 一回目の着地だけスピンをかける
        VerticalShot = false;

        // 衝突した瞬間に、速度の平面に平行する分速度V.xを求める
        // V: 速度
        // V.x: 平面に平行する分速度
        // V.y: 平面に垂直する分速度
        // 速度と平面の法線との内積から、V.yが求められる
        // V = V.x + V.y から V.x = V - V.y
        float dot = Vector3.Dot(LinearVelocity, normal);
        Vector3 vy = normal * dot;
        Vector3 vx = LinearVelocity + -vy;
        Vector3 spinY = vx;
        int sy = (int)Mathf.Abs(SpinPower.y);
        if (sy > 0.0f)
        {
            // V.xをFowardにSpinPower.y分、縦スピンをかける
            int maxSpinStep = 6 + 1; // 0を含めて7になる
            int ratio = maxSpinStep / (6 / 2); // -1～-3はまだ逆方向に飛ばない、-4～-6は逆方向に飛ぶ。ちょうど半分ずつ
            spinY += vx * (ratio * SpinPower.y / maxSpinStep);
        }
        
        // V.z: V.xをFoward、平面の法線をUpとした時のRight
        // Right方向にSpinPower.z分、横スピンをかける
        Vector3 vz = Vector3.Cross(normal, vx.normalized);
        Vector3 spinZ = vz.normalized * SpinPower.z;
        return spinY + spinZ + vy;
    }

    // 静止条件：
    // 速度が速度のDeadZone以下、
    // 前フレームの加速度が加速度のDeadZone以下
    public bool IsStop()
    {
        // 斜面と水平面との内積によって、VelocityDeadZoneを動的に変える
        // 斜面をのぼる時、速度の大きさが0にとても近いタイミングがある
        // その時、VelocityDeadZoneが速度の大きさを超えてしまうと、斜面に停止してしまう
        // 水平面にいる時、速度の大きさがある程度0に近づいたら、早く停止させても大丈夫
        float dot = Vector3.Dot(CurrGroundNormal, Vector3.up);
        bool linearDead = LinearVelocity.magnitude <= LinearVelocityDeadZone * dot;
        bool angularDead = AngularVelocity.magnitude <= AngularVelocityDeadZone * dot * Mathf.Rad2Deg;
        bool accelerationDead = Acceleration.magnitude <= AccelerationDeadZone * dot;
        return linearDead && angularDead && accelerationDead;
    }

    // 静止条件：
    // 水平面にいる場合、速度の大きさがDeadZone以下
    // 更新後の速度の方向が更新前の速度の方向が逆
    // 摩擦力、空気抵抗力を適用後、要チェック
    public bool IsStopAfterCancelOut(Vector3 before, Vector3 after)
    {
        bool linearDead = (after * Time.deltaTime).magnitude <= LinearVelocityDeadZone;
        bool angularDead = AngularVelocity.magnitude <= AngularVelocityDeadZone * Mathf.Rad2Deg;
        bool directionReverse = Vector3.Dot(before.normalized, after.normalized) <= -1.0f + Bias;
        bool onHorizontal = Vector3.Dot(Vector3.up, CurrGroundNormal) >= 1.0f - Bias;
        if (onHorizontal)
        {
            return linearDead && angularDead && directionReverse;
        }

        // 水平面ではない場合、加速度の大きさがDeadZone以下になっているかもチェック
        bool accelerationDead = Acceleration.magnitude <= AccelerationDeadZone;
        return linearDead && angularDead && directionReverse && accelerationDead;
    }

    // 水平面にいて、更新後に停止と判断した場合
    // 速度と加速度全部0にして、停止状態にして、trueを返す
    public bool CheckAndSetVelocityInfoAfterCancelOut(Vector3 before, Vector3 after)
    {
        if (IsStopAfterCancelOut(before, after))
        {
            Acceleration = Vector3.zero;
            AccelerationFriction = Vector3.zero;
            AccelerationLen = 0.0f;

            LinearVelocity = Vector3.zero;
            LinearVelocityLen = 0.0f;

            AngularVelocity = Vector3.zero;
            AngularVelocityLen = 0.0f;

            SpinPower = Vector3.zero;

            OnGround = true;
            return true;
        }
        return false;
    }

    // 接地情報を更新する
    public void SetGroundInfo(Transform ground, Vector3 normal)
    {
        PrevGround = CurrGround;
        PrevGroundNormal = CurrGroundNormal;
        CurrGround = ground;
        CurrGroundNormal = normal;
    }

    // 次接触する接地情報を更新する
    public void SetNextGroundInfo(Transform ground, Vector3 normal)
    {
        NextGround = ground;
        NextGroundNormal = normal;
    }
    
    // ゴロボールにする
    public void UpdateAngular()
    {
        // 静止状態だと更新不要
        if (IsStop())
        {
            return;
        }

        // 進行中、進行方向のFoward、Right、Upを求めて、
        // 進行方向にあったRotationを求める
        LinearVelocityRotation = ComputeLinearRotation();

        // 角速度と線速度の関係:ω = v / r
        // ωはラジアンなので、角度に変換する
        AngularVelocityLen = LinearVelocityLen / Collider.radius * Mathf.Rad2Deg;
        // 回転軸
        AngularVelocityAxis = LinearRight;
        AngularVelocity = AngularVelocityAxis * AngularVelocityLen;

        // 回転量を求める
        Quaternion rot = Quaternion.AngleAxis(AngularVelocityLen * Time.deltaTime, AngularVelocityAxis);
        // 回転させる
        transform.rotation = rot * transform.rotation;
    }

    // 進行中、進行方向のFoward、Right、Upを求めて、
    // 進行方向にあったRotationを求める
    public Quaternion ComputeLinearRotation()
    {
        LinearFoward = LinearVelocity.normalized;
        LinearRight = Vector3.Cross(LinearUp, LinearFoward).normalized;
        // 接地している時だけ更新
        // 平面間の移動によって、現在の平面の法線が変わるので
        if (OnGround)
        {
            // 進行方向のRight
            LinearRight = Vector3.Cross(CurrGroundNormal, LinearFoward).normalized;
        }

        // 進行方向のUp
        LinearUp = Vector3.Cross(LinearVelocity.normalized, LinearRight).normalized;

        Quaternion rot = Quaternion.LookRotation(LinearFoward, LinearUp);
        return rot;
    }

    private void DebugLine()
    {
        Debug.DrawLine(transform.position, transform.position + LinearFoward * DebugLineLen, Color.blue);
        Debug.DrawLine(transform.position, transform.position + LinearRight * DebugLineLen, Color.red);
        Debug.DrawLine(transform.position, transform.position + LinearUp * DebugLineLen, Color.green);
    }
}
