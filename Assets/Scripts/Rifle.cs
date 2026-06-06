using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rifle : MonoBehaviour
{
    [Header("Rifle Things")]
    public Camera camera;
    public float giveDamageOf = 10f;
    public float shootingRange = 100f;
    public float fireCharge = 15f;
    public Animator animator;
    public PlayerScript player;

    [Header("Gunshot Alert")]
    public bool alertEnemiesWhenShooting = true;
    public float gunShotAlertRadius = 35f;

    [Header("Rifle Ammunition and shooting")]
    private int maximumAmmunition = 20;
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
        }

        if (AmmoCount.occurrence != null)
        {
            AmmoCount.occurrence.UpdateAmmoText(presentAmmunition);
            AmmoCount.occurrence.UpdateMagText(mag);
        }
    }

    void Update()
    {
        // BUG DÜZELTMESİ: Oyuncu reload sırasında ölürse hızı sıfırda kalmasın
        if (setReloading && player != null && player.IsDead())
        {
            StopAllCoroutines();
            setReloading = false;
            player.playerSpeed = originalPlayerSpeed;
            player.playerSprint = originalPlayerSprint;
            SetAnimatorBoolIfExists("Reloading", false);
            return;
        }

        if (setReloading)
        {
            SetAnimatorBoolIfExists("Fire", false);
            SetAnimatorBoolIfExists("FireWalk", false);
            return;
        }

        if (player != null && !player.IsOnGround())
        {
            SetAnimatorBoolIfExists("Fire", false);
            SetAnimatorBoolIfExists("FireWalk", false);
            SetAnimatorBoolIfExists("Reloading", false);
            return;
        }

        bool playerSitting = player != null && player.IsSitting();

        if (!playerSitting && Input.GetKeyDown(KeyCode.R) && presentAmmunition < maximumAmmunition && mag > 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (!playerSitting && presentAmmunition <= 0 && mag > 0)
        {
            StartCoroutine(Reload());
            return;
        }

        bool isShooting = Input.GetButton("Fire1");

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

        if (isShooting)
        {
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

            SetAnimatorBoolIfExists("Reloading", false);

            if (Time.time >= nextTimeToShoot)
            {
                nextTimeToShoot = Time.time + 1f / fireCharge;
                Shoot();
            }
        }
        else
        {
            SetAnimatorBoolIfExists("Fire", false);
            SetAnimatorBoolIfExists("FireWalk", false);
            SetAnimatorBoolIfExists("Reloading", false);
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

        if (muzzleSpark != null)
        {
            muzzleSpark.Play();
        }

        if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy && shootingSound != null)
        {
            audioSource.PlayOneShot(shootingSound);
        }

        if (alertEnemiesWhenShooting && player != null)
        {
            player.MakeGunShotNoise(gunShotAlertRadius);
        }

        if (camera == null)
        {
            return;
        }

        RaycastHit hitInfo;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hitInfo, shootingRange))
        {
            Objects objects = hitInfo.transform.GetComponentInParent<Objects>();
            Enemy enemy = hitInfo.transform.GetComponentInParent<Enemy>();

            if (objects != null)
            {
                objects.objectHitDamage(giveDamageOf);

                if (impactEffect != null)
                {
                    GameObject impactGo = Instantiate(
                        impactEffect,
                        hitInfo.point,
                        Quaternion.LookRotation(hitInfo.normal)
                    );

                    Destroy(impactGo, 1f);
                }
            }
            else if (enemy != null)
            {
                enemy.enemyHitDamage(giveDamageOf);

                if (goreEffect != null)
                {
                    GameObject impactGo = Instantiate(
                        goreEffect,
                        hitInfo.point,
                        Quaternion.LookRotation(hitInfo.normal)
                    );

                    Destroy(impactGo, 2f);
                }
            }
        }
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

        if (player != null && player.IsSitting())
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
        }

        Debug.Log("Reloading....");

        if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy && reloadingSound != null)
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
        }

        SetAnimatorBoolIfExists("Reloading", false);

        setReloading = false;
    }

    IEnumerator ShowAmmoOut()
    {
        if (AmmoOutUI != null)
        {
            AmmoOutUI.SetActive(true);
            yield return new WaitForSeconds(timeToShowUI);
            AmmoOutUI.SetActive(false);
        }
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

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterName, value);
        }
    }
}