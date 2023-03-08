using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpTrigger : MonoBehaviour
{
    int triggerObjects = 0;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
            triggerObjects++;

        if(triggerObjects > 0)
            transform.parent.GetComponent<PlayerController>().isGrounded = true; 
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7)
            triggerObjects--;

        if (triggerObjects < 1)
            transform.parent.GetComponent<PlayerController>().isGrounded = false;
    }
}
