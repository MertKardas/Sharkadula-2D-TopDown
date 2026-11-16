using VContainer;
using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisualComponent : MonoBehaviour {
    private AudioService _audio;

    [Inject]
    public void Construct(AudioService audio) {
        _audio = audio;
    }

    private PlayerConfiguration _configuration;

    [Header("Animation Parameters")]
    public static readonly int MovinSpeedgHash = Animator.StringToHash("Move");
    public static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    public static readonly int MoveSpeedParamHash = Animator.StringToHash("MoveSpeed");
    public static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
    public static readonly int DeathHash = Animator.StringToHash("Death");
    public static readonly int InputX = Animator.StringToHash("InputX");
    public static readonly int InputY = Animator.StringToHash("InputY");
    public Vector2 LastDirection = Vector2.down;

    // Events
    public event Action OnFinishTakeDamage;
    public event Action OnFinishDeathAnimation;

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    public void Initialize(PlayerConfiguration configuration) {
        _configuration = configuration;
        _animator = GetComponent<Animator>();
        if (_animator == null) {
            Debug.LogError("Animator component not found on PlayerVisualComponent!");
            return;
        }
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null) {
            Debug.LogError("SpriteRenderer component not found on PlayerVisualComponent!");
            return;
        }
    }

    public void SetMoving(bool isMoving) {
        _animator.SetFloat(MovinSpeedgHash, isMoving ? 1f : 0f);
    }

    public void SetRunning(bool isRunning) {
        SetMoving(isRunning);
        _animator.SetFloat(MoveSpeedParamHash, isRunning ? _configuration.RunAnimationSpeedMultiplier : 1f);
    }

    public void PlayFootstepSound() {
        _audio.PlaySound(_configuration.FootstepClip, transform.position);
    }

    public void AnimationDirection(Vector2 input) {
        Vector2 direction;

        if (input.sqrMagnitude < 0.01f) {
            direction = LastDirection;
        } else {
            bool isDiagonal = Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) > 0.1f;

            // Preserve the last facing axis when input is diagonal.
            if (isDiagonal) {
                if (Mathf.Abs(LastDirection.x) > 0.1f) {
                    direction = new Vector2(Mathf.Sign(input.x), 0);
                } else {
                    direction = new Vector2(0, Mathf.Sign(input.y));
                }
            } else {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y)) {
                    direction = new Vector2(Mathf.Sign(input.x), 0);
                } else {
                    direction = new Vector2(0, Mathf.Sign(input.y));
                }
            }

            LastDirection = direction;
        }

        // Her iki parametreyi de set et
        _animator.SetFloat(InputX, direction.x);
        _animator.SetFloat(InputY, direction.y);
    }
    public Vector2 GetLastDirection() => LastDirection;

    public void TriggerTakeDamage() {
        _animator.SetTrigger(TakeDamageHash);
        _audio.PlaySound(_configuration.TakeDamageClip, transform.position);
    }
    public void TriggerDeath() {
        _audio.StopSound(_configuration.TakeDamageClip);
        _animator.SetTrigger(DeathHash);
        _audio.PlaySound(_configuration.DeathClip, transform.position);
    }
    public void FinishTakingDamage() {
        OnFinishTakeDamage?.Invoke();
    }
    public void FinishDeathAnimation() {
        OnFinishDeathAnimation?.Invoke();
        _spriteRenderer.enabled = false;
    }
}
