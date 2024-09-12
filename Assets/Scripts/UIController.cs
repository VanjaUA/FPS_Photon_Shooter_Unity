using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [SerializeField] public TMP_Text overheatedMessage;
    [SerializeField] public Slider weaponTempSlider;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private GameObject crosshair;

    [SerializeField] private TMP_Text killsText, deathsText;

    [SerializeField] public GameObject leaderBoard;
    [SerializeField] public LeaderboardPlayer leaderboardPlayerDisplay;

    [SerializeField] private GameObject endScreen;

    public GameObject optionsScreen;

    public TMP_Text timerText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        deathScreen.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleOptions();
        }
    }

    public void ShowDeathScreen(string damager) 
    {
        deathScreen.SetActive(true);
        crosshair.SetActive(false);
        deathText.text = "You were killed by " + damager;
    }

    public void HideDeathScreen() 
    {
        deathScreen.SetActive(false);
        crosshair.SetActive(true);
    }

    public void UpdateHealthBar(int healthAmount,int maxHealth) 
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = healthAmount;

        healthText.text = "HP: " + healthAmount;
    }

    public void UpdateStatsDisplay(int kills, int deaths)
    {
        killsText.text = "Kills: " + kills;
        deathsText.text = "Deaths: " + deaths;
    }

    public void ShowEndScreen() 
    {
        endScreen.gameObject.SetActive(true);
    }
    public void HideEndScreen() 
    {
        endScreen.gameObject.SetActive(false);
    }

    public void ToggleOptions () 
    {
        if (optionsScreen.activeInHierarchy)
        {
            optionsScreen.SetActive(false);
        }
        else
        {
            optionsScreen.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ReturnToMainMenu() 
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame() 
    {
        Application.Quit();
    }
}
