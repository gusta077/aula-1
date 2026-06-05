using UnityEngine;
using TMPro;

public class FPSAimController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;

    [Header("Mouse / Câmera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 120f;
    public float minLookX = -80f;
    public float maxLookX = 80f;

    private float cameraPitch = 0f;

    [Header("Arma")]
    public Transform weaponHolder;
    public GameObject currentWeapon;
    public bool hasWeapon = false;

    [Header("Tiro")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 30f;
    public int maxAmmo = 30;
    public int currentAmmo = 30;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text ammoText;
    public TMP_Text gameOverText;

    private int score = 0;

    void Start()
    {
        currentAmmo = maxAmmo;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateUI();

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        LookWithMouse();
        MovePlayer();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void LookWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Gira o corpo do Player para os lados
        transform.Rotate(Vector3.up * mouseX);

        // Gira a câmera para cima e para baixo
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookX, maxLookX);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    void MovePlayer()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A / D
        float vertical = Input.GetAxisRaw("Vertical");     // W / S

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection.y = 0f;
        moveDirection.Normalize();

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    public void EquipWeapon(GameObject weapon, Transform weaponFirePoint)
    {
        if (weaponHolder == null)
        {
            Debug.LogWarning("WeaponHolder não foi configurado no Player.");
            return;
        }

        currentWeapon = weapon;
        hasWeapon = true;

        weapon.transform.SetParent(weaponHolder);

        // Ajuste visual da arma na mão
        weapon.transform.localPosition = new Vector3(0f, 0f, 0f);
        weapon.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        weapon.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        firePoint = weaponFirePoint;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = weapon.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        Debug.Log("Arma equipada!");
    }

    void Shoot()
    {
        if (!hasWeapon)
        {
            Debug.Log("Você precisa pegar uma arma primeiro.");
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("Sem munição!");
            return;
        }

        if (bulletPrefab == null)
        {
            Debug.LogWarning("Bullet Prefab não foi configurado no Player.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("FirePoint não foi configurado.");
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("Main Camera não encontrada. Verifique se ela está com a tag MainCamera.");
            return;
        }

        // Raio saindo do centro da tela, onde está a mira
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 100f;
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
        UpdateUI();
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (ammoText != null)
        {
            ammoText.text = "Ammo: " + currentAmmo + "/" + maxAmmo + "\nTime: 60s";
        }
    }
}