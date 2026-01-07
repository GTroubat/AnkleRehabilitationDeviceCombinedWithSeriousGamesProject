using UnityEngine;
using UnityEngine.Rendering;
using System;

public class MarbleSpawn: MonoBehaviour {
    [SerializeField] private GameObject[] marbleArray;
    [SerializeField] private Transform marbleSpawnPoint;

    private float spawnTimer = 0f;
    [SerializeField] private float spawnInterval = 8f;
    private bool isPlaying = false;

    private void Update() {
        spawnTimer += Time.deltaTime;  

        if (spawnTimer >= spawnInterval) {
            Instantiate(marbleArray[UnityEngine.Random.Range(0, 3)], marbleSpawnPoint.position, Quaternion.identity);
            spawnTimer = 0f;
        }
    }

    public void SetBegin(bool value) {
        isPlaying = value;
    }
}
