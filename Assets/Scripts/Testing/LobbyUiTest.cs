using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityProjectCloner;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay.Scheduler;
using Unity.Services.Relay.Http;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;

public class LobbyUiTest : MonoBehaviour
{
    public LobbyEngineTest Lobby;

    //public TMP_Text ListText;
    public LobbyPanel LobbyPanelSample;
    public Transform LobbyPanelContent;

    public TMP_InputField PlayerName;
    public TMP_InputField LobbyName;
    public TMP_InputField MaxPlayers;

    private List<LobbyPanel> _lobbyPanels = new List<LobbyPanel>();
    private int _lobbyCountCache = 0;
    private Lobby _joinedCache = null;
    //private int _joinedCountCache = 0;
    private StringBuilder _lobbyQueryBuilder = new StringBuilder();

    public async void ConnectAndAuthenticate()
    {
        if (PlayerName == null)
            return;

        await Lobby.Authenticate(PlayerName.text);
    }

    public void HostPublicLobby()
    {
        if (Lobby == null ||
            LobbyName == null ||
            MaxPlayers == null)
            return;

        int playerCount;
        if (!int.TryParse(MaxPlayers.text, out playerCount))
            return;

        Lobby.HostLobby(LobbyName.text, playerCount);
    }

    public void JoinLobby(string lobbyId)
    {
        if (Lobby == null ||
            PlayerName == null)
            return;

        Lobby.JoinLobby(PlayerName.text, lobbyId);
    }

    public void LeaveLobby(string lobbyId)
    {
        if (Lobby == null)
            return;

        Lobby.LeaveLobby(lobbyId);
    }

    public void UpdateLobbyList()
    {
        if (Lobby == null ||
            !Lobby.IsSignedIn ||
            LobbyPanelSample == null)
            return;

        //Debug.Log("Source Targets good");

        Debug.Log($"{(Lobby.CurrentLobby == null ? "Not in lobby" : Lobby.CurrentLobby.Name)}");

        if (Lobby.FoundLobbies.Count == _lobbyCountCache &&
            Lobby.CurrentLobby == _joinedCache)
            return;

        Debug.Log("Change detected");

        _lobbyCountCache = Lobby.FoundLobbies.Count;
        _joinedCache = Lobby.CurrentLobby;

        BuildLobbyPanels(_joinedCache == null);
    }

    private void BuildLobbyPanels(bool join = true)
    {
        foreach (LobbyPanel panel in _lobbyPanels)
            Destroy(panel.gameObject);

        _lobbyPanels.Clear();
        //int listCount = 0;
        //Dictionary<string, Lobby> targetCache = join ? Lobby.FoundLobbies : Lobby.JoinedLobbies;

        if (join)
            foreach (Lobby nextLobby in Lobby.FoundLobbies.Values)
            {
                if (join && !nextLobby.Data.ContainsKey("JoinCode"))
                    continue;

                _lobbyQueryBuilder.Clear();
                _lobbyQueryBuilder.Append(
                        $"[Name:{nextLobby.Name}]\n" +
                        $"[Players:{nextLobby.Players.Count}/{nextLobby.MaxPlayers}]\n" +
                        $"[Created:{nextLobby.Created}]\n\n");
                
                LobbyPanel nextJoinPanel = Instantiate(LobbyPanelSample.gameObject, LobbyPanelContent).GetComponent<LobbyPanel>();
                nextJoinPanel.LinkedLobby = nextLobby;
                nextJoinPanel.InfoField.text = _lobbyQueryBuilder.ToString();
                nextJoinPanel.Join_LeaveButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "JOIN";
                nextJoinPanel.Join_LeaveButton.onClick.AddListener(delegate { JoinLobby(nextLobby.Id); });
                _lobbyPanels.Add(nextJoinPanel);
            }
        else
        {
            Lobby joinedLobby = Lobby.CurrentLobby;

            _lobbyQueryBuilder.Clear();
            _lobbyQueryBuilder.Append(
                    $"[Name:{joinedLobby.Name}]\n" +
                    $"[Players:{joinedLobby.Players.Count}/{joinedLobby.MaxPlayers}]\n" +
                    $"[Created:{joinedLobby.Created}]\n\n");

            LobbyPanel leavePanel = Instantiate(LobbyPanelSample.gameObject, LobbyPanelContent).GetComponent<LobbyPanel>();
            leavePanel.LinkedLobby = joinedLobby;
            leavePanel.InfoField.text = _lobbyQueryBuilder.ToString();
            leavePanel.Join_LeaveButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "LEAVE";
            leavePanel.Join_LeaveButton.onClick.AddListener(delegate { LeaveLobby(joinedLobby.Id); });
            _lobbyPanels.Add(leavePanel);
        }

    }

    void Start()
    {

    }

    void Update()
    {
        UpdateLobbyList();
    }
}
