using UnityEngine;

public class AllyWeaponSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip shootingSound;
    [Range(0f, 1f)] public float volume = 1f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayShootingSound()
    {
        if (audioSource == null)
        {
            return;
        }

        if (shootingSound == null)
        {
            return;
        }

        if (!audioSource.enabled || !audioSource.gameObject.activeInHierarchy)
        {
            return;
        }

        audioSource.PlayOneShot(shootingSound, volume);
    }
}