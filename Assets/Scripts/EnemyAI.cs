using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator anim;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float waitAtPoint = 0.5f;
    int patrolIndex;
    float waitTimer;

    [Header("Detection")]
    public float detectRange = 10f;
    public float attackRange = 2f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;
    float atkCd;
    bool isAttacking;

    enum State { Patrol, Chase, Attack }
    State state = State.Patrol;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        GoToNextPatrol();
    }

    void Update()
    {
        if (!player) return;

        float d = Vector3.Distance(transform.position, player.position);

        if (d <= attackRange) state = State.Attack;
        else if (d <= detectRange) state = State.Chase;
        else state = State.Patrol;

        switch (state)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase:  Chase();  break;
            case State.Attack: Attack(); break;
        }

        if (anim) anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtPoint)
            {
                waitTimer = 0f;
                GoToNextPatrol();
            }
        }
    }

    void GoToNextPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void Chase()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void Attack()
    {
        agent.isStopped = true;
        FacePlayer();

        if (atkCd > 0f) atkCd -= Time.deltaTime;
        if (atkCd > 0f) return;
        if (isAttacking) return;

        atkCd = attackCooldown;
        isAttacking = true;
        if (anim) anim.SetTrigger("Attack");

        // desbloqueo simple (si tu anim dura distinto, ajustá)
        Invoke(nameof(EndAttack), 0.9f);
    }

    void EndAttack() => isAttacking = false;

    void FacePlayer()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
    }
}
