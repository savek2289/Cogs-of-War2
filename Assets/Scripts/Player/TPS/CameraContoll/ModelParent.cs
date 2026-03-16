using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModelParent : MonoBehaviour
{
    [System.Serializable]
    public class ModelsStatistics
    {
        [Header("Имя волны")]
        [SerializeField] private string name;

        [Header("МестоМодуля")]
        [SerializeField] private GameObject parentModel;

        [SerializeField] private List<GameObject> modelsChild;

        public string Name => name;
        public GameObject ParentModel => parentModel;
        public List<GameObject> ModelsChild => modelsChild;

         
    }

    [SerializeField] private List<ModelsStatistics> modelsData;

    [SerializeField] private int countModels = 0;
    private void Start()
    {
        foreach (var model in modelsData)
        {
            for (int i = 0; i < model.ModelsChild.Count; i++)
            {
                countModels++;
            }
        }
    }
    public void DamageDistribution(float damage)
    {
        int modelActive = 0;
        for (int i = 0; i < modelsData.Count; i++)
        {
            for (int j = 0; j < modelsData[i].ModelsChild.Count; j++)
            {
                if (modelsData[i].ModelsChild[j].activeSelf == false)
                {
                    modelActive++;
                }
            }
        }
        if (modelActive == countModels)
        {
            SceneManager.LoadScene(0);
            return;
        }
        int rand = Random.Range(0, 14);
        int model = 0;
        int colome = 0;
        switch (rand)
        {
            case 0:
                model = 0;
                colome = 0;
                break;
            case 1:
                model = Random.Range(0, 3);
                colome = 1;
                break;
            case 2:
                model = Random.Range(0, 3);
                colome = 1;
                break;
            case 3:
                model = Random.Range(0, 3);
                colome = 1;
                break;
            case 4:
                model = Random.Range(0, 3);
                colome = 2;
                break;
            case 5:
                model = Random.Range(0, 3);
                colome = 2;
                break;
            case 6:
                model = Random.Range(0, 3);
                colome = 2;
                break;
            case 7:
                model = Random.Range(0, 3);
                colome = 3;
                break;
            case 8:
                model = Random.Range(0, 3);
                colome = 3;
                break;
            case 9:
                model = Random.Range(0, 3);
                colome = 3;
                break;
            case 10:
                model = Random.Range(0, 3);
                colome = 4;
                break;
            case 11:
                model = Random.Range(0, 3);
                colome = 4;
                break;
            case 12:
                model = Random.Range(0, 3);
                colome = 4;
                break;
            case 13:
                model = 0;
                colome = 5;
                break;

        }
        if(modelsData[colome].ModelsChild[model].activeSelf == false)
        {
            DamageDistribution(damage);
            return;
        }
        
        modelsData[colome].ModelsChild[model].GetComponent<SC_ModelHP>().TakeDamage(damage);
 
    }
}
