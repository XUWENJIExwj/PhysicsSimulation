using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float Mass = 1.0f; // ����
    public float SlopeLimit = 45.0f; // �o���Ζʂ̍ő�p�x
    public float PotentialLostRatio = 0.25f; // �����ɂԂ��鎞�A�ό`�Ȃǂ̌����ő�������G�l���M�[�̔䗦
    public float Gravity = 0.098f;
    public float Friction = 0.0f;
    public float FrictionRatio = 0.1f; // ���C�W��
    public float RollingFrictionRatio = 0.025f; // �]���鎞�̖��C�͂͒ʏ�̖��C�͂�1/60�`1/40�ɂȂ�ƌ����Ă���
    public float AirFriction = 0.001f; // ��C��R��
    public bool OnGround = false; // �ڒn�t���O
    public bool OnHit = false; // �i�s��̕��ʂƂ̏Փ˃t���O
    public Transform CurrGround; // ���ݐڒn���Ă��镽��
    public Vector3 CurrGroundNormal = Vector3.up; // ���ݐڒn���Ă��镽�ʂ̖@��
    public Transform PrevGround; // �O�ɐڒn���Ă��镽��
    public Vector3 PrevGroundNormal = Vector3.up; // �O�ɐڒn���Ă��镽�ʂ̖@��
    public Vector3 Acceleration = Vector3.zero; // �d�͂Ȃǂ̈ړ��𑣂��O�͂ɂ������x
    public float AccelerationLen = 0.0f; // �d�͂Ȃǂ̈ړ��𑣂��O�͂ɂ������x�̑傫��
    public Vector3 AccelerationFriction = Vector3.zero; // ���C�́A��C��R�͂Ȃǂ̈ړ����ז�����O�͂ɂ������x
    public float AccelerationFrictionLen = 0.0f; // ���C�́A��C��R�͂Ȃǂ̈ړ����ז�����O�͂ɂ������x�̑傫��
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
    public float Bias = 0.001f;
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
                CurrGround = RayHitInfoOnGround.transform;
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
        UpdatePhysics();
    }

    private void LateUpdate()
    {
        
    }

    public void AddForce(float power, Vector3 direction)
    {
        Acceleration = direction * power / Mass;
        Velocity += Acceleration;
        VelocityLen = Velocity.magnitude;
    }

    public void UpdatePhysics()
    {
        // �Î~��Ԃ��ƍX�V�s�v
        //if (IsStop())
        //{
        //    return;
        //}

        // �����x�Ƒ��x�̍X�V
        UpdateAccelerationAndVelocity();

        // �ʒu���A�ڒn�����X�V����
        UpdateTransform();
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
            Velocity += Acceleration;
            VelocityLen = Velocity.magnitude;

            // ��C��R�͂̌v�Z
            AccelerationFriction += ComputeAirFriction(mass);

            // �����ʂɂ��āA�X�V��ɒ�~�Ɣ��f�����ꍇ
            // ���x�Ɖ����x�S��0�ɂ��āA��~��Ԃɂ���
            if (CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + AccelerationFriction))
            {
                return;
            }

            // ���C�͂̌v�Z
            AccelerationFriction += ComputeFriction(mass, cos);
            AccelerationFrictionLen = AccelerationFriction.magnitude;

            // �����ʂɂ��āA�X�V��ɒ�~�Ɣ��f�����ꍇ
            // ���x�Ɖ����x�S��0�ɂ��āA��~��Ԃɂ���
            if (CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + AccelerationFriction))
            {
                return;
            }

            // AccelerationFriction���X�V�ł�����A���x���X�V����
            Velocity += AccelerationFriction;
            VelocityLen = Velocity.magnitude;
        }
        // �ڒn���Ă��Ȃ��ꍇ
        else
        {
            // �d��
            // �����͐^��
            Acceleration += Vector3.down * Gravity;
            AccelerationLen = Acceleration.magnitude;

            // ���x���X�V���ċ�C��R�͂���p����������擾����
            Velocity += Acceleration;
            VelocityLen = Velocity.magnitude;

            // ��C��R��
            // �����͑��x�̋t����
            AccelerationFriction += ComputeAirFriction(mass);
            AccelerationFrictionLen = AccelerationFriction.magnitude;

            // AccelerationFriction���X�V�ł�����A���x���X�V����
            Velocity += AccelerationFriction;
            VelocityLen = Velocity.magnitude;
        }
    }

    // ��C��R�͂̌v�Z
    public Vector3 ComputeAirFriction(float mass)
    {
        // ��C��R��
        // �����͑��x�̋t����
        Vector3 airFrictionVec = -Velocity.normalized;

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
        Vector3 frictionVec = Vector3.Cross(Velocity.normalized, CurrGroundNormal);
        frictionVec = Vector3.Cross(frictionVec.normalized, CurrGroundNormal);

        return frictionVec * Friction * mass;
    }

    // �ʒu���A�ڒn�����X�V����
    public void UpdateTransform()
    {
        // ���x������x���X�V��A�Î~�����𖞂����ꍇ������
        // �Î~��Ԃ��ƍX�V�s�v
        if (IsStop())
        {
            return;
        }

        // �ʒu���X�V����
        // DeltaTime��UpdateAccelerationAndVelocity()�̉����x�ɂ�����̂��v����
        PrevPosition = transform.position;
        transform.position += Velocity * Time.deltaTime;

        CheckOnCurrentGround();
        CheckMoveDirectionOnHit();
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
        Physics.Raycast(RayOnGround, out RayHitInfoOnGround, RayLen, LayerMask.GetMask("Stage"));

        // ���݂̕��ʂƂ̋�����Ball�̔��a�ȏゾ�ƁA������Ȃ������Ɣ��f�ł���
        // ����ȍ~�̏������s�v
        if (RayHitInfoOnGround.distance > Collider.radius)
        {
            return OnGround;
        }

        // ���݂̕��ʂƂ̋�����Ball�̔��a�ȉ����ƁA���������Ɣ��f�ł���
        // �ڒn�����X�V����
        CurrGround = RayHitInfoOnGround.transform;
        CurrGroundNormal = RayHitInfoOnGround.normal;

        // ���x�̕����𕽖ʂ̖@�������Ƃɔ��˂���
        // ���x�̕��������ʂƕ��s���Ă���ꍇ�A���˂��Ă��ς��Ȃ�
        // ���x�̕��������ʂƕ��s���Ă��Ȃ��ꍇ�A���ʂɓ˓����Ă������Ƃ��킩��
        // ���̂��߁A���ˌ�̑��x�̕����͒n�ʂ��痣���
        Velocity = Vector3.Reflect(Velocity, CurrGroundNormal);
        VelocityLen = Velocity.magnitude;

        // ���ʂɓ˓����Ă����ꍇ�̂݁A�G�l���M�[�̑����𔽉f����
        // ���x�̕����ƕ��ʂ̖@���̓��ςŁA��������0�`PotentialLostRatio�ɂ���
        float dot = Vector3.Dot(Velocity.normalized, CurrGroundNormal);
        Velocity *= 1 - PotentialLostRatio * dot;

        // ���x�����˂ɂ���ĕύX���ꂽ�̂ŁA���̏�Œ�~��Ԃɖ����������`�F�b�N
        // ���݂̕��ʂƐ����ʂ̊p�x��Cos�����߂�
        float cos = Vector3.Dot(CurrGroundNormal, Vector3.up);
        float mass = 1.0f / Mass;

        // �d�͂��v�Z���A��~��Ԃ��`�F�b�N
        Vector3 gravity = Vector3.down * Gravity;
        CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + gravity);

        // ��C��R�͂��v�Z���A��~��Ԃ��`�F�b�N
        Vector3 airfriction = ComputeAirFriction(mass);
        CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + airfriction);

        // ���C�͂��v�Z���A��~��Ԃ��`�F�b�N
        Vector3 friction = ComputeFriction(mass, cos);
        CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + friction);

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

        RayMoveDir.direction = Velocity.normalized;
        RayMoveDir.origin = transform.position;

        //// �i�s�����Ɛڒn�`�F�b�N����Raycast�����������ł��邩���`�F�b�N
        //// �����̏ꍇ�A���łɃ`�F�b�N�ς݂Ȃ̂ŁA�ă`�F�b�N���s�v
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
            return OnHit;
        }

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
        if (RayHitInfoMoveDir.distance > Collider.radius - Bias)
        {
            return OnHit;
        }

        // �Փ˂������ʂ����RayCast�ł̏Փ˂��镽�ʂƓ����ł���΁A�Ԃ��Ȃ��Փ˂Ɣ��f�ł���
        // �o��镽�ʂ̍ő�p�x��cos�����߂�
        // �Փ˂������ʂ̖@�������ƂɁA�i�s�����𔽎˂�����
        // ���ˌ�̃x�N�g���ƏՓ˂������ʂ̖@���Ƃ̓��ς����߂�
        // ���߂����ς����߂�cos��菬�����ꍇ�A���ʂɓo���Ɣ��f�ł���
        // �Փ˂������ʂ������ʂ̏ꍇ�A���̂܂ܔ��ˏ���������
        // �ǂ�����A���ʂƏՓ˂����̂ŁAcos�������đ�������0�`PotentialLostRatio�ɂ���
        float cos = Mathf.Cos(SlopeLimit * Mathf.Deg2Rad);
        Velocity *= 1 - PotentialLostRatio * cos;
        Vector3 reflectVel = Vector3.Reflect(Velocity, RayHitInfoMoveDir.normal);
        dot = Vector3.Dot(reflectVel.normalized, RayHitInfoMoveDir.normal);
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

            Velocity = Quaternion.AngleAxis(angle, axis.normalized) * Velocity;
            VelocityLen = Velocity.magnitude;

            // ���x����]�ɂ���ĕύX���ꂽ�̂ŁA���̏�Œ�~��Ԃɖ����������`�F�b�N
            // �Փ˂������ʂƐ����ʂ̊p�x��Cos�����߂�
            cos = Vector3.Dot(RayHitInfoMoveDir.normal, Vector3.up);
            float mass = 1.0f / Mass;

            // �d�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 gravity = Vector3.down * Gravity;
            CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + gravity);

            // ��C��R�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 airfriction = ComputeAirFriction(mass);
            CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + airfriction);

            // ���C�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 friction = ComputeFriction(mass, cos);
            CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + friction);

            CurrGround = RayHitInfoMoveDir.transform;
            CurrGroundNormal = RayHitInfoMoveDir.normal;
            OnGround = true;
        }
        // �o��Ȃ��ꍇ�A���ˏ���
        else
        {
            Velocity = reflectVel;
            VelocityLen = Velocity.magnitude;

            // ���x�����˂ɂ���ĕύX���ꂽ�̂ŁA���̏�Œ�~��Ԃɖ����������`�F�b�N
            // �Փ˂������ʂƐ����ʂ̊p�x��Cos�����߂�
            cos = Vector3.Dot(RayHitInfoMoveDir.normal, Vector3.up);
            float mass = 1.0f / Mass;

            // �d�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 gravity = Vector3.down * Gravity;
            CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + gravity);

            // ��C��R�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 airfriction = ComputeAirFriction(mass);
            CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + airfriction);

            // ���C�͂��v�Z���A��~��Ԃ��`�F�b�N
            Vector3 friction = ComputeFriction(mass, cos);
            CheckAndSetVelocityInfoAfterCancelOut(Velocity, Velocity + friction);

            //CurrGround = RayHitInfoMoveDir.transform;
            //CurrGroundNormal = RayHitInfoMoveDir.normal;
            OnGround = false;
        }

        // �߂荞�݂�␳����
        var pos = transform.position;
        pos = RayHitInfoMoveDir.point + RayHitInfoMoveDir.normal * Collider.radius;
        transform.position = pos;

        OnHit = true;

        return OnHit;

    }

    // �Î~�����F
    // ���x�����x��DeadZone�ȉ��A
    // �O�t���[���̉����x�������x��DeadZone�ȉ�
    public bool IsStop()
    {
        return 
            Velocity.magnitude <= VelocityDeadZone &&
            Acceleration.magnitude <= AccelerationDeadZone;
    }

    // �Î~�����F
    // �����ʂɂ���ꍇ�A���x�̑傫����DeadZone�ȉ�
    // �X�V��̑��x�̕������X�V�O�̑��x�̕������t
    // ���C�́A��C��R�͂�K�p��A�v�`�F�b�N
    public bool IsStopAfterCancelOut(Vector3 before, Vector3 after)
    {
        return 
            (after * Time.deltaTime).magnitude <= VelocityDeadZone ||
            Vector3.Dot(before.normalized, -after.normalized) >= 1.0f - Bias;
    }

    // �����ʂɂ��āA�X�V��ɒ�~�Ɣ��f�����ꍇ
    // ���x�Ɖ����x�S��0�ɂ��āA��~��Ԃɂ��āAtrue��Ԃ�
    public bool CheckAndSetVelocityInfoAfterCancelOut(Vector3 before, Vector3 after)
    {
        if (IsStopAfterCancelOut(before, after))
        {
            Velocity = Vector3.zero;
            VelocityLen = 0.0f;
            Acceleration = Vector3.zero;
            AccelerationFriction = Vector3.zero;
            AccelerationLen = 0.0f;

            OnGround = true;
            return true;
        }
        return false;
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

        Debug.DrawLine(transform.position, transform.position + Velocity.normalized * DebugLineLen, Color.black);
    }
}
