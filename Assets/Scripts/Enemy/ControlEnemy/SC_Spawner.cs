using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_Spawner : MonoBehaviour
{
    [System.Serializable]
    public class WaveStatistics
    {
        [Header("Имя волны")]
        [SerializeField] private string name;

        [Header("Противники")]
        [SerializeField] private List<GameObject> enemyPref;

        [Header("Количество каждого типа")]
        [SerializeField] private int enemyCount;

        public string Name => name;
        public int EnemyCount => enemyCount;
        public List<GameObject> EnemyPref => enemyPref;
    }

    [Header("Все волны")]
    [SerializeField] private List<WaveStatistics> WaveData;

    [Header("Точки спавна")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Состояние")]
    [SerializeField] private List<GameObject> enemies = new List<GameObject>();

    private int waveIndex = 0;

    private void Start()
    {
        StartCoroutine(SpawnWave());
    }

    private void Update()
    {
        // если все враги убиты, запускаем следующую волну
        if (enemies.Count == 0 && waveIndex < WaveData.Count)
        {
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        if (waveIndex >= WaveData.Count)
            yield break; // все волны пройдены

        WaveStatistics currentWave = WaveData[waveIndex];

        // список точек для этой волны
        List<Transform> chosenPoints = new List<Transform>();

        // создаем список точек для каждого врага
        int pointsCount = spawnPoints.Length;
        if (pointsCount == 0)
        {
            Debug.LogError("Нет spawnPoints для спавна врагов!");
            yield break;
        }

        for (int i = 0; i < currentWave.EnemyCount; i++)
        {
            Transform point = spawnPoints[Random.Range(0, pointsCount)];
            chosenPoints.Add(point);
        }

        // спавним врагов
        for (int i = 0; i < currentWave.EnemyCount; i++)
        {
            if (currentWave.EnemyPref.Count == 0)
            {
                Debug.LogWarning($"Нет префабов врагов для волны {currentWave.Name}");
                continue;
            }

            int enemyPrefabIndex = Random.Range(0, currentWave.EnemyPref.Count);

            // защита от выхода за границы chosenPoints
            Transform spawnPoint = chosenPoints[Mathf.Min(i, chosenPoints.Count - 1)];

            GameObject enemy = Instantiate(currentWave.EnemyPref[enemyPrefabIndex], spawnPoint.position, Quaternion.identity);
            enemies.Add(enemy);
        }

        waveIndex++; // переходим к следующей волне
        yield return null;
    }
}