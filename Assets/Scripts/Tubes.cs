using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Tubes : MonoBehaviour
{
    public float speed;
    public float amount;

    void Start()
    {
        Animate();
    }

    void Animate()
    {
        transform.DOMoveY(amount, speed).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
    }

}
