using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(Health))]
public class ZombieAI : MonoBehaviour
{
    public float attackRange = 1.6f;
    public float attackDamage = 12f;
    public float attackCooldown = 1.2f;

    NavMeshAgent agent;
    Animator anim;
    Health hp;
    Transform player;
    bool playerInZone;
    float nextAtk;

    void Awake(){
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();
        hp    = GetComponent<Health>();
        hp.OnDied += ()=>{ anim.SetBool("Dead", true); if(agent) agent.isStopped = true; enabled = false; };
    }

    void Start(){ EnsureOnNavMesh(); }

    bool EnsureOnNavMesh(){
        if (agent && agent.isOnNavMesh) return true;
        if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas)){
            agent.Warp(hit.position);  // “snap” sur le NavMesh
            return true;
        }
        return false;
    }

    void OnTriggerEnter(Collider other){
        if (other.CompareTag("Player")) { player = other.transform; playerInZone = true; }
    }

    void OnTriggerExit(Collider other){
        if (other.CompareTag("Player")) { player = null; playerInZone = false; if(agent && agent.isOnNavMesh) agent.ResetPath(); anim.SetBool("IsMoving", false); }
    }

    void Update(){
        if (hp.IsDead) return;
        if (!playerInZone || !player) { anim.SetBool("IsMoving", false); return; }

        if (!agent || !agent.enabled || !agent.isOnNavMesh){
            if (!EnsureOnNavMesh()) return;
        }

        agent.stoppingDistance = attackRange * 0.9f;
        agent.SetDestination(player.position);
        anim.SetBool("IsMoving", agent.velocity.sqrMagnitude > 0.05f);

        float d = Vector3.Distance(transform.position, player.position);
        if (d <= attackRange && Time.time >= nextAtk){
            nextAtk = Time.time + attackCooldown;
            anim.SetTrigger("Attack");
        }
    }

    // Appelé par l'Animation Event du clip Attack
    public void AnimationAttackHit(){
        if (!player) return;
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 0.2f){
            var targetHp = player.GetComponent<Health>();
            if (targetHp) targetHp.TakeDamage(attackDamage);
        }
    }
}
