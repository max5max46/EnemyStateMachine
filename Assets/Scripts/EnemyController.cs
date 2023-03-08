using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    enum State {patrolling, chasing, searching}
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

    float timer;
    int lookingAround;
    float enemyLookConstant;

    int health;

    Vector3 lastKnownPlayerPosition;

    // Start is called before the first frame update
    void Start()
    {
        enemyObject = transform.GetChild(1).gameObject;
        agent = enemyObject.GetComponent<NavMeshAgent>();
        enemyState = State.patrolling;
        currentPatrolPoint = 0;
        health = 5;

        goingForward = true;

        numberOfPatrolPoints = transform.GetChild(0).transform.childCount;

        agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);

        for (int i = 0; i < numberOfPatrolPoints; i++)
        {
            transform.GetChild(0).transform.GetChild(i).transform.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {

        detectionSphere.transform.localScale = new Vector3 (1, 1, 1) * 12 * 2 * player.GetComponent<PlayerController>().noise;

        currentPatrolPosition = transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position;
        
        //The State Machine itself
        switch (enemyState)
        {
            case State.patrolling:

                //sets enemy color to help show what state its in
                enemyObject.GetComponent<MeshRenderer>().material.color = Color.green;

                //if the Enemy hears a noise they'll look at it
                if (Vector3.Distance(enemyObject.transform.position, player.transform.position) < 12 * player.GetComponent<PlayerController>().noise)
                    sightCone.transform.rotation = Quaternion.Slerp(sightCone.transform.rotation, Quaternion.LookRotation(player.transform.position - sightCone.transform.position), Time.deltaTime * 10);
                else
                    sightCone.transform.rotation = Quaternion.Slerp(sightCone.transform.rotation, Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0), Time.deltaTime * 10);

                //Checks to see if the player has been spotted
                if (sightCone.GetComponent<EnemySight>().playerSpotted)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(sightCone.transform.position, player.transform.position - sightCone.transform.position, out hit, 20))
                        if (hit.collider.gameObject.CompareTag("Player"))
                        {
                            enemyState = State.chasing;
                            timer = 3;
                        }
                }


                //checks to see if the Enemy has reached there current patrol point and if the enemy has, will move the current patrol point to the next in sequence
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

                //sets enemy color to help show what state its in
                enemyObject.GetComponent<MeshRenderer>().material.color = Color.red;

                timer -= Time.deltaTime;

                //sight cone looks towards player
                sightCone.transform.rotation = Quaternion.Slerp(sightCone.transform.rotation, Quaternion.LookRotation(player.transform.position - sightCone.transform.position), Time.deltaTime * 10);

                //if player is still in sight keep chasing (reset timer)
                if (sightCone.GetComponent<EnemySight>().playerSpotted)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(sightCone.transform.position, player.transform.position - sightCone.transform.position, out hit, 20))
                        if (hit.collider.gameObject.CompareTag("Player"))
                        {
                            timer = 3;
                            agent.SetDestination(player.transform.position);
                        }
                }

                //if the player spends 3 seconds out of enemy sight switch state
                if (timer < 0)
                {
                    enemyState = State.searching;

                    //setup Variables for next state
                    lookingAround = 1;
                    timer = 2;
                    enemyLookConstant = enemyObject.transform.rotation.eulerAngles.y;
                    lastKnownPlayerPosition = player.transform.position;

                    agent.SetDestination(lastKnownPlayerPosition);
                }

                break;

            case State.searching:

                //sets enemy color to help show what state its in
                enemyObject.GetComponent<MeshRenderer>().material.color = Color.yellow;

                //Enemy moves to last position the player was at before the end of the chase then looks around
                if (new Vector3(enemyObject.transform.position.x, 0, enemyObject.transform.position.z) == new Vector3(lastKnownPlayerPosition.x, 0, lastKnownPlayerPosition.z))
                {
                    timer -= Time.deltaTime;

                    //enemy switchs looking states to look left and right
                    switch (lookingAround)
                    {
                        case 1:
                            // turn one direction
                            enemyObject.transform.rotation = Quaternion.Slerp(enemyObject.transform.rotation, Quaternion.Euler(enemyObject.transform.rotation.eulerAngles.x, enemyLookConstant - 60, enemyObject.transform.rotation.eulerAngles.z), Time.deltaTime * 3);
                            if (timer < 0)
                            {
                                //next looking state
                                lookingAround++;
                                timer = 2;
                            }
                            break;

                        case 2:
                            // turn the other direction
                            enemyObject.transform.rotation = Quaternion.Slerp(enemyObject.transform.rotation, Quaternion.Euler(enemyObject.transform.rotation.eulerAngles.x, enemyLookConstant + 60, enemyObject.transform.rotation.eulerAngles.z), Time.deltaTime * 3);
                            if (timer < 0)
                            {
                                //next looking state
                                lookingAround++;
                                timer = 2;
                            }
                            break;

                        case 3:
                            // look forward
                            enemyObject.transform.rotation = Quaternion.Slerp(enemyObject.transform.rotation, Quaternion.Euler(enemyObject.transform.rotation.eulerAngles.x, enemyLookConstant, enemyObject.transform.rotation.eulerAngles.z), Time.deltaTime * 3);
                            //if looking finishes, swap back to patrol mode
                            if (timer < 0)
                            {
                                enemyState = State.patrolling;
                                currentPatrolPoint = 0;
                                agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);
                            }
                            break;
                    }
                }
                else
                {
                    // makes sure enemy is looking forward
                    sightCone.transform.rotation = Quaternion.Slerp(sightCone.transform.rotation, Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0), Time.deltaTime * 10);
                }

                //check to see in player is in sight
                if (sightCone.GetComponent<EnemySight>().playerSpotted)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(sightCone.transform.position, player.transform.position - sightCone.transform.position, out hit, 20))
                        if (hit.collider.gameObject.CompareTag("Player"))
                        {
                            timer = 3;
                            enemyState = State.chasing;
                        }
                }

                break;
        }
    }

    public void TakeDamage()
    {
        health--;
        enemyObject.GetComponent<MeshRenderer>().material.color = Color.white;
        Debug.Log(health);
    }
}
