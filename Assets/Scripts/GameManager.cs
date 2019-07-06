using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public List<Transform> playersInZone = new List<Transform>();
    public PlayerController[] players;
    public bool gameStarted;
    public Transform respawnTransform;
    public float timer;
    public TextMeshProUGUI timerLabel;
    public Color[] playerColors;
    public NameManager nameManager;
    public TMP_InputField nameField;
    public Transform loadingImage;
    public TextMeshProUGUI playerCountLabel;
    public TextMeshProUGUI hintLabel;
    public string[] hints;
    public GameObject matchMakingPanel;
    public GameObject menuPanel;
    public GameObject gamePanel;
    public GameObject gameOverPanel;
    public Transform[] bots;
    public Transform zone;
    public Vector3 inGameCameraPosition;
    public Vector3 matchMakingCameraPosition;
    public Transform[] gameoverScoreCard;
    public TextMeshProUGUI gameOverRank;
    public GameObject respawnPanel;
    public TextMeshProUGUI respawnTimerLabel;

    private int respawnTries;
    private List<Transform> sortedList = new List<Transform>();
    private Camera cam;
    private CameraFollow cameraFollow;
    private string playerName;


    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        cameraFollow = cam.GetComponent<CameraFollow>();

        if (PlayerPrefs.HasKey("playerName"))
            playerName = PlayerPrefs.GetString("playerName");
        else
            playerName = "Player";

        nameField.text = playerName;

        loadingImage.DORotate(new Vector3(0, 0, -1), .005f).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);

        SetupPlayer();

        hintLabel.text = hints[UnityEngine.Random.Range(0, hints.Length)];

    }

    // Update is called once per frame
    void Update()
    {
        foreach (PlayerController player in players)
        {
            if(playersInZone.Contains(player.transform))
            {
                if (!player.isInZone)
                    player.isInZone = true;
            }
            else
            {
                if (player.isInZone)
                    player.isInZone = false;
            }
        }

        if (gameStarted)
        {
            if (timer > 0)
            {
                if (timer < 10 && timerLabel.color != Color.red)
                    timerLabel.color = Color.red;

                timer -= Time.deltaTime;
                timerLabel.text = ((int)timer / 60).ToString() + ":" + ((int)timer % 60).ToString("00");

                UpdateScore();
            }
            else
            {
                StartCoroutine(GameOver());
            }
        }
    }

    private void UpdateScore()
    {
        sortedList.Clear();
        Dictionary<Transform, int> unsortedDic = new Dictionary<Transform, int>();
        foreach(PlayerController p in players)
        {
            unsortedDic.Add(p.transform, p.score);
        }

        foreach(var item in unsortedDic.OrderByDescending(i => i.Value))
        {
            sortedList.Add(item.Key);
        }

        for(int i = 0; i< players.Length; i++)
        {
            PlayerController playerController = sortedList[i].GetComponent<PlayerController>();

            if (i == 0)
                playerController.crown.SetActive(true);
            else
                playerController.crown.SetActive(false);

            playerController.scoreCard.SetSiblingIndex(i);
            playerController.scoreCard.GetChild(0).GetComponent<TextMeshProUGUI>().text = (i + 1) + ".";
            playerController.scoreCard.GetChild(2).GetComponent<TextMeshProUGUI>().text = "- " + playerController.score;
        }
    }

    private IEnumerator GameOver()
    {
        gameStarted = false;

        for(int i = 0; i < sortedList.Count; i++) {
            if (i == 0)
            {
                cameraFollow.FocusOnWinner(sortedList[i].transform);
                sortedList[i].GetComponent<PlayerController>().Stop(true);
            }
            else
                sortedList[i].GetComponent<PlayerController>().Stop(false);
        }

        for(int i = 0; i < players.Length; i++)
        {
            PlayerController temp = sortedList[i].GetComponent<PlayerController>();
            gameoverScoreCard[i].GetChild(0).GetComponent<TextMeshProUGUI>().text = temp.scoreCard.GetSiblingIndex() + 1 + ".";
            gameoverScoreCard[i].GetChild(1).GetComponent<TextMeshProUGUI>().text = temp.playerName.text;
            gameoverScoreCard[i].GetChild(2).GetComponent<TextMeshProUGUI>().text = "- "+ temp.score;
            gameoverScoreCard[i].GetComponent<Image>().color = temp.scoreCard.GetComponent<Image>().color;

            if (!temp.isAI)
                gameOverRank.text = "You finished " + (i + 1) + GetRankOrdinal(i+1) +"!";
        }

        yield return new WaitForSeconds(2f);

        gamePanel.SetActive(false);
        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public IEnumerator Respawn(Transform t, int delay)
    {
        if(!t.GetComponent<PlayerController>().isAI)
        {
            respawnPanel.SetActive(true);
            gamePanel.SetActive(false);

            yield return new WaitForSeconds(delay / 3);
            respawnTimerLabel.text = "2";

            yield return new WaitForSeconds(delay / 3);
            respawnTimerLabel.text = "1";

            yield return new WaitForSeconds(delay / 3);
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        if (!t.GetComponent<PlayerController>().isAI)
        {
            respawnPanel.SetActive(false);
            gamePanel.SetActive(true);
        }

        if(gameStarted)
        {
            Spawn(t);
        }
    }

    private void Spawn(Transform t)
    {
        respawnTransform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 359), 0);
        Collider[] cols = Physics.OverlapSphere(respawnTransform.GetChild(0).position, 5);

        bool playerNearby = false;
        foreach (Collider component in cols)
        {
            if(component.CompareTag("player"))
            {
                playerNearby = true;
            }
        }

        if(!playerNearby)
        {
            t.position = respawnTransform.GetChild(0).position;
            t.gameObject.SetActive(true);
            respawnTries = 0;
        }
        else
        {
            if(respawnTries < 10)
            {
                respawnTries++;
                Spawn(t);
            }
            else
            {
                t.position = respawnTransform.GetChild(0).position;
                t.gameObject.SetActive(true);
                respawnTries = 0;
            }
        }

    }

    private void SetupPlayer()
    {
        int[] index = { 0, 1, 2, 3, 4};
        System.Random random = new System.Random();
        int[] randomIndex = index.OrderBy(x => random.Next()).ToArray();

        for ( int i = 0; i < players.Length; i++) {

            string n = string.Empty;
            if (i == 0)
                n = playerName;
            else
                n = nameManager.randomNames[UnityEngine.Random.Range(0, nameManager.randomNames.Length)];

            players[i].scoreCard.GetComponent<Image>().color = playerColors[i];
            players[i].scoreCard.GetChild(0).GetComponent<TextMeshProUGUI>().text = (i + 1) + ".";
            players[i].scoreCard.GetChild(1).GetComponent<TextMeshProUGUI>().text = n;
            players[i].scoreCard.GetChild(2).GetComponent<TextMeshProUGUI>().text ="- 0";

            Material material = players[i].transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().material;
            material.color = playerColors[i];

            players[i].playerName.text = n;
            players[i].playerName.color = playerColors[i];
        }
    }

    public void SavePlayerName()
    {
        playerName = nameField.text;
        PlayerPrefs.SetString("playerName", playerName);
        players[0].playerName.text = playerName;
        players[0].scoreCard.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = playerName;
    }

    private IEnumerator DoMatchMaking()
    {
        cam.transform.DOMove(matchMakingCameraPosition, .5f);

        playerCountLabel.text = "1/5";
        yield return new WaitForSeconds(UnityEngine.Random.Range(.25f, .7f));
        playerCountLabel.text = "2/5";
        bots[0].gameObject.SetActive(true);
        yield return new WaitForSeconds(UnityEngine.Random.Range(.25f, .7f));
        playerCountLabel.text = "3/5";
        bots[1].gameObject.SetActive(true);
        yield return new WaitForSeconds(UnityEngine.Random.Range(.25f, .7f));
        playerCountLabel.text = "4/5";
        bots[2].gameObject.SetActive(true);
        yield return new WaitForSeconds(UnityEngine.Random.Range(.25f, .7f));
        playerCountLabel.text = "Starting game..";
        bots[3].gameObject.SetActive(true);
        yield return new WaitForSeconds(UnityEngine.Random.Range(.25f, .7f));
        zone.gameObject.SetActive(true);
        bots[4].gameObject.SetActive(true);
        matchMakingPanel.SetActive(false);
        gamePanel.SetActive(true);

        cam.transform.DOMove(inGameCameraPosition, .5f).OnComplete(() => {
            cameraFollow.enabled = true;
        });
    }

    public void SearchMatch()
    {

        menuPanel.SetActive(false);
        matchMakingPanel.SetActive(true);
        StartCoroutine(DoMatchMaking());
    }

    public void StartGame()
    {
        gameStarted = true;
    }

    private string GetRankOrdinal(int rank)
    {
        string ordinal = string.Empty;
        switch(rank)
        {
            case 1:
                ordinal = "st";
                break;
            case 2:
                ordinal = "nd";
                break;
            case 3:
                ordinal = "rd";
                break;
            case 4:
                ordinal = "th";
                break;
            case 5:
                ordinal = "th";
                break;
        }
        return ordinal;
    }

}
