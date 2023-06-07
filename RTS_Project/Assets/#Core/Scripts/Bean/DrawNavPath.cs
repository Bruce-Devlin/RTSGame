using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class DrawNavPath : MonoBehaviour 
{
    private NavMeshAgent agent;
    public Color lineColor = Color.white;
    private LineRenderer line; 
    private Bean bean;
    public bool visible = true;
    public void Start() 
    {
        line = this.GetComponent<LineRenderer>();
        agent = gameObject.GetComponent<NavMeshAgent>(); 
        bean = this.GetComponent<Bean>();
    }

    public void Update() 
    {
        if (bean.playerRig != null && visible)
        {
            agent.speed = bean.beanSpeed;
            if (bean.playerRig.selectedUnits.Contains(bean) && bean.moving && !bean.possessed)
            {
                line.startWidth = 0.5f;
                line.endWidth = 0.5f;
            }
            else
            {
                line.startWidth = 0f;
                line.endWidth = 0f;
            } 
        }
        else
        {
            line.startWidth = 0f;
            line.endWidth = 0f;
        }

        OnDrawGizmosSelected();
    }

    void OnDrawGizmosSelected()
    {
        if( agent == null || agent.path == null )
            return;
 
        if( line == null )
        {
            line = this.gameObject.AddComponent<LineRenderer>();
        }

        line.startColor = bean.teamColor;
        line.endColor = bean.teamColor;

        var path = agent.path;

        List<Vector3> newCorners = new List<Vector3>();
        foreach (Vector3 corner in path.corners)
        {
            Vector3 newCorner = corner;
            newCorner.y += 0.5f;
            newCorners.Add(newCorner);
        }
 
        line.positionCount = newCorners.Count;
 
        for( int i = 0; i < newCorners.Count; i++ )
        {
            line.SetPosition( i, newCorners[ i ] );
        }
    }
}