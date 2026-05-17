
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

    [Header("Sounds & UI")]
    public AudioClip shootingSound;
    public AudioClip reloadingSound;
    public AudioSource audioSource;

    private void Awake()
    {
        presentAmmunition = maximumAmmunition;
    }

    void Update()
    {
        if (setReloading)
            return;

        if (Input.GetKeyDown(KeyCode.R) && presentAmmunition < maximumAmmunition && mag > 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (presentAmmunition <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        bool isShooting = Input.GetButton("Fire1");

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

        if (isShooting && Time.time >= nextTimeToShoot)
        {
            animator.SetBool("Fire", true);
            animator.SetBool("Idle", false);
            animator.SetBool("FireWalk", isMoving);
            animator.SetBool("Reloading", false);

            nextTimeToShoot = Time.time + 1f / fireCharge;

            Shoot();
        }
        else if (isShooting && isMoving)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Fire", false);
            animator.SetBool("FireWalk", true);
            animator.SetBool("Reloading", false);
        }
        else if (isShooting)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("FireWalk", false);
            animator.SetBool("Reloading", false);
        }
        else
        {
            animator.SetBool("Fire", false);
            animator.SetBool("FireWalk", false);
            animator.SetBool("Reloading", false);
        }
    }

    void Shoot()
    {
        if (mag == 0)
        {
            return;
        }

        presentAmmunition--;

        if (presentAmmunition == 0)
        {
            mag--;
        }

        muzzleSpark.Play();
        audioSource.PlayOneShot(shootingSound);

        RaycastHit hitInfo;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out hitInfo, shootingRange))
        {
            Debug.Log(hitInfo.transform.name);

            Objects objects = hitInfo.transform.GetComponent<Objects>();

            if (objects != null)
            {
                objects.objectHitDamage(giveDamageOf);

                GameObject impactGo = Instantiate(
                    impactEffect,
                    hitInfo.point,
                    Quaternion.LookRotation(hitInfo.normal)
                );

                Destroy(impactGo, 1f);
            }
        }
    }

    IEnumerator Reload()
    {
        player.playerSpeed = 0f;
        player.playerSprint = 0f;

        setReloading = true;

        Debug.Log("Reloading....");

        animator.SetBool("Reloading", true);

        audioSource.PlayOneShot(reloadingSound);

        yield return new WaitForSeconds(reloadingTime);

        animator.SetBool("Reloading", false);

        presentAmmunition = maximumAmmunition;

        player.playerSpeed = 3f;
        player.playerSprint = 6f;

        setReloading = false;
    }
}