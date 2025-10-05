using UnityEngine;
using System.Collections;

public class PowerUpSpawner : MonoBehaviour {
    public GameObject[] objectsToSpawn;
    public Vector2 spawnAreaSize = new Vector2(5f, 5f);
    public float spawnIntervalMin = 10f;
    public float spawnIntervalMax = 15f;
    public float despawnTime = 5f;

    private void Start() {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine() {
        while (true) {
            float waitTime = Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(waitTime);
            SpawnObject();
        }
    }

    private void SpawnObject() {
        if (objectsToSpawn.Length == 0) return;
        GameObject randomObject = objectsToSpawn[Random.Range(0, objectsToSpawn.Length)];
        Vector2 randomPosition = new Vector2(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
        );
        Vector2 spawnPosition = (Vector2)transform.position + randomPosition;
        GameObject spawnedObject = Instantiate(randomObject, spawnPosition, Quaternion.identity);
        Destroy(spawnedObject, despawnTime);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0));
    }
}
