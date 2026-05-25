using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    [Header("Weapon Sounds")]
    public AudioSource audioSource;
    public AudioClip shootingSound;

    public void PlayShootingSound()
    {
        if (audioSource != null && shootingSound != null)
        {
            audioSource.PlayOneShot(shootingSound);
        }
    }
}