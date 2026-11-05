using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý thứ tự sử dụng skill cho EvilWitch, luân phiên giữa:
///   Phase 0 → EvilWitchSpreadFireball (chiêu tản cầu lửa hướng Player)
///   Phase 1 → SpreadFireball Tengu    (chiêu bắn tròn 360°)
///
/// ──────────────────────────────────────────────────────────────
/// Cách thiết lập:
///   1. Gắn script này lên cùng GameObject với EvilWitchAttack.
///   2. Gắn thêm EvilWitchSpreadFireball và SpreadFireball lên cùng GameObject.
///      (Tắt autoTriggerWithMainAttack trên EvilWitchSpreadFireball — Sequencer điều khiển hoàn toàn.)
///   3. Trong Animation clip của EvilWitch, thêm Animation Event tại keyframe cuối
///      gọi hàm: ExecuteCurrentSkill
/// ──────────────────────────────────────────────────────────────
/// </summary>
public class EvilWitchSkillSequencer : MonoBehaviour
{
    // ─── References ────────────────────────────────────────────────────────────

    [Header("Skill References")]
    [Tooltip("Chiêu Phase 0: Spray Fireball hướng về Player (EvilWitchSpreadFireball trên cùng GO)")]
    [SerializeField] private EvilWitchSpreadFireball witchSpread;

    [Tooltip("Chiêu Phase 1: Bắn tròn 360° kiểu Tengu (SpreadFireball trên cùng GO)")]
    [SerializeField] private SpreadFireball tenguSpread;

    [Tooltip("Chiêu Phase 2: Cầu lửa bám đuổi Player cho tới khi chạm Wall (EvilWitchHomingFireball trên cùng GO)")]
    [SerializeField] private EvilWitchHomingFireball homingFireball;

    // ─── Debug ─────────────────────────────────────────────────────────────────

    [Header("Debug (Read-only)")]
    [SerializeField, Tooltip("Phase hiện tại (0 = Witch Spread, 1 = Tengu Radial)")]
    private int currentPhase = 0;

    // Nếu Homing Fireball đang tự chạy theo cooldown (autoCast) thì Sequencer chỉ luân phiên 2 phase đầu.
    private int PhaseCount => (homingFireball != null && homingFireball.AutoCast) ? 2 : 3;

    // ─── Vòng đời ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-resolve nếu chưa gán trong Inspector
        if (witchSpread == null) witchSpread = GetComponent<EvilWitchSpreadFireball>();
        if (tenguSpread == null) tenguSpread  = GetComponent<SpreadFireball>();
        if (homingFireball == null) homingFireball = GetComponent<EvilWitchHomingFireball>();
    }

    private void Start()
    {
        // Cảnh báo thiếu reference
        if (witchSpread == null)
            Debug.LogWarning("[EvilWitchSkillSequencer] Thiếu EvilWitchSpreadFireball!", this);
        if (tenguSpread == null)
            Debug.LogWarning("[EvilWitchSkillSequencer] Thiếu SpreadFireball (Tengu)!", this);
    }

    // ─── Animation Event Entry-point ───────────────────────────────────────────

    /// <summary>
    /// Gọi từ Animation Event trên keyframe cuối của clip Attack.
    /// Mỗi lần gọi sẽ kích hoạt skill theo phase hiện tại rồi chuyển sang phase kế tiếp.
    /// </summary>
    public void ExecuteCurrentSkill()
    {
        var owner = GetComponent<Health>();
        if (!isActiveAndEnabled || (owner != null && owner.IsDead)) return;
        if (TryGetComponent<EnemySkillStatus>(out var status) && status.Immobilized) return;
        switch (currentPhase)
        {
            case 0:
                FireWitchSpread();
                break;
            case 1:
                FireTenguRadial();
                break;
            case 2:
                FireHomingFireball();
                break;
        }

        // Chuyển sang phase kế tiếp (vòng lặp 2 phase)
        currentPhase = (currentPhase + 1) % PhaseCount;
    }

    // ─── Thực thi skill ────────────────────────────────────────────────────────

    private void FireWitchSpread()
    {
        if (witchSpread == null)
        {
            Debug.LogWarning("[EvilWitchSkillSequencer] Phase 0: EvilWitchSpreadFireball chưa được gán!", this);
            return;
        }

        witchSpread.FireSpreadAttack();
    }

    private void FireTenguRadial()
    {
        if (tenguSpread == null)
        {
            Debug.LogWarning("[EvilWitchSkillSequencer] Phase 1: SpreadFireball (Tengu) chưa được gán!", this);
            return;
        }

        tenguSpread.CastRadialFireBalls();
    }

    private void FireHomingFireball()
    {
        if (homingFireball == null)
        {
            Debug.LogWarning("[EvilWitchSkillSequencer] Phase 2: EvilWitchHomingFireball chưa được gán!", this);
            return;
        }

        homingFireball.CastHomingFireball();
    }


    // ─── Tiện ích ──────────────────────────────────────────────────────────────

    /// <summary>Reset về phase 0 (ví dụ: khi Witch bị re-spawn hoặc bắt đầu màn mới).</summary>
    public void ResetSequence() => currentPhase = 0;

    /// <summary>Chuyển thẳng đến phase cụ thể (dùng để debug hoặc scripted event).</summary>
    public void SetPhase(int phase) => currentPhase = Mathf.Clamp(phase, 0, PhaseCount - 1);
}
