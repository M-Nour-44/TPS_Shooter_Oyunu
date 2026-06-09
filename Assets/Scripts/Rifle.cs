using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rifle : MonoBehaviour
{
    [Header("Rifle Things")]
    public Camera camera;
    public float giveDamageOf = 10f;
    public float shootingRange = 32f;
    public float fireCharge = 32f;
    public Animator animator;
    public PlayerScript player;

    [Header("Gunshot Alert")]
    public bool alertEnemiesWhenShooting = true;
    public float gunShotAlertRadius = 35f;

    [Header("Rifle Ammunition and shooting")]
    private int maximumAmmunition = 30;
    private int mag = 15;
    private int presentAmmunition;
    public float reloadingTime = 1.3f;
    private bool setReloading = false;
    private float nextTimeToShoot = 0f;

    [Header("Rifle Effects")]
    public ParticleSystem muzzleSpark;
    public GameObject impactEffect;
    public GameObject goreEffect;

    [Header("Sounds & UI")]
    public AudioClip shootingSound;
    public AudioClip reloadingSound;
    public AudioSource audioSource;
    [SerializeField] private GameObject AmmoOutUI;
    [SerializeField] private int timeToShowUI = 1;

    private float originalPlayerSpeed = 3f;
    private float originalPlayerSprint = 6f;
    private float originalPlayerSit = 1.5f;

    private void Awake()
    {
        presentAmmunition = maximumAmmunition;
    }

    private void Start()
    {
        if (player == null)
        {
            player = GetComponentInParent<PlayerScript>();
        }

        if (player == null)
        {
            player = FindObjectOfType<PlayerScript>();
        }

        if (player != null)
        {
            originalPlayerSpeed = player.playerSpeed;
            originalPlayerSprint = player.playerSprint;
            originalPlayerSit = player.sitSpeed;
        }

        if (AmmoCount.occurrence != null)
        {
            AmmoCount.occurrence.UpdateAmmoText(presentAmmunition);
            AmmoCount.occurrence.UpdateMagText(mag);
        }
    }

    void Update()
    {
        if (player == null || player.IsDead())
        {
            return;
        }

        HandleReloadStateLock();
        HandleReloadInput();
        HandleAutoReload();
        HandleShooting();
    }

    void HandleReloadStateLock()
    {
        if (!setReloading)
        {
            return;
        }

        SetAnimatorBoolIfExists("Fire", false);
        SetAnimatorBoolIfExists("FireWalk", false);
    }

    void HandleReloadInput()
    {
        if (setReloading)
        {
            return;
        }

        if (player != null && !player.IsOnGround())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R) && mag > 0 && presentAmmunition < maximumAmmunition)
        {
            StartCoroutine(Reload());
        }
    }

    void HandleAutoReload()
    {
        if (setReloading)
        {
            return;
        }

        if (mag <= 0)
        {
            return;
        }

        if (presentAmmunition <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    void HandleShooting()
    {
        if (setReloading)
        {
            return;
        }

        if (player != null && !player.IsOnGround())
        {
            return;
        }

        bool isShooting = Input.GetButton("Fire1");

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        if (!isShooting)
        {
            SetAnimatorBoolIfExists("Fire", false);
            SetAnimatorBoolIfExists("FireWalk", false);
            SetAnimatorBoolIfExists("Reloading", false);
            return;
        }

        SetAnimatorBoolIfExists("Reloading", false);

        if (isMoving)
        {
            SetAnimatorBoolIfExists("Fire", false);
            SetAnimatorBoolIfExists("FireWalk", true);
        }
        else
        {
            SetAnimatorBoolIfExists("Fire", true);
            SetAnimatorBoolIfExists("FireWalk", false);
        }

        if (Time.time >= nextTimeToShoot)
        {
            nextTimeToShoot = Time.time + 1f / fireCharge;
            Shoot();
        }
    }

    void Shoot()
    {
        if (presentAmmunition <= 0)
        {
            if (mag == 0)
            {
                StartCoroutine(ShowAmmoOut());
            }

            return;
        }

        presentAmmunition--;

        if (AmmoCount.occurrence != null)
        {
            AmmoCount.occurrence.UpdateAmmoText(presentAmmunition);
            AmmoCount.occurrence.UpdateMagText(mag);
        }

        if (muzzleSpark != null && muzzleSpark.gameObject.activeInHierarchy)
        {
            muzzleSpark.Play();
        }

        if (audioSource != null && shootingSound != null)
        {
            audioSource.PlayOneShot(shootingSound);
        }

        if (alertEnemiesWhenShooting && player != null)
        {
            player.MakeGunShotNoise(gunShotAlertRadius);
        }

        if (camera == null || player == null)
        {
            return;
        }

        Vector3 shootDir = camera.transform.forward;

        float spread = player.GetCurrentSpread();

        shootDir += new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            Random.Range(-spread, spread)
        );

        shootDir.Normalize();

        if (Physics.Raycast(camera.transform.position, shootDir, out RaycastHit hitInfo, shootingRange))
        {
            Vector3 chestOrigin = player.transform.position + Vector3.up * 1.4f;
            Vector3 dirToTarget = hitInfo.point - chestOrigin;

            int mask = ~LayerMask.GetMask("Player", "Ignore Raycast");

            if (Physics.Raycast(chestOrigin, dirToTarget.normalized, out RaycastHit chestHit, dirToTarget.magnitude, mask))
            {
                if (chestHit.transform != hitInfo.transform &&
                    Vector3.Distance(chestHit.point, hitInfo.point) > 0.3f)
                {
                    hitInfo = chestHit;
                }
            }

            Objects obj = hitInfo.transform.GetComponentInParent<Objects>();
            Enemy enemy = hitInfo.transform.GetComponentInParent<Enemy>();

            if (obj != null)
            {
                obj.objectHitDamage(giveDamageOf);
                SpawnImpact(hitInfo, impactEffect);
            }
            else if (enemy != null)
            {
                enemy.enemyHitDamage(giveDamageOf);
                SpawnImpact(hitInfo, goreEffect);
            }
            // --- الكود الجديد الذي يضيف أثر الرصاص على الجدران وأي شيء آخر ---
            else 
            {
                SpawnImpact(hitInfo, impactEffect);
            }
            // -----------------------------------------------------------
        }
    }

    void SpawnImpact(RaycastHit hit, GameObject fx)
    {
        if (fx == null)
        {
            return;
        }

        GameObject go = Instantiate(fx, hit.point, Quaternion.LookRotation(hit.normal));
        Destroy(go, 1.5f);
    }

    IEnumerator Reload()
    {
        if (setReloading)
        {
            yield break;
        }

        if (mag <= 0)
        {
            yield break;
        }

        if (player != null && !player.IsOnGround())
        {
            yield break;
        }

        setReloading = true;

        SetAnimatorBoolIfExists("Fire", false);
        SetAnimatorBoolIfExists("FireWalk", false);
        SetAnimatorBoolIfExists("Reloading", true);

        if (player != null)
        {
            player.playerSpeed = 0f;
            player.playerSprint = 0f;
            player.sitSpeed = 0f;
        }

        if (audioSource != null && reloadingSound != null)
        {
            audioSource.PlayOneShot(reloadingSound);
        }

        yield return new WaitForSeconds(reloadingTime);

        mag--;
        presentAmmunition = maximumAmmunition;

        if (AmmoCount.occurrence != null)
        {
            AmmoCount.occurrence.UpdateAmmoText(presentAmmunition);
            AmmoCount.occurrence.UpdateMagText(mag);
        }

        if (player != null)
        {
            player.playerSpeed = originalPlayerSpeed;
            player.playerSprint = originalPlayerSprint;
            player.sitSpeed = originalPlayerSit;
        }

        SetAnimatorBoolIfExists("Reloading", false);
        setReloading = false;
    }

    IEnumerator ShowAmmoOut()
    {
        if (AmmoOutUI == null)
        {
            yield break;
        }

        AmmoOutUI.SetActive(true);
        yield return new WaitForSeconds(timeToShowUI);
        AmmoOutUI.SetActive(false);
    }

    public void AddMagazine(int amount)
    {
        mag += amount;

        if (AmmoCount.occurrence != null)
        {
            AmmoCount.occurrence.UpdateAmmoText(presentAmmunition);
            AmmoCount.occurrence.UpdateMagText(mag);
        }
    }

    private bool HasAnimatorParameter(string name, AnimatorControllerParameterType type)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (var p in animator.parameters)
        {
            if (p.name == name && p.type == type)
            {
                return true;
            }
        }

        return false;
    }

    private void SetAnimatorBoolIfExists(string name, bool value)
    {
        if (HasAnimatorParameter(name, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(name, value);
        }
    }
}