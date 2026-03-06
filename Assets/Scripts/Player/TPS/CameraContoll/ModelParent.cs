using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModelParent : MonoBehaviour
{
    public List<GameObject> childModel;

    private void Start()
    {
        FindModel();
    }

    private void FindModel()
    {
        childModel.AddRange(GameObject.FindGameObjectsWithTag("ModelParent"));
    }
}
