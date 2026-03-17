using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_ModelHP : MonoBehaviour
{
    [SerializeField] private float hp;

    public void TakeDamage(float damage)
    {
        if (hp -damage <= 0)
        {
            gameObject.SetActive(false);
            return;
        }
        hp -= damage;
    }
}
