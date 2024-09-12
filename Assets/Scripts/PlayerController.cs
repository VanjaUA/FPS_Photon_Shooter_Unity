using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform viewPoint;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float mouseSensitivity = 1f;
    private float verticalRotationStore;
    private Vector2 mouseInput;

    [SerializeField] private bool invertLook;

    [SerializeField] private float walkSpeed, runSpeed;
    [SerializeField] private float jumpForce, gravityModifier;
    private float activeMoveSpeed;
    private Vector3 moveDirection, movement;
    private bool isGrounded;

    private CharacterController characterController;

    private Camera mainCamera;

    [SerializeField] private GameObject bulletImpact;
    [SerializeField] private float maxHeat = 10f, coolRate = 4f, overHeatCoolRate = 5f;
    private float shotCounter;
    private float heatCounter;
    private bool overHeated;

    [SerializeField] private float muzzleDisplayTime;
    private float muzzleCounter;


    [SerializeField] private Gun[] allGuns;
    private int selectedGun;

    [SerializeField] private GameObject playerHitImpactPrefab;

    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [SerializeField] private Animator animator;
    private const string ANIMATOR_GROUNDED = "grounded";
    private const string ANIMATOR_SPEED = "speed";
    [SerializeField] private GameObject playerModel;
    [SerializeField] private Transform modelGunPoint, gunHolder;

    [SerializeField] private Material[] allSkins;

    [SerializeField] private float zoomSpeed = 5f;

    [SerializeField] private Transform zoomOutPoint, zoomInPoint;

    [SerializeField] private AudioSource footstepSlow, footstepFast;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        mainCamera = Camera.main;

        UIController.Instance.weaponTempSlider.maxValue = maxHeat;

        photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);

        Transform newTransform = SpawnManager.Instance.GetSpawnPoint();
        transform.position = newTransform.position;
        transform.rotation = newTransform.rotation;

        currentHealth = maxHealth;

        if (photonView.IsMine)
        {
            playerModel.SetActive(false);

            UIController.Instance.UpdateHealthBar(currentHealth, maxHealth);
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        playerModel.GetComponent<Renderer>().material = allSkins[photonView.Owner.ActorNumber % allSkins.Length];
    }

    void Update()
    {
        if (photonView.IsMine == false)
        {
            return;
        }

        RotationHandle();

        MoveHandle();

        GunHandle();

        GunChangingHandle();

        animator.SetBool(ANIMATOR_GROUNDED, isGrounded);
        animator.SetFloat(ANIMATOR_SPEED, moveDirection.magnitude);

        ZoomHandle();

        CursorHandle();
    }

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (MatchManager.Instance.gameState == MatchManager.GameState.Playing)
            {
                mainCamera.transform.position = viewPoint.position;
                mainCamera.transform.rotation = viewPoint.rotation;
            }
            else
            {
                mainCamera.transform.position = MatchManager.Instance.camPoint.position;
                mainCamera.transform.rotation = MatchManager.Instance.camPoint.rotation;
            }
        }
    }

    private void RotationHandle()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        verticalRotationStore -= mouseInput.y;
        verticalRotationStore = Mathf.Clamp(verticalRotationStore, -60f, 60f);

        viewPoint.rotation = Quaternion.Euler((invertLook ? -1f : 1f) * verticalRotationStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
    }

    private void MoveHandle()
    {
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;

            if (footstepFast.isPlaying == false && moveDirection != Vector3.zero)
            {
                footstepFast.Play();
                footstepSlow.Stop();
            }
        }
        else
        {
            activeMoveSpeed = walkSpeed;

            if (footstepSlow.isPlaying == false && moveDirection != Vector3.zero)
            {
                footstepFast.Stop();
                footstepSlow.Play();
            }
        }

        if (moveDirection == Vector3.zero || isGrounded == false)
        {
            footstepSlow.Stop();
            footstepFast.Stop();
        }

        float yVelocity = movement.y;
        movement = ((transform.right * moveDirection.x) + (transform.forward * moveDirection.z)).normalized * activeMoveSpeed;
        movement.y = yVelocity;

        if (characterController.isGrounded)
        {
            movement.y = 0;
        }

        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayers);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            movement.y = jumpForce;
        }

        movement.y += Physics.gravity.y * gravityModifier * Time.deltaTime;

        characterController.Move(movement * Time.deltaTime);
    }

    private void GunHandle() 
    {
        if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;
            if (muzzleCounter <= 0)
            {
                allGuns[selectedGun].muzzleFlash.SetActive(false);
            }
        }

        if (overHeated == false)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }

            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
            {
                shotCounter -= Time.deltaTime;

                if (shotCounter <= 0f)
                {
                    Shoot();
                }
            }

            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overHeatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {
                overHeated = false;
                UIController.Instance.overheatedMessage.gameObject.SetActive(false);
            }
        }

        if (heatCounter <= 0)
        {
            heatCounter = 0;
        }

        UIController.Instance.weaponTempSlider.value = heatCounter;
    }

    private void GunChangingHandle() 
    {
        if (Input.mouseScrollDelta.y > 0f)
        {
            selectedGun++;
            if (selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
            }
            photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);
        }
        else if (Input.mouseScrollDelta.y < 0f)
        {
            selectedGun--;
            if (selectedGun < 0)
            {
                selectedGun = allGuns.Length - 1;
            }
            photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);
        }

        for (int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);
            }
        }
    }

    private void CursorHandle() 
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            if (UIController.Instance.optionsScreen.activeInHierarchy == false)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void ZoomHandle() 
    {
        if (Input.GetMouseButton(1))
        {
            mainCamera.fieldOfView =  Mathf.Lerp(mainCamera.fieldOfView, allGuns[selectedGun].zoomValue, zoomSpeed * Time.deltaTime);
            gunHolder.position = Vector3.Lerp(gunHolder.position, zoomInPoint.position, zoomSpeed * Time.deltaTime);
        }
        else
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 60f, zoomSpeed * Time.deltaTime);
            gunHolder.position = Vector3.Lerp(gunHolder.position, zoomOutPoint.position, zoomSpeed * Time.deltaTime);
        }
    }

    private void Shoot() 
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,0f));
        ray.origin = mainCamera.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player"))
            {
                PhotonNetwork.Instantiate(playerHitImpactPrefab.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC(nameof(DealDamage),RpcTarget.All,photonView.Owner.NickName,
                    allGuns[selectedGun].shotDamage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + hit.normal * 0.002f, Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 5f);
            }
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;
            overHeated = true;

            UIController.Instance.overheatedMessage.gameObject.SetActive(true);
        }

        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;

        allGuns[selectedGun].shotSound.Stop();
        allGuns[selectedGun].shotSound.Play();
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo) 
    {
        if (gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }

    private void SwitchGun() 
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor) 
    {
        TakeDamage(damager, damageAmount, actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor) 
    {
        if (photonView.IsMine)
        {
            Debug.Log(photonView.Owner.NickName + " has been hit by " + damager);

            currentHealth -= damageAmount;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.Instance.Die(damager);

                MatchManager.Instance.UpdateStateSend(actor,0,1);
            }
            UIController.Instance.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
}
