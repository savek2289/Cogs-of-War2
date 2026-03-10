using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SC_Spawner : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [SerializeField] private int[] waveCountEnemy;

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private bool isArced = false;
    
    
    [SerializeField] private List<GameObject> enemies;
    [HideInInspector]
    [SerializeField] private List<Transform> randomPoint;

    public int waveCount = 0;

    private void Start()
    {
         
        StartCoroutine(SpawnWave());
    }

    private void Update()
    {
        if (enemies.Count == 0 && !isArced && waveCount < waveCountEnemy.Length)
        {
            randomPoint.Clear();
            enemies.Clear();
            waveCount++;
            StartCoroutine(SpawnWave());
        }

    }
    IEnumerator SpawnWave()
    {
        for (int i = 0; i < waveCountEnemy[waveCount]; i++)
        {
            randomPoint.Add(points[Random.Range(0, points.Length)]);
        }
        for (int i = 0; i < waveCountEnemy[waveCount]; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab, randomPoint[i]);
            enemies.Add(enemy);
        }
        yield return null;
    }
}
