using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TargetSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnPoint
    {
        public Transform position;
        public GameObject targetPrefab;
        public int quantity = 1;
        public Vector3 scale = Vector3.one;
        public Vector3 rotation = Vector3.zero;

        public bool moveHorizontal = false;
        public bool moveVertical = false;
        public float moveSpeed = 3f;
        public float moveRange = 5f;

        public int health = 1;
        public int pointsValue = 10;

        [Header("Respawn")]
        public float respawnDelay = 3f;
    }

    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    private List<GameObject> spawnedTargets = new List<GameObject>();
    private List<SpawnPoint> waitingRespawn = new List<SpawnPoint>();

    void Start()
    {
        SpawnAllTargets();
    }

    void Update()
    {
        spawnedTargets.RemoveAll(target => target == null);

        foreach (SpawnPoint point in spawnPoints)
        {
            int currentCount = CountTargetsFromPoint(point);

            if (currentCount < point.quantity && !waitingRespawn.Contains(point))
            {
                StartCoroutine(RespawnAfterDelay(point));
            }
        }
    }

    int CountTargetsFromPoint(SpawnPoint point)
    {
        int count = 0;

        foreach (GameObject target in spawnedTargets)
        {
            if (target != null)
            {
                Target targetScript = target.GetComponent<Target>();

                if (targetScript != null && targetScript.spawnPoint == point)
                {
                    count++;
                }
            }
        }

        return count;
    }

    void SpawnAllTargets()
    {
        foreach (SpawnPoint point in spawnPoints)
        {
            for (int i = 0; i < point.quantity; i++)
            {
                SpawnTarget(point);
            }
        }
    }

    IEnumerator RespawnAfterDelay(SpawnPoint point)
    {
        waitingRespawn.Add(point);

        yield return new WaitForSeconds(point.respawnDelay);

        int currentCount = CountTargetsFromPoint(point);

        if (currentCount < point.quantity)
        {
            SpawnTarget(point);
        }

        waitingRespawn.Remove(point);
    }

    void SpawnTarget(SpawnPoint point)
    {
        if (point.position == null || point.targetPrefab == null)
        {
            return;
        }

        GameObject target = Instantiate(
            point.targetPrefab,
            point.position.position,
            Quaternion.Euler(point.rotation)
        );

        target.transform.localScale = point.scale;

        Target targetScript = target.GetComponent<Target>();

        if (targetScript != null)
        {
            targetScript.spawnPoint = point;
            targetScript.moveHorizontal = point.moveHorizontal;
            targetScript.moveVertical = point.moveVertical;
            targetScript.moveSpeed = point.moveSpeed;
            targetScript.moveRange = point.moveRange;
            targetScript.health = point.health;
            targetScript.pointsValue = point.pointsValue;
        }

        spawnedTargets.Add(target);
    }
}