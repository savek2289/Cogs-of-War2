using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_HitPlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Enemy"))
        {
            other.GetComponentInParent<SC_EnemyAI>().TakeDamage(GetComponentInParent<SC_TPSController>().damage);
            return;
        }
    }
}
