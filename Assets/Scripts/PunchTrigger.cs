using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchTrigger : MonoBehaviour
{
    public PlayerController playerController;

    void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("player"))
        {
            playerController.Punch(other.transform);
        }
    }
}
