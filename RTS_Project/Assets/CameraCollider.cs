using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCollider : MonoBehaviour
{
    void OnTriggerEnter(Collider collider) 
    {
        Debug.Log(collider.gameObject.name);
    }
}
