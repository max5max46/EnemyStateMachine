using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    //all public set Variables
    public enum PatrolType {loop, backAndForth, stationary}
    [Header("Enemy Setting")]
    public PatrolType enemyPatrolType = PatrolType.loop;
    [Header("Enemy Debug")]
    public bool AreEnemyPatrolPointsVisable = false;
    public bool isVisionConeVisable = false;
    public bool isSoundDetectionRangeVisable = false;
    [Header("Important: Must Fill In")]
    public PlayerController playerScript;
    


    enum State {patrolling, chasing, searching, retreating, attacking}
    enum SearchingLookDirection {right, left, forward}

    GameObject enemyObject;
    NavMeshAgent agent;

    //patrol point Variables
    int numberOfPatrolPoints;
    int currentPatrolPoint;
    Vector3 currentPatrolPosition;
        //used for back and forth patrol type
        bool goingForward;

    State enemyState;

    //enemy sightcone Variables
    Quaternion enemyLookDirection;
    float enemyLookConstant;
    float enemyLookSpeed;



    GameObject soundDetectionSphere;
    GameObject sightCone;

    float timer;
    SearchingLookDirection lookingAround;

    int health;

    Vector3 lastKnownPlayerPosition;

    // Start is called before the first frame update
    void Start()
    {
        enemyObject = transform.GetChild(1).gameObject;
        soundDetectionSphere = enemyObject.transform.GetChild(0).gameObject;
        sightCone = transform.GetChild(2).gameObject;


        agent = enemyObject.GetComponent<NavMeshAgent>();
        health = 5;

        enemyLookSpeed = 10;
        enemyState = State.patrolling;
        currentPatrolPoint = 0;
        goingForward = true;
        numberOfPatrolPoints = transform.GetChild(0).transform.childCount;
        agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);

        if (!AreEnemyPatrolPointsVisable)
            for (int i = 0; i < numberOfPatrolPoints; i++)
                transform.GetChild(0).transform.GetChild(i).transform.gameObject.SetActive(false);

        if (!isSoundDetectionRangeVisable)
            soundDetectionSphere.SetActive(false);

        if (!isVisionConeVisable)
            sightCone.GetComponent<MeshRenderer>().enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        sightCone.transform.position = enemyObject.transform.position + new Vector3 (0, 0.3f, 0);

        if (isSoundDetectionRangeVisable)
            soundDetectionSphere.transform.localScale = new Vector3 (1, 1, 1) * 12 * 2 * playerScript.GetNoise();

        currentPatrolPosition = transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position;

        

        //The State Machine itself
        switch (enemyState)
        {
            case State.patrolling:

                //sets enemy color to help show what state its in
                enemyObject.GetComponent<MeshRenderer>().material.color = Color.green;

                //if the Enemy hears a noise they'll look at it
                if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) < 12 * playerScript.GetNoise())
                    enemyLookDirection = Quaternion.LookRotation(playerScript.GetPosition() - sightCone.transform.position);
                else
                    enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.rotation.eulerAngles.y, 0);

                //Checks to see if the player has been spotted
                if (sightCone.GetComponent<EnemySight>().playerSpotted)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(sightCone.transform.position, playerScript.GetPosition() - sightCone.transform.position, out hit, 20))
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
                enemyLookDirection = Quaternion.LookRotation(playerScript.GetPosition() - sightCone.transform.position);

                if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) < 6)
                {
                    enemyState = State.attacking;
                }

                //if player is still in sight keep chasing (reset timer)
                if (sightCone.GetComponent<EnemySight>().playerSpotted)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(sightCone.transform.position, playerScript.GetPosition() - sightCone.transform.position, out hit, 20))
                        if (hit.collider.gameObject.CompareTag("Player"))
                        {
                            timer = 3;
                            agent.SetDestination(playerScript.GetPosition());
                        }
                }

                //if the player spends 3 seconds out of enemy sight switch state
                if (timer < 0)
                {
                    enemyState = State.searching;

                    //setup Variables for next state
                    lookingAround = SearchingLookDirection.right;
                    timer = 2;
                    enemyLookConstant = enemyObject.transform.rotation.eulerAngles.y;
                    lastKnownPlayerPosition = playerScript.GetPosition();

                    agent.SetDestination(lastKnownPlayerPosition);
                }

                break;

            case State.attacking:

                enemyObject.GetComponent<MeshRenderer>().material.color = Color.black;
                agent.SetDestination(enemyObject.transform.position);
                enemyLookDirection = Quaternion.LookRotation(playerScript.GetPosition() - sightCone.transform.position);

                if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) > 12)
                    enemyState = State.chasing;

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
                        case SearchingLookDirection.right:
                            // turn one direction
                            enemyLookDirection = Quaternion.Euler(0, enemyLookConstant - 60, 0);
                            enemyLookSpeed = 3;
                            if (timer < 0)
                            {
                                //next looking state
                                lookingAround = SearchingLookDirection.left;
                                timer = 2;
                            }
                            break;

                        case SearchingLookDirection.left:
                            // turn the other direction
                            enemyLookDirection = Quaternion.Euler(0, enemyLookConstant + 60, 0);
                            enemyLookSpeed = 3;
                            if (timer < 0)
                            {
                                //next looking state
                                lookingAround = SearchingLookDirection.forward;
                                timer = 2;
                            }
                            break;

                        case SearchingLookDirection.forward:
                            // look forward
                            enemyLookDirection = Quaternion.Euler(0, enemyLookConstant, 0);
                            enemyLookSpeed = 3;
                            //if looking finishes, swap back to patrol mode
                            if (timer < 0)
                            {
                                enemyState = State.retreating;
                            }
                            break;
                    }
                }
                else
                {
                    // makes sure enemy is looking forward
                    enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0);
                }

                if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) < 12 * playerScript.GetNoise())
                {
                    lastKnownPlayerPosition = playerScript.GetPosition();
                    agent.SetDestination(lastKnownPlayerPosition);
                    enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0);
                    lookingAround = SearchingLookDirection.right;
                    timer = 2;
                }

                //check to see in player is in sight
                if (sightCone.GetComponent<EnemySight>().playerSpotted)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(sightCone.transform.position, playerScript.GetPosition() - sightCone.transform.position, out hit, 20))
                        if (hit.collider.gameObject.CompareTag("Player"))
                        {
                            timer = 3;
                            enemyState = State.chasing;
                        }
                }

                break;

            case State.retreating:

                enemyObject.GetComponent<MeshRenderer>().material.color = Color.cyan;

                enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0);
                currentPatrolPoint = 0;
                agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);

                if (new Vector3(enemyObject.transform.position.x, 0, enemyObject.transform.position.z) == new Vector3(currentPatrolPosition.x, 0, currentPatrolPosition.z))
                    enemyState = State.patrolling;

                if (sightCone.GetComponent<EnemySight>().playerSpotted)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(sightCone.transform.position, playerScript.GetPosition() - sightCone.transform.position, out hit, 20))
                        if (hit.collider.gameObject.CompareTag("Player"))
                        {
                            timer = 3;
                            enemyState = State.chasing;
                        }
                }

                break;

        }

        RotateVisionCone();
        enemyLookSpeed = 10;
    }


    public void RotateVisionCone()
    {
        sightCone.transform.rotation = Quaternion.Slerp(sightCone.transform.rotation, enemyLookDirection, Time.deltaTime * enemyLookSpeed);
    }


    public void TakeDamage()
    {
        health--;
        enemyObject.GetComponent<MeshRenderer>().material.color = Color.white;
        Debug.Log(health);
    }
}
