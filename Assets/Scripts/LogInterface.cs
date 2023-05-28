using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Toggle
{
    Time,
    Date,
    ID
}

public enum LoginState
{
    Disconnected,
    Guest,
    User,
    Admin,
    Login_Request_Sent,
    Logout_Request_Sent
}

public class LogInterface : MonoBehaviour
{
    [SerializeField]
    private string LocalName;
    [SerializeField]
    private int LogInstanceMax = 500;
    [SerializeField]
    private int LogDisplayMax = 50;

    public TMP_Text NameLabel;
    public TMP_Text LogScreen;
    public TMP_Text LoginTitle;
    public TMP_Text LoginStatus;

    public TMP_InputField LogInput;
    public TMP_InputField LoginNameInput;
    public TMP_InputField LoginPasswordInput;

    public GameObject MasterPanel;
    public GameObject MainPanel;
    public GameObject LoginPanel;

    /// <summary>
    /// Test Area for toggled states of Login Panel
    /// </summary>
    public GameObject LogOutButton;
    public GameObject Login_RegisterButton;

    public Logger MyLog;
    private StringBuilder LogBuilder = new StringBuilder();

    public bool MasterOptionsActive;
    public bool DebugScreen;

    public LoginState LoginState;
    private LoginState _loginState_cache;
    private bool[] ToggleOptions = new bool[Enum.GetNames(typeof(Toggle)).Length];
    private bool _connected_cache;
    private string _clientName_cache;
    
    public void BuildLogString()
    {
        if (MyLog.InstanceCount == 0)
            return;

        LogBuilder.Clear();

        ulong clientSpamBuffer = 0;
        DateTime clientDateBuffer = DateTime.Now;
        bool adminBuffer = false;

        for (int i = MyLog.InstanceCount - LogDisplayMax < 0 ? 0 : MyLog.InstanceCount - LogDisplayMax; i < MyLog.InstanceCount; i++)
        {
            if (MyLog[i].ClientID == 0)
            {
                if (!adminBuffer)
                {
                    clientSpamBuffer = Logger.AdminClientId;
                    adminBuffer = true;
                    LogBuilder.Append($"{MyLog[i].UserName}{(GetOption(Toggle.ID) ? $" | {MyLog[i].ClientID}" : "")}{(GetOption(Toggle.Date) ? $" | {MyLog[i].TimeStamp.ToString("d:M:y")}" : "")}\n");
                }
            }
            else
            {
                adminBuffer = false;
            }
            if (MyLog[i].ClientID.HasValue && clientSpamBuffer != MyLog[i].ClientID.Value)
            {
                clientSpamBuffer = MyLog[i].ClientID.Value;
                clientDateBuffer = MyLog[i].TimeStamp;
                LogBuilder.Append($"{MyLog[i].UserName}{(GetOption(Toggle.ID) ? $" | {MyLog[i].ClientID}" : "")}{(GetOption(Toggle.Date) ? $" | {MyLog[i].TimeStamp.ToString("d:M:y")}" : "")}\n");
            }
            if (GetOption(Toggle.Date) && clientDateBuffer.Date != MyLog[i].TimeStamp.Date)
            {
                LogBuilder.Append($"{MyLog[i].TimeStamp.ToString("d:M:y")}\n");
            }
            LogBuilder.Append($"    {(GetOption(Toggle.Time) ? $"{MyLog[i].TimeStamp.ToString("hh:mm:ss")} " : "")}- {MyLog[i].Message}\n");
        }
    }

    public void NewClientFile()
    {
        if (MyLog == null)
            return;

        Debug.Log("Sending call to logger...");
        MyLog.GenerateNewClientFile();
    }

    public void TestReadFile()
    {
        if (MyLog == null || LogScreen == null)
            return;

        LogScreen.text = MyLog.TestReadFile();
    }
    public void TestWriteFile()
    {
        if (MyLog == null || LogInput == null)
            return;

        MyLog.TestWriteFile(LogInput.text);
    }
    public void TestBuild()
    {
        if (MyLog == null)
            return;

        MyLog.BuildTestElementTree();
    }
    public void SaveInstance()
    {
        if (MyLog == null)
            return;

        MyLog.SaveTestElementTree();
    }
    public void Login_Register()
    {
        if (LoginNameInput == null || LoginPasswordInput == null)
            return;

        MyLog.SendLoginUserRequest(LoginNameInput.text, LoginPasswordInput.text);
    }

    public void SendInputAsMessage()
    {
        if (LogInput == null)
        {
            Debug.LogError("Missing LogInput");
            return;
        }
        if (Logger.StringSizeBytes(LogInput.text) > (int)Chattribute.Message)
        {
            Debug.LogError("Message too long");
            return;
        }

        MyLog.ChatMessage(LogInput.text);
        LogInput.text = string.Empty;
        LogInput.MoveTextStart(false);
        //LogInput.MoveToEndOfLine(false, true);
    }
    public void ToggleLoginPanel()
    {
        if (LoginPanel != null)
            LoginPanel.SetActive(!LoginPanel.activeSelf);
    }
    public void SetLoginPanelActive(bool active)
    {
        if (LoginPanel != null)
            LoginPanel.SetActive(active);
    }
    public void ToggleOption(Toggle option) { ToggleOption((int)option); }
    public void ToggleOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= ToggleOptions.Length)
        {
            Debug.LogError("OptionIndex out of bounds");
            return;
        }

        SetOption(optionIndex, !ToggleOptions[optionIndex]);
    }
    public void SetOption(Toggle option, bool value) { SetOption((int)option, value); }
    public void SetOption(int optionIndex, bool value)
    {
        if (optionIndex < 0 || optionIndex >= ToggleOptions.Length)
        {
            Debug.LogError("OptionIndex out of bounds");
            return;
        }

        ToggleOptions[optionIndex] = value;
    }
    public bool GetOption(Toggle option) { return GetOption((int)option); }
    public bool GetOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= ToggleOptions.Length)
        {
            Debug.LogError("OptionIndex out of bounds");
            return false;
        }

        return ToggleOptions[optionIndex];
    }

    void UpdateName()
    {
        if (NameLabel != null)
            NameLabel.text = MyLog != null && MyLog.IsSpawned ? $"<< [Online] {MyLog.MyName} >>" :
                $"<< [Offline] {(LocalName == null ? "[No Local Name]" : LocalName)} >>";
    }
    void UpdateLoginState()
    {
        //Debug.Log("Updating Login State...");
        //LoginState oldLoginState = _loginState_cache;
        if (!MyLog.IsSpawned)
        {
            //Debug.Log("IsDisconnected");
            LoginState = LoginState.Disconnected;
        }
        else if (MyLog.IsAdmin)
        {
            //Debug.Log("IsAdmin");
            LoginState = LoginState.Admin;
        }
        else if (MyLog.IsGuest)
        {
            //Debug.Log("IsGuest");
            LoginState = LoginState.Guest;
        }
        else
            LoginState = LoginState.User;

        if (LoginState != _loginState_cache)
        {
            Debug.Log($"Current LoginState: {LoginState}");
            _loginState_cache = LoginState;
            UpdateName();
            UpdateLoginPanel();
        }
    }
    void UpdateLoginPanel()
    {
        if (LoginTitle != null)
            LoginTitle.text = MyLog.IsSpawned ? MyLog.IsAdmin ? $"This is your server: {MyLog.ServerInstance.ServerName}" : $"Login to: {MyLog.ServerInstance.ServerName}" : "Not connected to server";

        if (LoginStatus != null)
            LoginStatus.text = MyLog.IsSpawned ? MyLog.IsAdmin ? $"You do not need to be here..." : "Please provide your loginName and passWord for this server..." : "Please connect to a server first before logging in..."; 

        if (LogOutButton != null)
            LogOutButton.SetActive(!MyLog.IsGuest && !MyLog.IsAdmin);

        if (Login_RegisterButton != null)
            Login_RegisterButton.SetActive(MyLog.IsSpawned && !MyLog.IsAdmin);
    }
    void Start()
    {
        SetLoginPanelActive(false);
        UpdateName();
        UpdateLoginPanel();
    }
    void Update()
    {
        if (MainPanel != null &&
            MasterPanel != null)// &&
            //MyLog != null)
        {
            RectTransform main = ((RectTransform)MainPanel.transform);
            RectTransform master = ((RectTransform)MasterPanel.transform);

            if (!MasterPanel.activeSelf &&
            //MyLog.NetworkManager.IsServer)
            MasterOptionsActive)
            {
                MasterPanel.SetActive(true);
                main.offsetMin = new Vector2(main.offsetMin.x + master.sizeDelta.x, main.offsetMin.y);
            }
                

            if (MasterPanel.activeSelf &&
            //!MyLog.NetworkManager.IsServer)
            !MasterOptionsActive)
            {
                MasterPanel.SetActive(false);
                main.offsetMin = new Vector2(main.offsetMin.x - master.sizeDelta.x, main.offsetMin.y);
            }
                
        }
            

        if (LogScreen == null)
        {
            Debug.LogError("Missing LogScreen");
            return;
        }

        if (Input.GetButtonDown("Submit"))
        {
            Debug.Log("Message Sent!");
            SendInputAsMessage();
        }

        if (Input.GetButton("FontSize"))
        {
            int xDelta = (int)Input.mouseScrollDelta.x;
            int yDelta = (int)Input.mouseScrollDelta.y;

            LogScreen.fontSize += (xDelta + yDelta);
            LogScreen.transform.localPosition = new Vector3(LogScreen.transform.localPosition.x, 0, LogScreen.transform.localPosition.z);
        }
        if (MyLog != null)
        {
            UpdateLoginState();

            if (!DebugScreen)
            {
                BuildLogString();
                LogScreen.text = LogBuilder.ToString();
            }
        }
        
        
    }
}
