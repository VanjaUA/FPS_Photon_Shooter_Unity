using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string name, int actor, int kills = 0, int deaths = 0)
    {
        this.name = name;
        this.actor = actor;
        this.kills = kills;
        this.deaths = deaths;
    }   
}

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch,
        TimerSync,
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Ending,
    }

    public static MatchManager Instance { get; private set; }

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;

    private List<LeaderboardPlayer> leaderboardPlayers = new List<LeaderboardPlayer>();

    [SerializeField] private int killsToWin = 2;
    [SerializeField] public Transform camPoint;
    [SerializeField] public GameState gameState = GameState.Waiting;
    [SerializeField] private float waitAfterEnding = 5f;

    public bool perpetual;

    public float matchLength = 60f;
    private float currentMatchTime;
    private float sendTimer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            SceneManager.LoadScene(0); //Main menu
            return;
        }

        NewPlayerSend(PhotonNetwork.NickName);

        gameState = GameState.Playing;

        SetupTimer();

        if (PhotonNetwork.IsMasterClient == false)
        {
            UIController.Instance.timerText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && gameState != GameState.Ending)
        {
            if (UIController.Instance.leaderBoard.activeInHierarchy)
            {
                UIController.Instance.leaderBoard.SetActive(false);
            }
            else
            {
                ShowLeaderboard();
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (currentMatchTime > 0 && gameState == GameState.Playing)
            {
                currentMatchTime -= Time.deltaTime;
                if (currentMatchTime <= 0.1f)
                {
                    currentMatchTime = 0f;
                    gameState = GameState.Ending;

                    ListPlayersSend();

                    StateCheck();
                }

                UpdateTimerDisplay();

                sendTimer -= Time.deltaTime;
                if (sendTimer <= 0)
                {
                    sendTimer++;

                    TimerSend();
                }
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200)
        {
            return;
        }
        EventCodes eventCode = (EventCodes)photonEvent.Code;
        object[] data = (object[])photonEvent.CustomData;

        Debug.Log("Received event " + eventCode);

        switch (eventCode)
        {
            case EventCodes.NewPlayer:
                NewPlayerReceive(data);
                break;
            case EventCodes.ListPlayers:
                ListPlayersReceive(data);
                break;
            case EventCodes.UpdateStat:
                UpdateStateReceive(data);
                break;
            case EventCodes.NextMatch:
                NextMatchRecieve();
                break;
            case EventCodes.TimerSync:
                TimerRecieve(data);
                break;
            default:
                break;
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene(0); //Main menu
    }

    public void NewPlayerSend(string userName) 
    {
        object[] package = new object[4] {userName,PhotonNetwork.LocalPlayer.ActorNumber,0,0};

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions {Receivers = ReceiverGroup.MasterClient},
            new SendOptions {Reliability = true});
    }
    public void NewPlayerReceive(object[] receivedData) 
    {
        PlayerInfo player = new PlayerInfo((string)receivedData[0],(int)receivedData[1], (int)receivedData[2], (int)receivedData[3]);
        allPlayers.Add(player);

        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        object[] package = new object[allPlayers.Count + 1];

        package[0] = gameState;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[4] { allPlayers[i].name, allPlayers[i].actor, allPlayers[i].kills, allPlayers[i].deaths};
            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }
    public void ListPlayersReceive(object[] receivedData)
    {
        allPlayers.Clear();

        gameState = (GameState)receivedData[0];

        for (int i = 1; i < receivedData.Length; i++)
        {
            object[] piece = (object[])receivedData[i];
            PlayerInfo player = new PlayerInfo((string)piece[0], (int)piece[1], (int)piece[2], (int)piece[3]);
            allPlayers.Add(player);

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
            }
        }

        StateCheck();
    }

    public void UpdateStateSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void UpdateStateReceive(object[] receivedData)
    {
        int actor = (int)receivedData[0];
        int statType = (int)receivedData[1];
        int amount = (int)receivedData[2];

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].actor == actor)
            {
                switch (statType)
                {
                    //kills
                    case 0:
                        allPlayers[i].kills += amount;
                        Debug.Log(allPlayers[i].name + " kills is " + allPlayers[i].kills);
                        break;

                    //deaths
                    case 1:
                        allPlayers[i].deaths += amount;
                        Debug.Log(allPlayers[i].name + " deaths is " + allPlayers[i].deaths);
                        break;
                }
                if (i == index)
                {
                    UpdateStatsDisplay();
                }
                if (UIController.Instance.leaderBoard.activeInHierarchy)
                {
                    ShowLeaderboard();
                }
                break;
            }
        }

        ScoreCheck();
    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }
    public void NextMatchRecieve()
    {
        gameState = GameState.Playing;

        UIController.Instance.HideEndScreen();
        UIController.Instance.leaderBoard.SetActive(false);

        foreach (PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.Instance.SpawnPlayer();

        SetupTimer();
    }
    public void TimerSend()
    {
        object[] package = new object[] {(int)currentMatchTime,gameState };

        PhotonNetwork.RaiseEvent(
             (byte)EventCodes.TimerSync,
             package,
             new RaiseEventOptions { Receivers = ReceiverGroup.All },
             new SendOptions { Reliability = true });
    }

    public void TimerRecieve(object[] recievedData)
    {
        currentMatchTime = (int)recievedData[0];
        gameState = (GameState)recievedData[1];

        UpdateTimerDisplay();

        UIController.Instance.timerText.gameObject.SetActive(true);
    }

    public void UpdateStatsDisplay() 
    {
        if (allPlayers.Count <= index)
        {
            UIController.Instance.UpdateStatsDisplay(0, 0);
            return;
        }
        UIController.Instance.UpdateStatsDisplay(allPlayers[index].kills,allPlayers[index].deaths);
    }

    private void ShowLeaderboard() 
    {
        UIController.Instance.leaderBoard.SetActive(true);

        foreach (LeaderboardPlayer leaderboardPlayer in leaderboardPlayers)
        {
            Destroy(leaderboardPlayer.gameObject);
        }
        leaderboardPlayers.Clear();

        UIController.Instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sortedPlayers = SortPlayers(allPlayers);

        foreach (PlayerInfo player in sortedPlayers)
        {
            LeaderboardPlayer newLeaderboardPlayer = Instantiate(UIController.Instance.leaderboardPlayerDisplay,
                UIController.Instance.leaderboardPlayerDisplay.transform.parent);

            newLeaderboardPlayer.SetDetails(player.name,player.kills,player.deaths);
            newLeaderboardPlayer.gameObject.SetActive(true);

            leaderboardPlayers.Add(newLeaderboardPlayer);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players) 
    {
        List<PlayerInfo> sortedPlayers = new List<PlayerInfo>();

        while (sortedPlayers.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo player in players)
            {
                if (sortedPlayers.Contains(player))
                {
                    continue;
                }
                if (player.kills > highest)
                {
                    selectedPlayer = player;
                    highest = player.kills;
                }
            }
            sortedPlayers.Add(selectedPlayer);
        }

        return sortedPlayers;
    }

    private void ScoreCheck() 
    {
        bool winnerFound = false;
        foreach (PlayerInfo player in allPlayers)
        {
            if (player.kills >= killsToWin && killsToWin > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && gameState != GameState.Ending)
            {
                gameState = GameState.Ending;
                ListPlayersSend();
            }
        }
    }

    private void StateCheck() 
    {
        if (gameState == GameState.Ending)
        {
            EndGame();
        }
    }

    private void EndGame() 
    {
        gameState = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.Instance.ShowEndScreen();
        ShowLeaderboard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = camPoint.transform.position;
        Camera.main.transform.rotation = camPoint.transform.rotation;

        StartCoroutine(EndCoroutine());
    }

    private IEnumerator EndCoroutine() 
    {
        yield return new WaitForSeconds(waitAfterEnding);

        if (perpetual == false)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (Launcher.Instance.changeMapBetweenRounds == false)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0,Launcher.Instance.allMaps.Length);

                    if (Launcher.Instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.Instance.allMaps[newLevel]);
                    }
                }
            }
        }
    }


    public void SetupTimer() 
    {
        if (matchLength > 0)
        {
            currentMatchTime = matchLength;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay() 
    {
        System.TimeSpan timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        UIController.Instance.timerText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }

}
