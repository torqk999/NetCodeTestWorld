using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;
using System;

public class TestPing : NetworkBehaviour
{
    public string MyName;
    public TMP_Text MyLabel;
    public NetworkVariable<FixedString64Bytes> NetVariable = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    //Dictionary<Logger> 
    
    // Start is called before the first frame update
    void Start()
    {
        NetVariable.OnValueChanged += OverWrite;

        if (IsServer)
        {
            
            NetworkManager.OnClientConnectedCallback += Registration;
            //NetworkManager.EventHandler temp = MyEvent;
            //if (temp != null)
            //{
            //    temp();
            //}
        }
            
        
    }

    private void Registration(ulong obj)
    {
        List<NetworkClient> clients = (List<NetworkClient>)NetworkManager.ConnectedClientsList;
        NetworkClient registry = clients.Find(x => x.ClientId == obj);
        if (registry == null)
        {
            Debug.LogError($"Registry not found: {obj}");
            return;
        }

        NetworkObject logBox = registry.OwnedObjects.Find(x => x.GetComponent<Logger>() != null);
        if (logBox == null)
        {
            Debug.LogError($"Logger not found: {obj}");
            return;
        }

        Logger regLogger = logBox.GetComponent<Logger>();

        
    }

    //private TestPing GetPing(ulong obj)
    //{
    //    NetworkManager.
    //}

    public void OverWriteMyName()
    {
        if (IsServer)
            NetVariable.Value = MyName;
        else
            RequestOverWrite(MyName);
    }

    public void OverWriteTargetName()
    {

    }

    private void RequestOverWrite(string overWrite)
    {
        if (!IsServer)
            return;

        NetVariable.Value = overWrite;
    }

    public void OverWrite(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        if (NetVariable.Value != newValue)
            NetVariable.Value = newValue;
    }

    // Update is called once per frame
    void Update()
    {
        if (MyLabel != null)
            MyLabel.text = NetVariable.Value.ToString();
    }
}
