using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEnemy : MonoBehaviour
{
    public Collider col;
    
    [SerializeField] private bool isRange;
    
    private void Start()
    {
        col = GetComponent<Collider>();

        if (isRange)
            Destroy(gameObject, 2);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isRange)
            if (other.CompareTag("Player"))
                Destroy(gameObject);

        if (!isRange)
            if (other.CompareTag("Player"))
                Debug.Log("Hit");
    }
}
