using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    private Vector3 offset;
    private bool gameOver;

    void Start()
    {
        offset = target.position - transform.position;
    }

    void Update()
    {
        if(!gameOver)
            transform.position = target.position - offset;
    }

    public void FocusOnWinner(Transform winner)
    {
        gameOver = true;
        target = winner;
        transform.DOMove(target.position - offset, .5f);
    }
}
