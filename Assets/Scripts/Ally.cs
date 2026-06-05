using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))] // Ana objede Animator olmasını zorunlu kılar
public class Ally : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public Transform player; 
    public float followDistance = 3f; 

    private NavMeshAgent agent;
    private Animator animator; // Animator referansımız

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
            // Hedefi güncelle
            agent.SetDestination(player.position);
        }

        // --- ANİMASYON KISMI ---
        // NavMeshAgent'ın mevcut hız vektörünün büyüklüğünü (skaler hızını) al
        float currentSpeed = agent.velocity.magnitude;

        // Animator'daki "Speed" isimli float parametresine bu hızı gönder
        animator.SetFloat("Speed", currentSpeed);
    }
}