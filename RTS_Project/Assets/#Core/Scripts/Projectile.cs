using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    float spawnTimer;
    float aliveTime = 10;

    public float speed;
    public GameObject owner;

    public float damage;
    public Vector3 targetPosition = Vector3.zero;
    private Vector3 lastPos;
    private void Update() 
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Collided(collision);
    }

    public virtual void OnCollisionStay(Collision collision)    
    { 
        Collided(collision);
    }

    void Collided(Collision collision)
    {
        if (collision.gameObject.tag == "Bean" && collision.gameObject != owner)
        {
            float damage = Random.Range(5,15);
            collision.transform.GetComponentInParent<Bean>().beanHealth -= damage;
        }
        Destroy(gameObject);
    }
}
