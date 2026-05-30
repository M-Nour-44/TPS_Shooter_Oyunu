using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public int magazineAmount = 1;

    [Header("Pickup Sound")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float pickupVolume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerScript player = other.GetComponentInParent<PlayerScript>();

        if (player == null)
        {
            return;
        }

        Rifle rifle = player.GetComponentInChildren<Rifle>();

        if (rifle == null)
        {
            rifle = FindObjectOfType<Rifle>();
        }

        if (rifle != null)
        {
            rifle.AddMagazine(magazineAmount);

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            }

            Destroy(gameObject);
        }
    }
}