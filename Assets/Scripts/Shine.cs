using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Shine : MonoBehaviour
{

    public Transform shineMask;
    public float speed;
    public float minDelay;
    public float maxDelay;
    public float offset;
    void Start()
    {
        Animate();
    }

    void Animate()
    {
       shineMask.DOLocalMoveX(offset, speed).SetDelay(UnityEngine.Random.Range(minDelay, maxDelay)).SetEase(Ease.Linear).OnComplete(() => {
           shineMask.DOLocalMoveX(-offset, 0);
           Animate();
       }) ;
    }
}
