using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;

    private RoomInfo roomInfo;

    public void SetButtonDetails(RoomInfo roomInfo) 
    {
        this.roomInfo = roomInfo;

        buttonText.text = roomInfo.Name;
    }

    public void OpenRoom() 
    {
        Launcher.Instance.JoinRoom(roomInfo);
    }
}
