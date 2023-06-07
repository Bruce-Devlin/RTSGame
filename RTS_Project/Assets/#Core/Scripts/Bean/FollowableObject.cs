using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowableObject : MonoBehaviour
{
    public bool moving = false;
    
    public float clickDelta = 0.35f;  // Max between two click to be considered a double click
 
    private bool click = false;
    private float clickTime;
 
    void OnMouseDown() {
        if (click && Time.time <= (clickTime + clickDelta)) 
        {
            Bean bean = transform.parent.GetComponent<Bean>();
            if (bean.playerRig.allUnits.Contains(bean))
            {
                bean.playerRig.followObject = this;
            }
            
            click = false;
        }
        else 
        {
            click = true;
            clickTime = Time.time;
        }
    }
}
