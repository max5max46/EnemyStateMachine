using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            transform.parent.GetComponent<PlayerController>().isGrounded = true; 
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            transform.parent.GetComponent<PlayerController>().isGrounded = false;
    }
}
