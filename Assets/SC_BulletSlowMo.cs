using UnityEngine;

public class SC_BulletSlowMo : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float speed;

    private Animator anim;

    private void Start()
    {
        player = GameObject.Find("TPS");
        anim = player.GetComponent<Animator>();
         
    }
}