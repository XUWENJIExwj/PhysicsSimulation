using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float Mass = 1.0f; // ����
    public float SlopeLimit = 45.0f; // �o���Ζʂ̍ő�p�x
    public float PotentialLostRatio = 0.1f; // �����ɂԂ��鎞�A�ό`�Ȃǂ̌����ő�������G�l���M�[�̔䗦
    public float Gravity = 0.098f;
    public float Friction = 0.0f;
    public float FrictionRatio = 0.1f; // ���C�W��
    public float RollingFrictionRatio = 0.025f; // �]���鎞�̖��C�͂͒ʏ�̖��C�͂�1/60�`1/40�ɂȂ�ƌ����Ă���
    public float AirFriction = 0.01f; // ��C��R��
    public bool OnGround = false; // �ڒn�t���O
    public Transform CurrentGround; // ���ݐڒn���Ă��镽��
    public Vector3 CurrGroundNormal = Vector3.up; // ���ݐڒn���Ă��镽�ʂ̖@��
    public Vector3 Acceleration = Vector3.zero; // �d�͂Ȃǂ̈ړ��𑣂��O�͂ɂ������x
    public Vector3 AccelerationFriction = Vector3.zero; // ���C�́A��C��R�͂Ȃǂ̈ړ����ז�����O�͂ɂ������x
    public float AccelerationLen = 0.0f; // �����x�̑傫��
    public float AccelerationDeadZone = 0.00001f; // �����x��DeadZone
    public Vector3 Velocity = Vector3.zero; // ���x
    public float VelocityLen = 0.0f; // ���x�̑傫��
    public float VelocityDeadZone = 0.00001f; // ���x��DeadZone
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

        // ����̂��ׂĂ̕��ʂ��`�F�b�N����ׂ�
        // ���݂͐^���̕��ʂ����`�F�b�N���Ă��Ȃ�
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
        // �Î~��Ԃ��ƍX�V�s�v
        if (IsStop())
        {
            return;
        }

        // �����x�Ƒ��x�̍X�V
        UpdateAccelerationAndVelocity();
    }

    // �����x�Ƒ��x�̍X�V
    public void UpdateAccelerationAndVelocity()
    {
        // �����x�̓}�C�t���[���X�V����̂ŁA0�ɏ�����
        Acceleration = Vector3.zero;
        AccelerationFriction = Vector3.zero;

        // �ڒn���Ă���ꍇ
        if (OnGround)
        {
            // ���݂̕��ʂƐ����ʂ̊p�x��Cos�����߂�
            float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);

            // �d�͂ɂ��Ζʂ��犊���
            // �����ʂ̏ꍇ�ASin = 0�Ȃ̂ŁA�e�����󂯂Ȃ�
            // �Ζʂ̏ꍇ�A0 < Sin < 1�A�����I�Ɏ󂯂�
            // �����ʂ̏ꍇ�A Sin = 1�A�e����S���󂯂�
            // �����͊Y�����ʂƐ����ʂ̖@���Ƃ̊O�ς����߂��x�N�g��������
            // 90�x��]�����x�N�g���ɂȂ�
            float sin = 0.0f;
            Vector3 travelling = Vector3.zero;
            // Cos = 1�̏ꍇ�A�����ʏ�ɂ���̂ŁA�d�͂ɂ�銊��͂��Ȃ�
            // ����͂��v�Z����K�v���Ȃ��Ȃ�
            if (cos < 1.0f)
            {
                sin = Mathf.Sqrt(1.0f - cos * cos);
                Vector3 cross = Vector3.Cross(CurrGroundNormal, Vector3.up);
                travelling = Quaternion.AngleAxis(90.0f, cross.normalized) * CurrGroundNormal;
                travelling *= Gravity * sin;
            }
            Acceleration += travelling;

            // �d�͂Əd�͂ɂ�镽�ʂ���̎x�����
            // ���̓�̗͂���{���E����A�܂�A�n�ʂɂ߂肱�ނ悤�ȗ͂��Ȃ��Ȃ�
            // �����ʂ̏ꍇ�ACos = 1�Ȃ̂ŁA�e����S���󂯂�
            // �Ζʂ̏ꍇ�A0 < Cos < 1�A�����I�Ɏ󂯂�
            // �����ʂ̏ꍇ�A Cos = 0�A�e�����󂯂Ȃ�
            // ���̗͕͂��ʏ�̖��C�͂ɉe����^����
            // �����͕��ʂ��ړ����鑬�x�̋t�����ɂȂ�
            // ���݂̃t���[���ł̑��x�̕��������߂Ă���A
            // ���ʂɕ��s���鑬�x�̕����̋t�����ɂ�����Ƃ���
            Friction = Gravity * cos * FrictionRatio * RollingFrictionRatio;

            // ���x���X�V���Ė��C�́A��C��R�͂���p����������擾����
            float mass = 1.0f / Mass;
            Acceleration *= mass;
            Velocity += Acceleration;

            // ��C��R��
            // �����͑��x�̋t����
            Vector3 airFrictionVec = -Velocity.normalized;

            // AccelerationFriction���X�V
            AccelerationFriction += airFrictionVec * AirFriction;

            // ���C��
            // �����͕��ʂƕ��s����
            // �{�[���̑��x�̕�����2�p�^�[���l������
            // 1: ���ʂɉ����Ĉړ�����B�܂�A���ʂƕ��s���Ă���
            // 2: �O�̃t���[���ł͏ォ�畽�ʂƂԂ����āA
            //    ���ʂ̖@�������ɔ��˂��A���ʂ̏�Ɍ����Ă���
            // �ǂ̃p�^�[���ł��A���x�̕����Ɩ@���Ƃ̓��ς����߂�Cos�̊p�x��Theta�ɁA
            // �@���Ƒ��x�̕����Ƃ̊O�ς����߂��x�N�g����Axis�ɂ��āA
            // Axis�����ɑ��x�̕�����(90.0f - Theta)�x��]����΁A
            // ���ʂɕ��s���鑬�x�̕��������߂���
            float dot = Vector3.Dot(Velocity.normalized, CurrGroundNormal);
            float theta = 90.0f - Mathf.Acos(dot) * Mathf.Rad2Deg;
            Vector3 axis = Vector3.Cross(CurrGroundNormal, Velocity.normalized);
            Vector3 frictionVec = Quaternion.AngleAxis(theta, axis.normalized) * Velocity.normalized;

            // AccelerationFriction���X�V
            AccelerationFriction += frictionVec * Friction;
            AccelerationFriction *= mass;

            // AccelerationFriction���X�V�ł�����A���x���X�V����
            Velocity += AccelerationFriction;

            // �S�Ă̗͂�������������x��ۑ����Ă���
            // ��~��Ԃɗ��p�����
            Acceleration += AccelerationFriction;
        }
        // �ڒn���Ă��Ȃ��ꍇ
        else
        {

        }
    }

    // �Î~�����F���x�����x��DeadZone�ȉ��A�O�t���[���̉����x�������x��DeadZone�ȉ�
    public bool IsStop()
    {
        return VelocityLen <= VelocityDeadZone && AccelerationLen <= AccelerationDeadZone;
    }

    private void DebugLine()
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
}
