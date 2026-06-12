using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FPSAimController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;
    public Transform playerBody;

    private Rigidbody rb;
    private Vector2 moveInput;

    [Header("Mouse / Câmera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 100f;
    public float minLookX = -80f;
    public float maxLookX = 80f;
    public bool lockCursor = true;

    private float cameraPitch = 0f;

    [Header("Arma")]
    public Transform weaponHolder;
    public GameObject currentWeapon;
    public bool hasWeapon = false;

    [Header("Ajuste Visual da Arma")]
    public Vector3 weaponLocalPosition = new Vector3(0f, 0f, 0f);
    public Vector3 weaponLocalRotation = new Vector3(0f, 90f, 0f);
    public Vector3 weaponLocalScale = new Vector3(0.2f, 0.2f, 0.2f);

    [Header("Tiro")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 50f;
    public float fireRate = 0.2f;
    public float maxShootDistance = 100f;
    public LayerMask targetLayer;
    public int maxAmmo = 30;
    public int currentAmmo = 30;

    private float nextFireTime = 0f;

    [Header("Recarregar")]
    public KeyCode reloadKey = KeyCode.R;
    public float reloadTime = 1.5f;
    private bool isReloading = false;

    [Header("Timer")]
    public float gameDuration = 60f;
    private float currentTime;
    private bool timerStarted = false;
    private bool gameEnded = false;

    [Header("UI")]
    public RectTransform crosshairRect;
    public TMP_Text ammoText;
    public TMP_Text scoreText;
    public TMP_Text gameOverText;
    public Button restartButton;

    private int score = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.freezeRotation = true;
        }

        if (playerBody == null)
        {
            playerBody = transform;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        currentAmmo = maxAmmo;
        currentTime = gameDuration;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(RestartGame);
        }

        UpdateUI();
    }

    void Update()
    {
        if (gameEnded)
        {
            return;
        }

        LookWithMouse();

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (timerStarted)
        {
            UpdateTimer();
        }

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }

        if (Input.GetKeyDown(reloadKey))
        {
            StartReload();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void FixedUpdate()
    {
        if (!gameEnded)
        {
            MovePlayerWithGravity();
        }
    }

    void LookWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        playerBody.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookX, maxLookX);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    void MovePlayerWithGravity()
    {
        if (rb == null)
        {
            return;
        }

        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        moveDirection.y = 0f;
        moveDirection.Normalize();

        Vector3 horizontalVelocity = moveDirection * moveSpeed;

        rb.linearVelocity = new Vector3(
            horizontalVelocity.x,
            rb.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    public void EquipWeapon(GameObject weapon, Transform weaponFirePoint)
    {
        if (weaponHolder == null)
        {
            Debug.LogWarning("WeaponHolder não foi configurado no Player.");
            return;
        }

        if (weaponFirePoint == null)
        {
            Debug.LogWarning("FirePoint da arma não foi configurado.");
            return;
        }

        currentWeapon = weapon;
        hasWeapon = true;

        if (!timerStarted)
        {
            timerStarted = true;
            currentTime = gameDuration;
        }

        weapon.transform.SetParent(weaponHolder);

        weapon.transform.localPosition = weaponLocalPosition;
        weapon.transform.localRotation = Quaternion.Euler(weaponLocalRotation);
        weapon.transform.localScale = weaponLocalScale;

        firePoint = weaponFirePoint;

        Rigidbody weaponRb = weapon.GetComponent<Rigidbody>();

        if (weaponRb != null)
        {
            weaponRb.isKinematic = true;
            weaponRb.useGravity = false;
        }

        Collider col = weapon.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        Debug.Log("Arma equipada!");
        UpdateUI();
    }

    void Shoot()
    {
        if (!hasWeapon)
        {
            Debug.Log("Você precisa pegar uma arma primeiro.");
            return;
        }

        if (isReloading)
        {
            Debug.Log("Recarregando...");
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("Sem munição! Aperte R para recarregar.");
            return;
        }

        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Bullet Prefab ou FirePoint não configurado.");
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("Main Camera não encontrada.");
            return;
        }

        Vector2 screenPoint;

        if (crosshairRect != null)
        {
            screenPoint = RectTransformUtility.WorldToScreenPoint(null, crosshairRect.position);
        }
        else
        {
            screenPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        }

        Ray ray = cam.ScreenPointToRay(screenPoint);

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, targetLayer))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * maxShootDistance;
        }

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDirection)
        );

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb != null)
        {
            bulletRb.linearVelocity = shootDirection * bulletSpeed;
        }

        currentAmmo--;
        nextFireTime = Time.time + fireRate;

        UpdateUI();
    }

    public void StartReload()
    {
        if (!hasWeapon)
        {
            Debug.Log("Pegue uma arma primeiro.");
            return;
        }

        if (isReloading)
        {
            return;
        }

        if (currentAmmo >= maxAmmo)
        {
            Debug.Log("Munição já está cheia.");
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        UpdateUI();

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;

        UpdateUI();
    }

    void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            EndGame();
        }

        UpdateUI();
    }

    void EndGame()
    {
        gameEnded = true;

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "GAME OVER";
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (ammoText != null)
        {
            if (isReloading)
            {
                ammoText.text = "Ammo: Recarregando...\nTime: " + Mathf.CeilToInt(currentTime) + "s";
            }
            else
            {
                ammoText.text = "Ammo: " + currentAmmo + "/" + maxAmmo + "\nTime: " + Mathf.CeilToInt(currentTime) + "s";
            }
        }
    }
}