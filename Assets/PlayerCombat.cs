using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public float attackRange = 1.7f;
    public float attackDamage = 20f;
    public Transform attackOrigin;     // vide placé devant la main/torse
    public LayerMask enemyMask;        // couche "Enemy" si tu en crées une

    // Appelée par un Animation Event dans le clip d'attaque du Player
    public void AnimationAttackHit()
    {
        Vector3 origin = attackOrigin ? attackOrigin.position
            : transform.position + transform.forward * 0.8f + Vector3.up * 1.0f;

        Collider[] hits = Physics.OverlapSphere(origin, attackRange, enemyMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var hp = h.GetComponentInParent<Health>();
            if (hp != null) hp.TakeDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!attackOrigin) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
    }
}
