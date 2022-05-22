using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float Mass = 1.0f; // ����
    public float SlopeLimit = 45.0f; // �o���Ζʂ̍ő�p�x
    public float PotentialLostRatio = 0.15f; // �����ɂԂ��鎞�A�ό`�Ȃǂ̌����ő�������G�l���M�[�̔䗦
    public float Gravity = 0.098f;
    public float Friction = 0.0f;
    public float FrictionRatio = 0.1f; // ���C�W��
    public float RollingFrictionRatio = 0.025f; // �]���鎞�̖��C�͂͒ʏ�̖��C�͂�1/60�`1/40�ɂȂ�ƌ����Ă���
    public float AirFriction = 0.01f; // ��C��R��
    public bool OnGround = false; // �ڒn�t���O
    public bool OnHit = false; // �i�s��̕��ʂƂ̏Փ˃t���O
    public Transform PrevGround; // �O�ɐڒn���Ă��镽��
    public Vector3 PrevGroundNormal = Vector3.up; // �O�ɐڒn���Ă��镽�ʂ̖@��
    public Transform CurrGround; // ���ݐڒn���Ă��镽��
    public Vector3 CurrGroundNormal = Vector3.up; // ���ݐڒn���Ă��镽�ʂ̖@��
    public Transform NextGround; // ���ɐڒn���Ă��镽��
    public Vector3 NextGroundNormal = Vector3.up; // ���ɐڒn���Ă��镽�ʂ̖@��
    public Vector3 Acceleration = Vector3.zero; // �d�͂Ȃǂ̈ړ��𑣂��O�͂ɂ������x
    public float AccelerationLen = 0.0f; // �d�͂Ȃǂ̈ړ��𑣂��O�͂ɂ������x�̑傫��
    public Vector3 AccelerationFriction = Vector3.zero; // ���C�́A��C��R�͂Ȃǂ̈ړ����ז�����O�͂ɂ������x
    public float AccelerationFrictionLen = 0.0f; // ���C�́A��C��R�͂Ȃǂ̈ړ����ז�����O�͂ɂ������x�̑傫��
    public float AccelerationDeadZone = 0.0001f; // �����x��DeadZone
    public Vector3 LinearVelocity = Vector3.zero; // ���x
    public float LinearVelocityLen = 0.0f; // ���x�̑傫��
    public float LinearVelocityDeadZone = 0.0001f; // ���x��DeadZone
    public Vector3 AngularVelocityAxis = Vector3.zero; // �p��]�̎�
    public Vector3 AngularVelocity = Vector3.zero; // �p���x
    public float AngularVelocityLen = 0.0f; // �p���x�̑傫��
    public float AngularVelocityDeadZone = 0.1f; // �p���x��DeadZone
    public Vector3 SpinPower = Vector2.zero; // �X�s����������
    public bool VerticalShot = false; // �󒆂��΂�Shot�ł��邩
    public Quaternion LinearVelocityRotation; // �i�s����Rotation
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

    // Guideline�̕������ς�������A�Ăяo���K�v������
    public void InitPhysicsInfo(Transform target)
    {
        PrevPosition = target.position;
        transform.position = target.position;

        RayOnGround.direction = Vector3.down;
        RayOnGround.origin = target.position;

        LinearFoward = target.forward;
        LinearRight = target.right;
        LinearUp = target.up;

        // ����̂��ׂĂ̕��ʂ��`�F�b�N����ׂ�
        // ���݂͐^���̕��ʂ����`�F�b�N���Ă��Ȃ�
        if (Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        {
            SetNextGroundInfo(RayHitInfoOnGround.transform, RayHitInfoOnGround.normal);
            if (RayHitInfoOnGround.distance <= Collider.radius + Bias)
            {
                // �ڒn�����X�V����
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
        // �󒆂ɔ�΂�Shot�ł��邩
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
        // �󒆂ɔ�΂�Shot�ł��邩
        if (Vector3.Dot(Acceleration, Vector3.up) > 0.0f)
        {
            VerticalShot = true;
        }
    }


    public Vector3 UpdatePhysics()
    {
        // �Î~��Ԃ��ƍX�V�s�v
        if (IsStop())
        {
            return transform.position;
        }

        // �����x�Ƒ��x�̍X�V
        UpdateAccelerationAndVelocity();

        // ��]�̍X�V
        UpdateAngular();

        // �ʒu���A�ڒn�����X�V����
        Vector3 pos = UpdatePosition();
        return pos;
    }

    // �����x�Ƒ��x�̍X�V
    // ���x�͐�Ɉړ��𑣂��O�͂ŏ�������
    // ���̌�A�ړ����ז�����O�͂ŏ�������
    // 2�i�K�ɂȂ��Ă���̂ŁA���ꂼ��̒l��ۑ����Ă�����
    // ���ꂼ����o���Ďg�����Ƃ��\
    public void UpdateAccelerationAndVelocity()
    {
        // �����x�̓}�C�t���[���X�V����̂ŁA0�ɏ�����
        Acceleration = Vector3.zero;
        AccelerationFriction = Vector3.zero;
        float mass = 1.0f / Mass;

        // �ڒn���Ă���ꍇ
        if (OnGround)
        {
            // �ڒn���Ă��鎞�A���X�s������ɋ@�\����̂ŁA��ɉ��X�s����������
            Quaternion rot = Quaternion.AngleAxis(SpinPower.x * Time.deltaTime, CurrGroundNormal);
            LinearVelocity = rot * LinearVelocity;
            // ���X�s��������������
            SpinPower.x *= 1 - PotentialLostRatio * Time.deltaTime;

            // ���݂̕��ʂƐ����ʂ̊p�x��Cos�����߂�
            float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);

            // �d�͂ɂ��Ζʂ��犊���
            // �����ʂ̏ꍇ�ASin = 0�Ȃ̂ŁA�e�����󂯂Ȃ�
            // �Ζʂ̏ꍇ�A0 < Sin < 1�A�����I�Ɏ󂯂�
            // �����ʂ̏ꍇ�A Sin = 1�A�e����S���󂯂�
            float sin = 0.0f;
            Vector3 travelling = Vector3.zero;
            // Cos = 1�̏ꍇ�A�����ʏ�ɂ���̂ŁA�d�͂ɂ�銊��͂��Ȃ�
            // ����͂��v�Z����K�v���Ȃ��Ȃ�
            if (cos < 1.0f - Bias)
            {
                sin = Mathf.Sqrt(1.0f - cos * cos);
                // �����ʂ̖@���ƌ��݂̕��ʂ̖@���̊O�ς����߂�
                travelling = Vector3.Cross(Vector3.up, CurrGroundNormal);
                // ���߂��O�ςƌ��݂̕��ʂ̖@���̊O�ς����߂�
                // �Ō�ɋ��߂��O�ς͊���͂̕����ƂȂ�
                travelling = Vector3.Cross(travelling.normalized, CurrGroundNormal);
                travelling *= Gravity * sin;
            }
            Acceleration += travelling * mass;
            AccelerationLen = Acceleration.magnitude;

            // ���x���X�V���Ė��C�́A��C��R�͂���p����������擾����
            LinearVelocity += Acceleration;
            LinearVelocityLen = LinearVelocity.magnitude;

            // ��C��R�͂̌v�Z
            AccelerationFriction += ComputeAirFriction(mass);

            // �����ʂɂ��āA�X�V��ɒ�~�Ɣ��f�����ꍇ
            // ���x�Ɖ����x�S��0�ɂ��āA��~��Ԃɂ���
            if (CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + AccelerationFriction))
            {
                return;
            }

            // ���C�͂̌v�Z
            AccelerationFriction += ComputeFriction(mass, cos);
            AccelerationFrictionLen = AccelerationFriction.magnitude;

            // �����ʂɂ��āA�X�V��ɒ�~�Ɣ��f�����ꍇ
            // ���x�Ɖ����x�S��0�ɂ��āA��~��Ԃɂ���
            if (CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + AccelerationFriction))
            {
                return;
            }

            // AccelerationFriction���X�V�ł�����A���x���X�V����
            LinearVelocity += AccelerationFriction;

            // Ball���]����Ԃ��A�ό`����̂ŁA�G�l���M�[��������������
            LinearVelocity *= 1 - PotentialLostRatio * Time.deltaTime;
            LinearVelocityLen = LinearVelocity.magnitude;
        }
        // �ڒn���Ă��Ȃ��ꍇ
        else
        {
            // �d��
            // �����͐^��
            Acceleration += Vector3.down * Gravity;
            AccelerationLen = Acceleration.magnitude;

            // ���x���X�V���ċ�C��R�͂���p����������擾����
            LinearVelocity += Acceleration;
            LinearVelocityLen = LinearVelocity.magnitude;

            // ��C��R��
            // �����͑��x�̋t����
            AccelerationFriction += ComputeAirFriction(mass);
            AccelerationFrictionLen = AccelerationFriction.magnitude;

            // AccelerationFriction���X�V�ł�����A���x���X�V����
            LinearVelocity += AccelerationFriction;
            LinearVelocityLen = LinearVelocity.magnitude;
        }
    }

    // ��C��R�͂̌v�Z
    public Vector3 ComputeAirFriction(float mass)
    {
        // ��C��R��
        // �����͑��x�̋t����
        Vector3 airFrictionVec = -LinearVelocity.normalized;

        return airFrictionVec * AirFriction * mass;
    }

    public Vector3 ComputeFriction(float mass, float cos)
    {
        // �d�͂Əd�͂ɂ�镽�ʂ���̎x�����
        // ���̓�̗͂���{���E����A�܂�A�n�ʂɂ߂肱�ނ悤�ȗ͂��Ȃ��Ȃ�
        // �����ʂ̏ꍇ�ACos = 1�Ȃ̂ŁA�e����S���󂯂�
        // �Ζʂ̏ꍇ�A0 < Cos < 1�A�����I�Ɏ󂯂�
        // �����ʂ̏ꍇ�A Cos = 0�A�e�����󂯂Ȃ�
        // ���̗͕͂��ʏ�̖��C�͂ɉe����^����
        // �����͕��ʂ��ړ����鑬�x�̋t�����ɂȂ�
        // ���݂̃t���[���ł̑��x�̕��������߂Ă���A
        // ���ʂɕ��s���鑬�x�̕����̋t�����ɂ�����Ƃ���
        // ���݋��߂��͍̂ő�Î~���C��
        // ���̂Ȃ̂ŁA��ɋ��߂�����͂�0�o�Ȃ��A�܂�Ζʂɂ���ꍇ
        // �K�����������̂ŁAFriction�𖀎C�͂Ƃ��Ďg����
        // �����ʂɂ���ꍇ�A�Î~�^�C�~���O�͑��x�̑傫����DeadZone�ȉ��̎��A
        // �������́A�X�V��̑��x�̕������X�V�O�̑��x�̕������t�ɂȂ鎞�A
        // ���C�́A��C��R�͂�K�p��A�`�F�b�N����K�v������
        Friction = Gravity * cos * FrictionRatio * RollingFrictionRatio;

        // ���C��
        // �����͕��ʂƕ��s����
        // �{�[���̑��x�̕�����2�p�^�[���l������
        // 1: ���ʂɉ����Ĉړ�����B�܂�A���ʂƕ��s���Ă���
        // 2: �O�̃t���[���ł͏ォ�畽�ʂƂԂ����āA
        //    ���ʂ̖@�������ɔ��˂��A���ʂ̏�Ɍ����Ă���
        // �ǂ̃p�^�[���ł��A���x�̕����ƌ��݂̕��ʂ̖@���Ƃ̊O�ς����߂�
        // ���߂��O�ςƌ��݂̕��ʂ̖@���Ƃ̊O�ς����߂�
        // �Ō�ɋ��߂��O�ς͖��C�͂̕����ƂȂ�
        Vector3 frictionVec = Vector3.Cross(LinearVelocity.normalized, CurrGroundNormal);
        frictionVec = Vector3.Cross(frictionVec.normalized, CurrGroundNormal);

        return frictionVec * Friction * mass;
    }

    // �ʒu���A�ڒn�����X�V����
    public Vector3 UpdatePosition()
    {
        // ���x������x���X�V��A�Î~�����𖞂����ꍇ������
        // �Î~��Ԃ��ƍX�V�s�v
        if (IsStop())
        {
            return transform.position;
        }

        // �ʒu���X�V����
        PrevPosition = transform.position;
        transform.position += LinearVelocity * Time.deltaTime;

        CheckOnCurrentGround();
        CheckMoveDirectionOnHit();

        return transform.position;
    }

    // ���ʂɂ��邩���`�F�b�N����
    // RayCast�œ����蔻������
    // �Փˌ�̔��ˏ������s��
    public bool CheckOnCurrentGround()
    {
        OnGround = false;

        // Raycast�̕��������ݎ����Ă��镽�ʂ̖@�����̋t�����ɂ���
        RayOnGround.direction = -CurrGroundNormal;
        RayOnGround.origin = transform.position;

        // ���ݎ����Ă��镽�ʂ̖@������Raycast���āA����������Ȃ�������A
        // ���̕��ʂ�ʂ�߂����Ɣ��f�ł���̂ŁA�ȉ��̏����͕s�v
        if (!Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage")))
        {
            SetGroundInfo(null, Vector3.zero);
            return OnGround;
        }

        // ���݂̕��ʂƂ̋�����Ball�̔��a�ȏゾ�ƁA������Ȃ������Ɣ��f�ł���
        // ����ȍ~�̏������s�v
        if (RayHitInfoOnGround.distance > Collider.radius + Bias)
        {
            return OnGround;
        }

        // ���݂̕��ʂƂ̋�����Ball�̔��a�ȉ����ƁA���������Ɣ��f�ł���
        // �ڒn�����X�V����
        SetGroundInfo(RayHitInfoOnGround.transform, RayHitInfoOnGround.normal);

        // �c�X�s����������
        LinearVelocity = ComputeVelocityWhenIsVerticalShot(RayHitInfoOnGround.normal);

        // ���x�̕����𕽖ʂ̖@�������Ƃɔ��˂���
        // ���x�̕��������ʂƕ��s���Ă���ꍇ�A���˂��Ă��ς��Ȃ�
        // ���x�̕��������ʂƕ��s���Ă��Ȃ��ꍇ�A���ʂɓ˓����Ă������Ƃ��킩��
        // ���̂��߁A���ˌ�̑��x�̕����͒n�ʂ��痣���
        LinearVelocity = Vector3.Reflect(LinearVelocity, CurrGroundNormal);

        // ���ʂɓ˓����Ă����ꍇ�̂݁A�G�l���M�[�̑����𔽉f����
        // ���x�̕����ƕ��ʂ̖@���̓��ςŁA��������0�`PotentialLostRatio�ɂ���
        float dot = Vector3.Dot(LinearVelocity.normalized, CurrGroundNormal);
        LinearVelocity *= 1 - PotentialLostRatio * dot;
        LinearVelocityLen = LinearVelocity.magnitude;

        // ���x�����˂ɂ���ĕύX���ꂽ�̂ŁA���̏�Œ�~��Ԃɖ����������`�F�b�N
        // ���݂̕��ʂƐ����ʂ̊p�x��Cos�����߂�
        float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);
        float mass = 1.0f / Mass;

        // �d�͂��v�Z���A��~��Ԃ��`�F�b�N
        Vector3 gravity = Vector3.down * Gravity;
        CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + gravity);

        // ��C��R�͂��v�Z���A��~��Ԃ��`�F�b�N
        Vector3 airfriction = ComputeAirFriction(mass);
        CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + airfriction);

        // ���C�͂��v�Z���A��~��Ԃ��`�F�b�N
        Vector3 friction = ComputeFriction(mass, cos);
        CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + friction);

        // �߂荞�݂�␳����
        var pos = transform.position;
        pos = RayHitInfoOnGround.point + RayHitInfoOnGround.normal * Collider.radius;
        transform.position = pos;

        OnGround = true;

        return OnGround;
    }

    // �i�s��̂�����`�F�b�N
    public bool CheckMoveDirectionOnHit()
    {
        OnHit = false;

        RayMoveDir.direction = LinearVelocity.normalized;
        RayMoveDir.origin = transform.position;

        // �i�s�����Ɛڒn�`�F�b�N����Raycast�����������ł��邩���`�F�b�N
        // �����̏ꍇ�A���łɃ`�F�b�N�ς݂Ȃ̂ŁA�ă`�F�b�N���s�v
        float dot = Vector3.Dot(RayMoveDir.direction, RayOnGround.direction);
        if (dot >= 1.0f - Bias)
        {
            OnHit = OnGround;
            return OnHit;
        }

        // Raycast����
        // �i�s��ɕ��ʂȂǂ��Ȃ��ꍇ�A����ȍ~�̏������s�v
        if (!Physics.Raycast(RayMoveDir, out RayHitInfoMoveDir, RayLen, LayerMask.GetMask("Stage")))
        {
            SetNextGroundInfo(null, Vector3.zero);
            return OnHit;
        }
        SetNextGroundInfo(RayHitInfoMoveDir.transform, RayHitInfoMoveDir.normal);

        // �i�s��ɕ��ʂ�����ꍇ�A���̕��ʂ̖@���̋t������Ray�Ƃ��āARayCast�ŏՓ˂��镽�ʂ��擾
        Transform targetTransform = RayHitInfoMoveDir.transform;
        RayMoveDir.direction = -RayHitInfoMoveDir.normal;

        // �i�s��̕��ʂ̖@���̋t������RayCast����
        Physics.Raycast(RayMoveDir, out RayHitInfoMoveDir, RayLen, LayerMask.GetMask("Stage"));

        // �Փ˂��镽�ʂ����RayCast�ł̏Փ˂��镽�ʂƓ����łȂ���΁A����ȍ~�̏������s�v�ɂȂ�
        if (targetTransform != RayHitInfoMoveDir.transform)
        {
            return OnHit;
        }

        // �Փ˂��镽�ʂƂ̋�����Ball�̔��a�ȏゾ�ƁA������Ȃ������Ɣ��f�ł���
        // ����ȍ~�̏������s�v
        if (RayHitInfoMoveDir.distance > Collider.radius + Bias)
        {
            return OnHit;
        }

        // ���x�̔��ˍX�V
        UpdateVelocityAfterHit();

        // �߂荞�݂�␳����
        var pos = transform.position;
        pos = RayHitInfoMoveDir.point + RayHitInfoMoveDir.normal * Collider.radius;
        transform.position = pos;

        OnHit = true;

        return OnHit;
    }

    // ���x�̔��ˍX�V
    public void UpdateVelocityAfterHit()
    {
        // �c�X�s����������
        LinearVelocity = ComputeVelocityWhenIsVerticalShot(RayHitInfoMoveDir.normal);

        // �Փ˂������ʂ����RayCast�ł̏Փ˂��镽�ʂƓ����ł���΁A�Ԃ��Ȃ��Փ˂Ɣ��f�ł���
        // �o��镽�ʂ̍ő�p�x��cos�����߂�
        // �Փ˂������ʂ̖@�������ƂɁA�i�s�����𔽎˂�����
        // ���ˌ�̃x�N�g���ƏՓ˂������ʂ̖@���Ƃ̓��ς����߂�
        // ���߂����ς����߂�cos��菬�����ꍇ�A���ʂɓo���Ɣ��f�ł���
        // �Փ˂������ʂ������ʂ̏ꍇ�A���̂܂ܔ��ˏ���������
        // �ǂ�����A���ʂƏՓ˂����̂ŁAcos�������đ�������0�`PotentialLostRatio�ɂ���
        float cos = Mathf.Cos(SlopeLimit * Mathf.Deg2Rad);
        LinearVelocity *= 1 - PotentialLostRatio * cos;
        Vector3 reflectVel = Vector3.Reflect(LinearVelocity, RayHitInfoMoveDir.normal);
        float dot = Vector3.Dot(reflectVel.normalized, RayHitInfoMoveDir.normal);
        // �o���ꍇ�ACurrentGround��CurrGroundNormal�̍X�V�����ɓ���
        if (dot <= cos + Bias)
        {
            // �Փ˂������ʂɓo��u�ԑ��x�̑傫���͂��̂܂܁A
            // ���������ʂƕ��s����悤�ɕς��
            // ���x�̉�]���͌��݂̕��ʂƏՓ˂������ʂƂ̊O�ςɂȂ�
            // ���݂̕��ʂƏՓ˂������ʂƂ̊p�x����ςŋ��߂�
            // ���x�����߂��O�ς����ɁA���߂��p�x����]������ƁA
            // ���ʂ�o�ꂽ��̑��x�ƂȂ�
            Vector3 axis = Vector3.Cross(CurrGroundNormal, RayHitInfoMoveDir.normal);
            cos = Vector3.Dot(CurrGroundNormal, RayHitInfoMoveDir.normal);
            float angle = Mathf.Rad2Deg * Mathf.Acos(cos);
            LinearVelocity = Quaternion.AngleAxis(angle, axis.normalized) * LinearVelocity;
            LinearVelocityLen = LinearVelocity.magnitude;

            // ���x����]�ɂ���ĕύX���ꂽ�̂ŁA���̏�Œ�~��Ԃɖ����������`�F�b�N
            // �Փ˂������ʂƐ����ʂ̊p�x��Cos�����߂�
            cos = Vector3.Dot(RayHitInfoMoveDir.normal, Vector3.up);
            float mass = 1.0f / Mass;

            // �d�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 gravity = Vector3.down * Gravity;
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + gravity);

            // ��C��R�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 airfriction = ComputeAirFriction(mass);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + airfriction);

            // ���C�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 friction = ComputeFriction(mass, cos);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + friction);

            // �ڒn�����X�V����
            SetGroundInfo(RayHitInfoMoveDir.transform, RayHitInfoMoveDir.normal);
            OnGround = true;
        }
        // �o��Ȃ��ꍇ�A���ˏ���
        else
        {
            // ���x�̔���
            LinearVelocity = reflectVel;
            LinearVelocityLen = LinearVelocity.magnitude;

            // ���x�����˂ɂ���ĕύX���ꂽ�̂ŁA���̏�Œ�~��Ԃɖ����������`�F�b�N
            // �Փ˂������ʂƐ����ʂ̊p�x��Cos�����߂�
            cos = Vector3.Dot(RayHitInfoMoveDir.normal, Vector3.up);
            float mass = 1.0f / Mass;

            // ��C��R�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 airfriction = ComputeAirFriction(mass);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + airfriction);

            // ���C�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 friction = ComputeFriction(mass, cos);
            CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + friction);

            // ���ʂ������ʂ̏ꍇ�A���˂��Ă��d�͂��`�F�b�N����K�v���Ȃ�
            // �ڒn�����X�V����K�v���Ȃ�
            // �܂�Acos > 0.0f�̏ꍇ�i�����ʂł͂Ȃ��j�̂݁A�`�F�b�Nand�X�V
            if (cos > 0.0f + Bias)
            {
                // �d�͂��v�Z���A��~��Ԃ��`�F�b�N
                Vector3 gravity = Vector3.down * Gravity;
                CheckAndSetVelocityInfoAfterCancelOut(LinearVelocity, LinearVelocity + gravity);

                // �ڒn�����X�V����
                SetGroundInfo(RayHitInfoMoveDir.transform, RayHitInfoMoveDir.normal);
                OnGround = false;
            }
        }

        // �Փ˂�����X�s�����Ȃ���
        SpinPower = Vector3.zero;
    }

    // �c�X�s�������鎞�A�Փˌ�̑��x�X�V
    public Vector3 ComputeVelocityWhenIsVerticalShot(Vector3 normal)
    {
        // VerticalShot�ł͂Ȃ���΁A�X�V�s�v
        if (!VerticalShot)
        {
            return LinearVelocity;
        }
        // ���ڂ̒��n�����X�s����������
        VerticalShot = false;

        // �Փ˂����u�ԂɁA���x�̕��ʂɕ��s���镪���xV.x�����߂�
        // V: ���x
        // V.x: ���ʂɕ��s���镪���x
        // V.y: ���ʂɐ������镪���x
        // ���x�ƕ��ʂ̖@���Ƃ̓��ς���AV.y�����߂���
        // V = V.x + V.y ���� V.x = V - V.y
        float dot = Vector3.Dot(LinearVelocity, normal);
        Vector3 vy = normal * dot;
        Vector3 vx = LinearVelocity + -vy;
        Vector3 spinY = vx;
        int sy = (int)Mathf.Abs(SpinPower.y);
        if (sy > 0.0f)
        {
            // V.x��Foward��SpinPower.y���A�c�X�s����������
            int maxSpinStep = 6 + 1; // 0���܂߂�7�ɂȂ�
            int ratio = maxSpinStep / (6 / 2); // -1�`-3�͂܂��t�����ɔ�΂Ȃ��A-4�`-6�͋t�����ɔ�ԁB���傤�ǔ�������
            spinY += vx * (ratio * SpinPower.y / maxSpinStep);
        }
        
        // V.z: V.x��Foward�A���ʂ̖@����Up�Ƃ�������Right
        // Right������SpinPower.z���A���X�s����������
        Vector3 vz = Vector3.Cross(normal, vx.normalized);
        Vector3 spinZ = vz.normalized * SpinPower.z;
        return spinY + spinZ + vy;
    }

    // �Î~�����F
    // ���x�����x��DeadZone�ȉ��A
    // �O�t���[���̉����x�������x��DeadZone�ȉ�
    public bool IsStop()
    {
        // �ΖʂƐ����ʂƂ̓��ςɂ���āAVelocityDeadZone�𓮓I�ɕς���
        // �Ζʂ��̂ڂ鎞�A���x�̑傫����0�ɂƂĂ��߂��^�C�~���O������
        // ���̎��AVelocityDeadZone�����x�̑傫���𒴂��Ă��܂��ƁA�Ζʂɒ�~���Ă��܂�
        // �����ʂɂ��鎞�A���x�̑傫����������x0�ɋ߂Â�����A������~�����Ă����v
        float dot = Vector3.Dot(CurrGroundNormal, Vector3.up);
        bool linearDead = LinearVelocity.magnitude <= LinearVelocityDeadZone * dot;
        bool angularDead = AngularVelocity.magnitude <= AngularVelocityDeadZone * dot * Mathf.Rad2Deg;
        bool accelerationDead = Acceleration.magnitude <= AccelerationDeadZone * dot;
        return linearDead && angularDead && accelerationDead;
    }

    // �Î~�����F
    // �����ʂɂ���ꍇ�A���x�̑傫����DeadZone�ȉ�
    // �X�V��̑��x�̕������X�V�O�̑��x�̕������t
    // ���C�́A��C��R�͂�K�p��A�v�`�F�b�N
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

        // �����ʂł͂Ȃ��ꍇ�A�����x�̑傫����DeadZone�ȉ��ɂȂ��Ă��邩���`�F�b�N
        bool accelerationDead = Acceleration.magnitude <= AccelerationDeadZone;
        return linearDead && angularDead && directionReverse && accelerationDead;
    }

    // �����ʂɂ��āA�X�V��ɒ�~�Ɣ��f�����ꍇ
    // ���x�Ɖ����x�S��0�ɂ��āA��~��Ԃɂ��āAtrue��Ԃ�
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

    // �ڒn�����X�V����
    public void SetGroundInfo(Transform ground, Vector3 normal)
    {
        PrevGround = CurrGround;
        PrevGroundNormal = CurrGroundNormal;
        CurrGround = ground;
        CurrGroundNormal = normal;
    }

    // ���ڐG����ڒn�����X�V����
    public void SetNextGroundInfo(Transform ground, Vector3 normal)
    {
        NextGround = ground;
        NextGroundNormal = normal;
    }
    
    // �S���{�[���ɂ���
    public void UpdateAngular()
    {
        // �Î~��Ԃ��ƍX�V�s�v
        if (IsStop())
        {
            return;
        }

        // �i�s���A�i�s������Foward�ARight�AUp�����߂āA
        // �i�s�����ɂ�����Rotation�����߂�
        LinearVelocityRotation = ComputeLinearRotation();

        // �p���x�Ɛ����x�̊֌W:�� = v / r
        // �ւ̓��W�A���Ȃ̂ŁA�p�x�ɕϊ�����
        AngularVelocityLen = LinearVelocityLen / Collider.radius * Mathf.Rad2Deg;
        // ��]��
        AngularVelocityAxis = LinearRight;
        AngularVelocity = AngularVelocityAxis * AngularVelocityLen;

        // ��]�ʂ����߂�
        Quaternion rot = Quaternion.AngleAxis(AngularVelocityLen * Time.deltaTime, AngularVelocityAxis);
        // ��]������
        transform.rotation = rot * transform.rotation;
    }

    // �i�s���A�i�s������Foward�ARight�AUp�����߂āA
    // �i�s�����ɂ�����Rotation�����߂�
    public Quaternion ComputeLinearRotation()
    {
        LinearFoward = LinearVelocity.normalized;
        LinearRight = Vector3.Cross(LinearUp, LinearFoward).normalized;
        // �ڒn���Ă��鎞�����X�V
        // ���ʊԂ̈ړ��ɂ���āA���݂̕��ʂ̖@�����ς��̂�
        if (OnGround)
        {
            // �i�s������Right
            LinearRight = Vector3.Cross(CurrGroundNormal, LinearFoward).normalized;
        }

        // �i�s������Up
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
