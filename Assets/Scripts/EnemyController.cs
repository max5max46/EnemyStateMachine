using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    enum State {patroling, chasing}
    public enum PatrolType {loop, backAndForth, stationary}
    
    public PatrolType enemyPatrolType;
    public GameObject player;

    GameObject enemyObject;
    NavMeshAgent agent;

    int numberOfPatrolPoints;
    int currentPatrolPoint;
    Vector3 currentPatrolPosition;

    State enemyState;

    bool goingForward;

    public GameObject detectionSphere;
    public GameObject sightCone;

    // Start is called before the first frame update
    void Start()
    {
        enemyObject = transform.GetChild(1).gameObject;
        agent = enemyObject.GetComponent<NavMeshAgent>();
        enemyState = State.patroling;
        currentPatrolPoint = 0;

        goingForward = true;

        numberOfPatrolPoints = transform.GetChild(0).transform.childCount;

        agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        detectionSphere.transform.localScale = new Vector3 (1, 1, 1) * 15 * 2 * player.GetComponent<PlayerController>().noise;

        currentPatrolPosition = transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position;
        
        switch (enemyState)
        {
            case State.patroling:

                enemyObject.GetComponent<MeshRenderer>().material.color = Color.green;

                sightCone.transform.localRotation = Quaternion.Euler(90, 0, 0);

                if (Vector3.Distance(enemyObject.transform.position, player.transform.position) < 15 * player.GetComponent<PlayerController>().noise)
                {
                    sightCone.transform.LookAt(player.transform.position);
                }
                
                if (new Vector3 (enemyObject.transform.position.x, 0, enemyObject.transform.position.z) == new Vector3(currentPatrolPosition.x, 0, currentPatrolPosition.z))
                {
                    //Patrol AI type (Behaviour)
                    switch (enemyPatrolType)
                    {
                        //(1, 2, 3, 4, 1, 2, 3, 4, 1, 2 ...)
                        case PatrolType.loop:

                            currentPatrolPoint++;

                            if (currentPatrolPoint == numberOfPatrolPoints)
                                currentPatrolPoint = 0;

                            agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);

                            break;

                        //(1, 2, 3, 4, 3, 2, 1, 2, 3, 4 ...)
                        case PatrolType.backAndForth:
                            
                            if (goingForward)
                                currentPatrolPoint++;
                            else
                                currentPatrolPoint--;

                            if (currentPatrolPoint == numberOfPatrolPoints)
                            {
                                currentPatrolPoint--;
                                goingForward = false;
                            }

                            if (currentPatrolPoint == -1)
                            {
                                currentPatrolPoint++;
                                goingForward = true;
                            }

                            agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);

                            break;

                        //(1)
                        case PatrolType.stationary:

                            break;
                    }
                }
                break;

            case State.chasing:

                enemyObject.GetComponent<MeshRenderer>().material.color = Color.red;
                agent.SetDestination(player.transform.position);

                break;
        }
    }
}
