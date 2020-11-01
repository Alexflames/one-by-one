using UnityEngine;

public abstract class Attack : EnemyBehavior
{
    [SerializeField, Header("Attack Block")]
    protected Vector2 cooldownRange = new Vector2(1f, 1f);
    [HideInInspector] public float attackSpeedModifier = 1f;
    [SerializeField] protected AudioClip attackSound = null;

    protected override void Awake()
    {
        base.Awake();
        cooldownLeft = Random.Range(cooldownRange.x, cooldownRange.y);
    }

    public override void CalledUpdate()
    {
        base.CalledUpdate();
        if (isActive)
        {
            cooldownLeft = Mathf.Max(cooldownLeft - Time.deltaTime, 0);
            if (cooldownLeft <= 0)
            {
                cooldownLeft = Random.Range(cooldownRange.x, cooldownRange.y) / attackSpeedModifier;
                DoAttack();
            }
        }
    }

    public void ForceAttack()
    {
        cooldownLeft = Random.Range(cooldownRange.x, cooldownRange.y) / attackSpeedModifier;
        DoAttack();
    }

    protected abstract void DoAttack();

    protected float cooldownLeft;
}