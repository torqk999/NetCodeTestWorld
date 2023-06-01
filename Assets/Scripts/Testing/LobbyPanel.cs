using UnityEngine;
using Unity.Services.Lobbies.Models;
using TMPro;
using UnityEngine.UI;

public class LobbyPanel : MonoBehaviour
{
    public TMP_Text InfoField;
    public Button Join_LeaveButton;
    public Lobby LinkedLobby;
    public bool IsGood =>
        InfoField != null &&
        Join_LeaveButton != null &&
        LinkedLobby != null;
}
