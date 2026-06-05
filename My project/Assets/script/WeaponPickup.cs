using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public Transform weaponFirePoint;

    private FPSAimController playerController;
    private bool playerNearby = false;
    private bool pickedUp = false;

    void Start()
    {
        if (weaponFirePoint == null)
        {
            weaponFirePoint = transform.Find("FirePoint");
        }
    }

    void Update()
    {
        if (playerNearby && !pickedUp)
        {
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.F))
            {
                Debug.Log("Tecla de pegar detectada.");

                if (playerController != null)
                {
                    pickedUp = true;
                    playerController.EquipWeapon(gameObject, weaponFirePoint);
                }
                else
                {
                    Debug.LogWarning("PlayerController está vazio.");
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        FPSAimController controller = other.GetComponentInParent<FPSAimController>();

        if (controller != null)
        {
            playerController = controller;
            playerNearby = true;
            Debug.Log("Perto da arma. Aperte E ou F para pegar.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        FPSAimController controller = other.GetComponentInParent<FPSAimController>();

        if (controller != null && !pickedUp)
        {
            playerNearby = false;
            playerController = null;
            Debug.Log("Saiu de perto da arma.");
        }
    }
}