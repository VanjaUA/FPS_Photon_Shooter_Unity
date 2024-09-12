using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance { get; private set; }

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TMP_Text loadingText;

    [SerializeField] private GameObject menuButtons;
    [SerializeField] private GameObject roomTestButton;

    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private TMP_InputField roomNameInput;

    [SerializeField] private GameObject roomScreen;
    [SerializeField] private TMP_Text roomNameText, playerNameLabel;
    private List<TMP_Text> allPlayerNames = new List<TMP_Text>();

    [SerializeField] private GameObject errorScreen;
    [SerializeField] private TMP_Text errorText;

    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private RoomButton roomButton;
    private List<RoomButton> allRoomButtons = new List<RoomButton>();

    [SerializeField] private GameObject nicknameInputScreen;
    [SerializeField] private TMP_InputField nicknameInput;
    public static bool hasSetNickname;
    private const string PLAYER_NICKNAME_FIELD = "playerNickname";

    [SerializeField] private string levelToPlay;
    [SerializeField] private GameObject startGameButton;

    public string[] allMaps;
    public bool changeMapBetweenRounds = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CloseMenus();

        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to network...";

        if (PhotonNetwork.IsConnected == false)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

    #if UNITY_EDITOR
        roomTestButton.SetActive(true);
    #endif

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        loadingText.text = "Joining lobby...";

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 10000).ToString();


        if (hasSetNickname == false)
        {
            CloseMenus();
            nicknameInputScreen.SetActive(true);

            if (PlayerPrefs.HasKey(PLAYER_NICKNAME_FIELD))
            {
                nicknameInput.text = PlayerPrefs.GetString(PLAYER_NICKNAME_FIELD);
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString(PLAYER_NICKNAME_FIELD);
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenus();
        errorText.text = "Failde to create room " + message;
        errorScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var item in roomList)
        {
            Debug.Log(item + ",");
        }

        roomButton.gameObject.SetActive(false);

        foreach (RoomButton roomButton in allRoomButtons)
        {
            Destroy(roomButton.gameObject);
        }
        allRoomButtons.Clear();

        foreach (RoomInfo roomInfo in roomList)
        {
            if (roomInfo.PlayerCount != roomInfo.MaxPlayers)
            {
                RoomButton newRoomButton = Instantiate(roomButton, roomButton.transform.parent);
                newRoomButton.SetButtonDetails(roomInfo);
                newRoomButton.gameObject.SetActive(true);

                allRoomButtons.Add(newRoomButton);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);

        allPlayerNames.Add(newPlayerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    private void CloseMenus() 
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nicknameInputScreen.SetActive(false);
    }

    private void ListAllPlayers() 
    {
        playerNameLabel.gameObject.SetActive(false);

        foreach (TMP_Text playerName in allPlayerNames)
        {
            Destroy(playerName.gameObject);
        }
        allPlayerNames.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        foreach (Player player in players)
        {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            newPlayerLabel.text = player.NickName;
            newPlayerLabel.gameObject.SetActive(true);

            allPlayerNames.Add(newPlayerLabel);
        }
    }

    public void OpenRoomCreate() 
    {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom() 
    {
        if (string.IsNullOrEmpty(roomNameInput.text) == false)
        {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text,roomOptions);

            CloseMenus();
            loadingText.text = "Creating room...";
            loadingScreen.SetActive(true);
        }
    }

    public void CloseErrorScreen() 
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom() 
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Leaving Room";
        loadingScreen.SetActive(true);
    }

    public void OpenRoomBrowser() 
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);
    }

    public void CloseRoomBrowser() 
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void JoinRoom(RoomInfo roomInfo) 
    {
        PhotonNetwork.JoinRoom(roomInfo.Name);

        CloseMenus();
        loadingText.text = "Joining room...";
        loadingScreen.SetActive(true);
    }

    public void QuitGame() 
    {
        Application.Quit();
    }

    public void SetNickname() 
    {
        if (string.IsNullOrEmpty(nicknameInput.text) == false)
        {
            PhotonNetwork.NickName = nicknameInput.text;

            PlayerPrefs.SetString(PLAYER_NICKNAME_FIELD, nicknameInput.text);

            CloseMenus();
            menuButtons.SetActive(true);

            hasSetNickname = true;
        }
    }

    public void StartGame() 
    {
        //PhotonNetwork.LoadLevel(levelToPlay);

        PhotonNetwork.LoadLevel(allMaps[Random.Range(0, allMaps.Length)]);
    }

    public void QuickJoin() 
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 8;
        PhotonNetwork.CreateRoom("TEST", roomOptions);
        CloseMenus();
        loadingText.text = "Creating room...";
        loadingScreen.SetActive(true);
    }
}
