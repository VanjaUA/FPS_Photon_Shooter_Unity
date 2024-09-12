using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;
    private GameObject player;

    [SerializeField] private GameObject deathEffect;

    [SerializeField] private float respawnTime = 5f;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer() 
    {
        Transform spawnPoint = SpawnManager.Instance.GetSpawnPoint();

        player = PhotonNetwork.Instantiate(playerPrefab.name,spawnPoint.position,spawnPoint.rotation);
    }

    public void Die(string damager) 
    {
        MatchManager.Instance.UpdateStateSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        if (player != null)
        {
            StartCoroutine(DieCoroutine(damager));
        }
    }

    public IEnumerator DieCoroutine(string damager) 
    {
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);
        player = null;

        UIController.Instance.ShowDeathScreen(damager);
        yield return new WaitForSeconds(respawnTime);

        UIController.Instance.HideDeathScreen();

        if (MatchManager.Instance.gameState == MatchManager.GameState.Playing)
        {
            SpawnPlayer();
        }
    }

}
