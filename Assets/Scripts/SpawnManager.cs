using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            spawnPoint.gameObject.SetActive(false);
        }
    }

    public Transform GetSpawnPoint() 
    {
        return spawnPoints[Random.Range(0,spawnPoints.Length)];
    }
}
