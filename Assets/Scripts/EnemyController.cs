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
        currentPatrolPosition = transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position;
        
        switch (enemyState)
        {
            case State.patroling:
                
                if (new Vector3 (enemyObject.transform.position.x, 0, enemyObject.transform.position.z) == new Vector3(currentPatrolPosition.x, 0, currentPatrolPosition.z))
                {
                    switch (enemyPatrolType)
                    {
                        case PatrolType.loop:

                            currentPatrolPoint++;

                            if (currentPatrolPoint == numberOfPatrolPoints)
                                currentPatrolPoint = 0;

                            agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);

                            break;


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


                        case PatrolType.stationary:

                            break;
                    }
                }
                break;

            case State.chasing:

                agent.SetDestination(player.transform.position);

                break;
        }
    }
}
