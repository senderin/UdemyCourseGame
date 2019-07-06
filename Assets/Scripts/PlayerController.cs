using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnControls;
using DG.Tweening;
using TMPro;
using UnityEngine.AI;
using System;

public class PlayerController : MonoBehaviour
{
    public Transform[] bones;
    public bool keepBonesStraight;
    public float speed;
    public float turnSpeed;
    public bool isInZone;
    public int score;
    public Transform scoreFx;
    public bool active;
    public bool isAI;
    public Transform zone;
    public Transform target;
    public float outsideOfZoneRange;
    public ParticleSystem traileFx;
    public bool canPunch;
    public float punchForce;
    public GameObject punchFx;
    public bool stunned;
    public Transform puncher;
    public CameraFollow cameraFollow;
    public bool isBomber;
    public bool unstable;
    public Transform explosionFx;
    public Transform scoreCard;
    public TextMeshPro playerName;
    public GameObject crown;

    private GameManager gameManager;
    private NavMeshAgent agent;
    private Zone zoneScript;
    private float defaultSpeed;
    private Rigidbody rigidbody;
    private Animator animator;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        gameManager = GameObject.FindWithTag("manager").GetComponent<GameManager>();
        animator = GetComponent<Animator>();
        cameraFollow = Camera.main.transform.GetComponent<CameraFollow>();
        InvokeRepeating("AddZoneScore", 1, 1);

        if(isAI)
        {
            agent = GetComponent<NavMeshAgent>();
            zoneScript = zone.GetComponent<Zone>();
        }

        else
        {
            defaultSpeed = speed;
        }
    }

    private void Update()
    {

        if (gameManager.gameStarted && !active)
        {
            active = true;
            animator.SetBool("run", true);

            ParticleSystem.EmissionModule em= traileFx.emission;
            em.enabled = true;

            if (isAI)
                agent.enabled = true;
        }


        if(active)
        {
            if(isAI)
            {   
                if(!stunned)
                {
                    if (gameManager.playersInZone.Contains(transform))
                    {
                        target = GetClosestEnemyInZone(gameManager.playersInZone);
                        if (target == null)
                        {
                            target = zoneScript.wayPoints[UnityEngine.Random.Range(0, zoneScript.wayPoints.Length)];
                        }
                    }
                    else
                    {
                        target = GetClosestEnemyInZone(gameManager.players);
                        if (Vector3.Distance(transform.position, target.position) > outsideOfZoneRange)
                        {
                            target = zone;
                        }
                    }
                    agent.SetDestination(target.position);
                }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    Vector3 touchMagnitude = new Vector3(CnInputManager.GetAxis("Horizontal"), CnInputManager.GetAxis("Vertical"), 0);
                    Vector3 touchPosition = transform.position + touchMagnitude.normalized;
                    Vector3 touchDirection = touchPosition - transform.position;
                    float angle = Mathf.Atan2(touchDirection.y, touchDirection.x) * Mathf.Rad2Deg;
                    angle -= 90;
                    Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.down);
                    transform.rotation = Quaternion.Lerp(transform.rotation, rotation, turnSpeed * Mathf.Min(Time.deltaTime, .04f));
                }
            }

            if(transform.position.y < -1 && !stunned)
            {
                ParticleSystem.EmissionModule emission = traileFx.emission;
                emission.enabled = false;
                stunned = true;
                speed = 0;
                StartCoroutine(Dead());
            }
        }
    }

    void FixedUpdate()
    {
        if(active && !isAI)
            rigidbody.MovePosition(transform.position + transform.forward * speed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if(keepBonesStraight)
        {
            foreach(Transform t in bones)
            {
                t.eulerAngles = new Vector3(0, t.eulerAngles.y, t.eulerAngles.z);
            }
        }
    }

    private void AddZoneScore()
    {
        if (!isInZone)
            return;

        score++;

        if (!isAI)
        {
            Transform scoreFxInstance = Instantiate(scoreFx, new Vector3(transform.position.x, transform.position.y + 2, transform.position.z), Quaternion.identity);
            TextMeshPro text = scoreFxInstance.GetComponent<TextMeshPro>();
            text.DOFade(0, 1).SetDelay(.5f);
            scoreFxInstance.DOMoveY(scoreFxInstance.position.y + 2, 1f);
            Destroy(scoreFxInstance.gameObject, 2f);
        }

    }

    private Transform GetClosestEnemyInZone(PlayerController[] enemies)
    {
        Transform bestTarget = null;
        float smallestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (PlayerController potentialTarget in enemies)
        {
            if (potentialTarget.transform != transform)
            {
                Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
                float distance = directionToTarget.sqrMagnitude;

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    bestTarget = potentialTarget.transform;
                }
            }
        }

        return bestTarget;

    }

    private Transform GetClosestEnemyInZone(List<Transform> enemies)
    {
        Transform bestTarget = null;
        float smallestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach(Transform potentialTarget in enemies)
        {
            if(potentialTarget != transform)
            {
                Vector3 directionToTarget = potentialTarget.position - currentPosition;
                float distance = directionToTarget.sqrMagnitude;

                if(distance < smallestDistance)
                {
                    smallestDistance = distance;
                    bestTarget = potentialTarget;
                }
            }
        }

        return bestTarget;

    }


    public void Punch(Transform other)
    {
        if (!canPunch)
            return;

        if(isBomber && !unstable)
        {
            unstable = true;

            Material material = transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(material.DOColor(Color.red, .25f));
            sequence.Join(transform.GetChild(0).DOScale(1.5f, .25f));
            sequence.Append(material.DOColor(Color.white, .25f));
            sequence.Join(transform.GetChild(0).DOScale(1f, .25f));
            sequence.SetLoops(5);
            sequence.OnComplete(() =>
            {
                Transform t = Instantiate(explosionFx, new Vector3(transform.position.x, 1.8f, transform.position.z), Quaternion.identity);
                Destroy(t.gameObject, 2f);
                StartCoroutine(Dead());

                Collider[] cols = Physics.OverlapSphere(transform.position, 5);
                foreach(Collider c in cols)
                {
                    Rigidbody rb = c.GetComponent<Rigidbody>();
                    rb.velocity = Vector3.zero;
                    rb.velocity = (c.transform.position - transform.position).normalized * punchForce;
                    PlayerController temp = c.GetComponent<PlayerController>();
                    temp.StartCoroutine(temp.Stun());
                }
            });
        }
        else
        {
            canPunch = false;
            animator.SetBool("attack", true);
            StartCoroutine(ResetPunch());

            PlayerController playerController = other.GetComponent<PlayerController>();
            playerController.puncher = transform;
            playerController.StartCoroutine(playerController.Stun());


            Rigidbody rb = other.GetComponent<Rigidbody>();
            rb.velocity = transform.forward * punchForce;
        }
    }

    public IEnumerator ResetPunch()
    {
        yield return new WaitForSeconds(.1f);
        keepBonesStraight = false;
        punchFx.SetActive(true);

        yield return new WaitForSeconds(.25f);
        keepBonesStraight = true;
        punchFx.SetActive(false);
        canPunch = true;
        animator.SetBool("attack", false);
    }

    public IEnumerator Stun()
    {
        stunned = true;
        ParticleSystem.EmissionModule emission = traileFx.emission;
        emission.enabled = false;

        if(isAI)
        {
            canPunch = false;
            agent.enabled = false;
        }

        else
        {
            speed = 0;
        }

        yield return new WaitForSeconds(.25f);

        // 12 radius of platform
        if(Vector3.Distance(Vector3.zero, new Vector3(transform.position.x, 0, transform.position.z)) < 12f)
        {
            rigidbody.velocity = Vector3.zero;
            stunned = false;
            emission.enabled = true;

            if (isAI)
            {
                canPunch = true;
                agent.enabled = true;
            }

            else
            {
                speed = defaultSpeed;
            }
        }
        else
        {
            if(puncher != null)
            {
                puncher.GetComponent<PlayerController>().AddKnockOutScore();
            }

            StartCoroutine(Dead());
        }
    }

    public void AddKnockOutScore()
    {
        if (isAI)
        {
            score+= 4;
        }
        else
        {
            score += 4;
            Transform scoreFxInstance = Instantiate(scoreFx, new Vector3(transform.position.x, transform.position.y + 2, transform.position.z), Quaternion.identity);
            TextMeshPro text = scoreFxInstance.GetComponent<TextMeshPro>();
            text.text = "+4";
            text.color = Color.yellow;
            text.DOFade(0, 1).SetDelay(.5f);
            scoreFxInstance.DOMoveY(scoreFxInstance.position.y + 2, 1f);
            scoreFxInstance.DOPunchScale(new Vector3(.5f, .5f, .5f), .8f);
            Destroy(scoreFxInstance.gameObject, 2f);
        }
    }

    public IEnumerator Dead()
    {
        if(!isAI)
        {
            cameraFollow.enabled = false;
        }

        yield return new WaitForSeconds(.5f);

        gameObject.SetActive(false);
        transform.position = new Vector3(0, -100, 0);

        gameManager.StartCoroutine(gameManager.Respawn(transform, 3));
    }

    private void OnEnable()
    {
        if (!active)
            return;

        canPunch = true;
        stunned = false;
        ParticleSystem.EmissionModule emission = traileFx.emission;
        emission.enabled = true;
        animator.SetBool("run", true);

        rigidbody.velocity = Vector3.zero;

        if (isAI)
        {
            agent.enabled = true;

        }

        else
        {
            cameraFollow.enabled = true;
            speed = defaultSpeed;
        }

        if(isBomber)
        {
            unstable = false;
        }

        Vector3 dir = new Vector3(transform.position.x, 0, transform.position.z) 
            - new Vector3(zone.position.x, 0, zone.position.z);

        transform.rotation = Quaternion.LookRotation(-dir);
    }

    public void Stop(bool won)
    {
        active = false;

        ParticleSystem.EmissionModule emission = traileFx.emission;
        emission.enabled = false;

        rigidbody.velocity = Vector3.zero;

        if (isAI)
            agent.enabled = false;
        else
            speed = 0;

        animator.SetBool("attack", false);
        if (won)
            animator.SetBool("won", true);
        else
            animator.SetBool("lost", true);
        animator.SetBool("run", false);
    }
}
