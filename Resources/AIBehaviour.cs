using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

namespace ChobiAssets.KTP 
{
    public class AIBehaviour : MonoBehaviour
    {
        public UnityEngine.AI.NavMeshAgent agent { get; private set; }             // the navmesh agent required for the path finding
        public Transform tank { get; private set; } // the character we are controlling
        public Transform target;                                    // target to aim for
        public Aiming_Control_CS aim;
        public List<Vector3> patrol_route = new List<Vector3>(4);
        private bool on_chase;
        private int pos_index = 0;

        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            tank = this.transform.parent;
            aim = GetComponent<Aiming_Control_CS>();
            agent.updateRotation = true;
            agent.updatePosition = true;
            on_chase = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.tag == "Player")
            {
                agent.stoppingDistance = 15;
                //Debug.Log("chasing... ");
                target = other.transform;
                on_chase = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.tag == "Player")
            {
                if (on_chase)
                    on_chase = false;
                //Debug.Log("losing target...");
                target = null;
                on_chase = false;
                agent.stoppingDistance = 0;
            }
        }

        private void Update()
        {
            if (tank.tag == "AI")
            {
                if (isInSight())
                {
                    Debug.Log("in range");
                    Transform turret = this.transform.Find("Turret_Objects");
                    aim.targetPosition = target.position;
                    aim.targetTransform = target;
                }
                if (on_chase)
                {
                    if (target != null)
                    {
                        agent.SetDestination(target.position);
                    }
                }
                else
                {
                    if (agent.remainingDistance == agent.stoppingDistance)
                    {
                        pos_index = (pos_index + 1) % 4;
                        agent.SetDestination(patrol_route[pos_index]);
                    }

                }
            }
            else
            {
                agent.isStopped = true;
            }
        }

        public bool isInSight()
        {
            if (tank.tag == "AI" && target != null)
            {
                Vector3 perspect = tank.position + new Vector3(0, 2, 0);
                Ray ray = new Ray(perspect, target.position - perspect);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    //Debug.Log(hit.collider.name);
                    if (hit.collider.tag == "Player")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void SetRoute(List<Vector3> route)
        {
            for (int i = 0; i < 4; i++)
            {
                patrol_route[i] = route[i];
            }
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }
    }
}
