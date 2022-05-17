using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPhysics : MonoBehaviour
{
    public float Mass = 1.0f;
    public float SlopeLimit = 45.0f;
    public float PotentialLostRatio = 0.1f; // �����ɂԂ��鎞�A�ό`�Ȃǂ̌����ő�������G�l���M�[�̔䗦
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
        // transform.localRotation��Quaternion
        // Quaternion * Vec�ŁAVec����]������
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
        // �ڒn���Ă���
        if (OnGround)
        {
            // ���݂̕��ʂƐ����ʂ̊p�x�����߂�
            float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);
            Vector3 axis = Vector3.Cross(Vector3.up, CurrGroundNormal);

            var back = -Vector3.Normalize(Velocity);
            // ���C��
            AccelerationFriction = back * Gravity * cos * RollingFrictionRatio;

            // ��C��R��
            AccelerationFriction += back * AirFriction;

            // �d�͉����x
            float sin = 1.0f - cos * cos;
            if (sin > 0.0f)
            {
                sin = Mathf.Sqrt(sin);
                Vector3 gravityVec = Quaternion.AngleAxis(90.0f, axis) * CurrGroundNormal;
                AccelerationFriction += gravityVec * Gravity * sin;
            }
        }
        // �ڒn���Ă��Ȃ�
        else
        {
            //if (Mathf)
            var back = -Vector3.Normalize(Velocity);

            if (Velocity.magnitude <= VelocityDeadZone)
            {
                back = Vector3.down;
            }

            // ��C��R��
            AccelerationFriction = back * AirFriction;

            // �d�͉����x
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
        // ���ۂ̕����ł���΁A���͂̕������^��ŁA�d�͂�菬�����ꍇ�̂݁A��~����
        // ����̃Q�[���ł͑��x��VelocityDeadZone��菬�����A���ʂ̏�ɂ���΁A��~�ɂȂ��
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

        // �i�s��̕��ʃ`�F�b�N
        VelocityLen = Length(Velocity);
        Vector3 norVelocity = Velocity * (1.0f / VelocityLen);
        RayOnGround.direction = norVelocity;
        RayOnGround.origin = transform.position;
        // �i�s�̕�����RayCast����
        if (Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        {
            // �i�s��ɕ��ʂ�����ꍇ�A���̕��ʂ̖@���̋t������Ray�Ƃ��āARayCast�ŏՓ˖ڕW���擾
            // ���̖ڕW�����RayCast�ł̏Փ˖ڕW�Ɠ����ł���΁A�Ԃ��Ȃ��Փ˂Ɣ��f�ł���
            // CurrentGround��CurrGroundNormal�̍X�V�����ɓ���
            var targetTransform = RayHitInfoOnGround.transform;
            RayOnGround.direction = -RayHitInfoOnGround.normal;

            // �i�s��̕��ʂ̖@���̋t������RayCast����
            Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage"));
            if (targetTransform == RayHitInfoOnGround.transform)
            {
                // �ꍇ�ɂ���āAVelocity�̕�����RayCast�����āA
                // �擾�����Փ˃|�C���g�͎��g�ɂƂ��āA
                // ���ۂ̏Փ˃|�C���g�ł͂Ȃ��A���̏Փ˃|�C���g�ɓ��B����O�ɂ��łɏՓ˂��Ă���
                // ��FZ���̉��ɁA�@����Vector3(0.0f, 0.7XXXf, 0.7XXXf)���ʂ�����
                // �@�@Vector3(1.0f, 0.0f, 1.0f)�̕����������āA�i�߂�ꍇ�A
                // �@�@Vector3(0.0f, 0.0f, 1.0f)�̕�����Raycast�����������A
                // �@�@�ŏI�I�ɁA���ۂ̏Փ˃|�C���g���擾���邱�Ƃ��ł���
                // ���ʂ̖@�������ƂɁARaycast�̕����𐳂����ݒ肷�邽�߂ɁA
                // �i�s��̕��ʂ����݂̐ڒn���Ă��镽�ʂƂ̂Ȃ��p�x��90�x�ƂȂ�悤�ɁA
                // �i�s��̕��ʂ���]����΁A��]��̕��ʂ̖@���̋t������Raycast�̕����ł���

                // ��]�������߂�
                Vector3 axis = Vector3.Normalize(Vector3.Cross(CurrGroundNormal, RayHitInfoOnGround.normal));
                // ���݂̕��ʊԂ̊p�x�����߂�
                float cos = Vector3.Dot(CurrGroundNormal, RayHitInfoOnGround.normal);
                float angle = Mathf.Rad2Deg * Mathf.Acos(cos);
                // �@����axis������(90.0f - angle)��]������
                Vector3 normal = -Vector3.Normalize(Quaternion.AngleAxis(90.0f - angle, axis) * RayHitInfoOnGround.normal);
                Debug.DrawLine(transform.position, transform.position + normal * DebugLineLen, Color.black);

                // �i�s��̕��ʂƏՓ˂�����AVelocity�̌�����axis�����ɁAangle�x��]������
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

        // �^���ɂ��镽�ʃ`�F�b�N
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

    // ���ʂƐ����ʂ̂Ȃ��p�x
    public float ThetaBetweenPlanes(Vector3 normal)
    {
        return Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(normal, Vector3.up));
    }

    public bool isStop()
    {
        return OnGround && Length2(Velocity) <= VelocityDeadZone;
    }
}
