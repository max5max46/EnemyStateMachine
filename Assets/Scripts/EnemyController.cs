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
    [Header("Important: Must Fill In")]
    public PlayerController playerScript;
    [Header("DEBUG")]
    public bool AreEnemyPatrolPointsVisable = false;
    public bool isVisionConeVisable = false;
    public bool isSoundDetectionRangeVisable = false;
    public bool enemyStateColors = false;

    //private enums and the variables that use them
    enum State {patrolling, chasing, searching, retreating, attacking}
    enum SearchingLookDirection {right, left, forward}

    State enemyState;
    SearchingLookDirection lookingAround;

    //all outside objects (Besides Player)
    GameObject enemyObject;
    GameObject soundDetectionSphere;
    GameObject sightCone;
    NavMeshAgent agent;

    //patrol point Variables
    int numberOfPatrolPoints;
    int currentPatrolPoint;
    Vector3 currentPatrolPosition;
        //used for back and forth patrol type
        bool goingForward;

    //enemy sightcone Variables
    Quaternion enemyLookDirection;
    float enemyLookConstant;
    float enemyLookSpeed;

    //extra Variables
    Vector3 lastKnownPlayerPosition;
    float timer;

    // Start is called before the first frame update
    void Start()
    {
        //initialize all outside objects
        enemyObject = transform.GetChild(1).gameObject;
        soundDetectionSphere = enemyObject.transform.GetChild(0).gameObject;
        sightCone = transform.GetChild(2).gameObject;
        agent = enemyObject.GetComponent<NavMeshAgent>();

        //initialize enemy state
        enemyState = State.patrolling;

        //initialize Patrolling State Variables
        currentPatrolPoint = 0;
        numberOfPatrolPoints = transform.GetChild(0).transform.childCount;
        agent.SetDestination(transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position);
        goingForward = true;

        //extra initializations
        enemyLookSpeed = 10;
        
        //DEBUG: Call the debug method (true = on start)
        DebugElements(true);
    }

    // Update is called once per frame
    void Update()
    {
        //DEBUG: updates debug elements
        DebugElements();

        //updates sightCone to match enemy's position (with some added offset)
        sightCone.transform.position = enemyObject.transform.position + new Vector3 (0, 0.3f, 0);

        //The State Machine itself
        switch (enemyState)
        {
            case State.patrolling:


                currentPatrolPosition = transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position;

                //if the Enemy hears a noise they'll look at it
                if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) < 12 * playerScript.GetNoise())
                    enemyLookDirection = Quaternion.LookRotation(playerScript.GetPosition() - sightCone.transform.position);
                else
                    enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.rotation.eulerAngles.y, 0);

                //Checks to see if the player has been spotted
                if (CanSeePlayer())
                {
                    enemyState = State.chasing;
                    timer = 3;
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

                //ticks down timer
                timer -= Time.deltaTime;

                //sight cone looks towards player
                enemyLookDirection = Quaternion.LookRotation(playerScript.GetPosition() - sightCone.transform.position);

                //if close enough to player switch to attacking
                if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) < 6)
                    enemyState = State.attacking;

                //if player is still in sight keep chasing (reset timer)
                if (CanSeePlayer())
                {
                    timer = 3;
                    agent.SetDestination(playerScript.GetPosition());
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

                //makes the enemy stand still
                agent.SetDestination(enemyObject.transform.position);

                //sight cone looks towards player
                enemyLookDirection = Quaternion.LookRotation(playerScript.GetPosition() - sightCone.transform.position);

                //if player moves to far away switch to chasing
                if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) > 12)
                    enemyState = State.chasing;

                //if player cuts off line of line of sight switch to chasing
                if (!CanSeePlayer())
                    enemyState = State.chasing;

                break;

            case State.searching:


                //Enemy moves to last position the player was at before the end of the chase then looks around
                if (new Vector3(enemyObject.transform.position.x, 0, enemyObject.transform.position.z) == new Vector3(lastKnownPlayerPosition.x, 0, lastKnownPlayerPosition.z))
                {
                    //ticks down timer
                    timer -= Time.deltaTime;

                    //enemy switchs looking states to look left and right
                    switch (lookingAround)
                    {
                        case SearchingLookDirection.right:

                            //turn one direction
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

                            //turn the other direction
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

                            //look forward
                            enemyLookDirection = Quaternion.Euler(0, enemyLookConstant, 0);
                            enemyLookSpeed = 3;

                            //if looking finishes, switch back to patrol mode
                            if (timer < 0)
                                enemyState = State.retreating;

                            break;
                    }
                }
                else
                {
                    //makes sure enemy is looking forward
                    enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0);
                }

                //if enemy hears player but can't see them move to the location they heard the player at and start searching 
                if (CanHearPlayer())
                {
                    lastKnownPlayerPosition = playerScript.GetPosition();
                    agent.SetDestination(lastKnownPlayerPosition);
                    enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0);
                    lookingAround = SearchingLookDirection.right;
                    timer = 2;
                }

                //check to see in player is in sight, if so switch to chasing
                if (CanSeePlayer())
                {
                    timer = 3;
                    enemyState = State.chasing;
                }

                break;

            case State.retreating:

                //gets the transform of the current patrol point
                currentPatrolPosition = transform.GetChild(0).transform.GetChild(currentPatrolPoint).transform.position;

                //look towards first patrol point
                enemyLookDirection = Quaternion.Euler(0, enemyObject.transform.localRotation.eulerAngles.y, 0);

                //set current patrol point to first
                currentPatrolPoint = 0;

                //go to first patrol point
                agent.SetDestination(currentPatrolPosition);

                //if the enemy reachs their retreat point (first patrol point) then switch to patrolling
                if (new Vector3(enemyObject.transform.position.x, 0, enemyObject.transform.position.z) == new Vector3(currentPatrolPosition.x, 0, currentPatrolPosition.z))
                    enemyState = State.patrolling;

                //if the enemy hears the player they look towards and move towards the player's location for as long as the player is heard
                if (CanHearPlayer())
                {
                    agent.SetDestination(enemyObject.transform.position);
                    enemyLookDirection = Quaternion.LookRotation(playerScript.GetPosition() - sightCone.transform.position);
                }

                //if the enemy spots the player switch to chasing
                if (CanSeePlayer())
                {
                    timer = 3;
                    enemyState = State.chasing;
                }

                break;

        }

        RotateVisionCone();
        enemyLookSpeed = 10;
    }


    public void RotateVisionCone()
    {
        //rotates the sight cone every update towards a specified direction
        sightCone.transform.rotation = Quaternion.Slerp(sightCone.transform.rotation, enemyLookDirection, Time.deltaTime * enemyLookSpeed);
    }

    private bool CanSeePlayer()
    {
        //if the player is in the enemy sight cone and a raycast can reach the player from the enemy unimpeded, then return true
        if (sightCone.GetComponent<EnemySight>().playerSpotted)
        {
            RaycastHit hit;
            if (Physics.Raycast(sightCone.transform.position, playerScript.GetPosition() - sightCone.transform.position, out hit, 30))
            {
                if (hit.collider.gameObject.CompareTag("Player"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CanHearPlayer()
    {
        //check a distance away from the enemy based off the noise made by the player and if the distance is within our specified range, then return true
        if (Vector3.Distance(enemyObject.transform.position, playerScript.GetPosition()) < 12 * playerScript.GetNoise())
            return true;

        return false;
    }

    private void DebugElements(bool isOnStart = false)
    {
        if (isOnStart)
        {
            //DEBUG: if patrol point visuals are off, make them invisable
            if (!AreEnemyPatrolPointsVisable)
                for (int i = 0; i < numberOfPatrolPoints; i++)
                    transform.GetChild(0).transform.GetChild(i).transform.gameObject.SetActive(false);

            //DEBUG: if the Sound Detection Sphere visuals are off, make it invisable
            if (!isSoundDetectionRangeVisable)
                soundDetectionSphere.SetActive(false);

            //DEBUG: if the Sight Cone visuals are off, make it invisable
            if (!isVisionConeVisable)
                sightCone.GetComponent<MeshRenderer>().enabled = false;
        }

        //DEBUG: scales the Sound Detection Sphere
        if (isSoundDetectionRangeVisable)
            soundDetectionSphere.transform.localScale = new Vector3(1, 1, 1) * 12 * 2 * playerScript.GetNoise();

        //DEBUG: color switching to help discern the state of the enemy
        if (enemyStateColors)
        {
            switch (enemyState)
            {
                case State.patrolling:
                    enemyObject.GetComponent<MeshRenderer>().material.color = Color.green;
                    break;

                case State.chasing:
                    enemyObject.GetComponent<MeshRenderer>().material.color = Color.red; 
                    break;

                case State.attacking:
                    enemyObject.GetComponent<MeshRenderer>().material.color = Color.magenta;
                    break;

                case State.searching:
                    enemyObject.GetComponent<MeshRenderer>().material.color = Color.yellow;
                    break;

                case State.retreating:
                    enemyObject.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    break;
            }
        }
    }
}
