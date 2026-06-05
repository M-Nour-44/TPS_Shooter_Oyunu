using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Ally : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public Transform player; 
    public float followDistance = 3f; // Durma mesafesi

    [Header("Hız Ayarları")]
    public float walkSpeed = 2f; // Yürüme hızı
    public float runSpeed = 5f;  // Koşma hızı
    public float runDistanceThreshold = 6f; // Oyuncu bu mesafeden fazla uzaklaşırsa koş

    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); 
        
        agent.stoppingDistance = followDistance;
    }

    void Update()
    {
        if (player != null)
        {
            // Oyuncu ile Ally arasındaki mesafeyi hesapla
            float distance = Vector3.Distance(transform.position, player.position);

            // Mesafeye göre hızı dinamik olarak değiştir
            if (distance > runDistanceThreshold)
            {
                agent.speed = runSpeed; // Oyuncu uzaklaştı, koşarak yetiş!
            }
            else
            {
                agent.speed = walkSpeed; // Oyuncu yakın, sakin yürü.
            }

            // Hedefi güncelle
            agent.SetDestination(player.position);
        }

        // Mevcut hızı Animator'a gönder
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);
    }
}