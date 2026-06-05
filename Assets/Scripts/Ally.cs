using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Ally : MonoBehaviour
{
    [Header("Hedef Ayarları")]
    public Transform player; // Inspector'dan kendi (Player) karakterini buraya sürükle
    public float followDistance = 3f; // Oyuncuya ne kadar yaklaşacağı

    private NavMeshAgent agent;

    void Start()
    {
        // Bileşeni otomatik olarak çekiyoruz
        agent = GetComponent<NavMeshAgent>();
        
        // Karakterin seni ittirmemesi için NavMesh üzerinde bir durma mesafesi belirliyoruz
        agent.stoppingDistance = followDistance;
    }

    void Update()
    {
        // Eğer oyuncu sahnedeyse, NavMesh hedefini sürekli oyuncunun pozisyonu olarak güncelle
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
    }
}