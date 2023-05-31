using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Toggle
{
    Time,
    Date,
    ID_Client,
    ID_User
}

public enum LoginState
{
    Disconnected,
    UnHandled,
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
    public GameObject LogOutButton;
    public GameObject Login_RegisterButton;

    public GameObject MemberPanel;
    public GameObject SampleSubMemberPanel;
    public GameObject SampleMemberButton;

    public Logger MyLogger;
    private StringBuilder LogBuilder = new StringBuilder();

    public bool MasterOptionsActive;
    public bool MemberPanelActive;
    public bool DebugScreen;

    public LoginState LoginState;
    private LoginState _loginState_cache;
    private bool[] ToggleOptions = new bool[Enum.GetNames(typeof(Toggle)).Length];
    private bool _connected_cache;
    private string _clientName_cache;
    
    public void BuildLogString()
    {
        if (MyLogger.InstanceCount == 0)
            return;

        LogBuilder.Clear();

        ulong clientSpamBuffer = 0;
        DateTime clientDateBuffer = DateTime.Now;
        bool adminBuffer = false;

        for (int i = MyLogger.InstanceCount - LogDisplayMax < 0 ? 0 : MyLogger.InstanceCount - LogDisplayMax; i < MyLogger.InstanceCount; i++)
        {
            if (MyLogger.LogInstance[i].ClientID == 0)
            {
                if (!adminBuffer)
                {
                    clientSpamBuffer = Logger.AdminClientId;
                    adminBuffer = true;
                    LogBuilder.Append($"{MyLogger.LogInstance[i].UserName}{(GetOption(Toggle.ID_Client) ? $" | {MyLogger.LogInstance[i].ClientID}" : "")}{(GetOption(Toggle.Date) ? $" | {MyLogger.LogInstance[i].TimeStamp.ToString("d:M:y")}" : "")}\n");
                }
            }
            else
            {
                adminBuffer = false;
            }
            if (MyLogger.LogInstance[i].ClientID.HasValue && clientSpamBuffer != MyLogger.LogInstance[i].ClientID.Value)
            {
                clientSpamBuffer = MyLogger.LogInstance[i].ClientID.Value;
                clientDateBuffer = MyLogger.LogInstance[i].TimeStamp;
                LogBuilder.Append($"{MyLogger.LogInstance[i].UserName}{(GetOption(Toggle.ID_Client) ? $" | {MyLogger.LogInstance[i].ClientID}" : "")}{(GetOption(Toggle.Date) ? $" | {MyLogger.LogInstance[i].TimeStamp.ToString("d:M:y")}" : "")}\n");
            }
            if (GetOption(Toggle.Date) && clientDateBuffer.Date != MyLogger.LogInstance[i].TimeStamp.Date)
            {
                LogBuilder.Append($"{MyLogger.LogInstance[i].TimeStamp.ToString("d:M:y")}\n");
            }
            LogBuilder.Append($"    {(GetOption(Toggle.Time) ? $"{MyLogger.LogInstance[i].TimeStamp.ToString("hh:mm:ss")} " : "")}- {MyLogger.LogInstance[i].Message}\n");
        }
    }

    public void ClearRegistry()
    {
        if (MyLogger == null)
            return;

        MyLogger.ClearRegistry();
    }

    public void TestReadFile()
    {
        if (MyLogger == null || LogScreen == null)
            return;

        LogScreen.text = MyLogger.TestReadFile();
    }
    public void TestWriteFile()
    {
        if (MyLogger == null || LogInput == null)
            return;

        MyLogger.TestWriteFile(LogInput.text);
    }
    public void TestBuild()
    {
        if (MyLogger == null)
            return;

        MyLogger.BuildTestElementTree();
    }
    
    public void FullServerSave()
    {
        if (MyLogger == null)
            return;

        MyLogger.SaveFullServer();
    }
    public void FullServerLoad()
    {
        if (MyLogger == null)
            return;

        MyLogger.LoadFullServer();
    }

    public void Login_Register()
    {
        if (LoginNameInput == null || LoginPasswordInput == null)
            return;

        MyLogger.SendLoginUserRequest(LoginNameInput.text, LoginPasswordInput.text);
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

        MyLogger.ChatMessage(LogInput.text);
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

    public void Poosh()
    {
        if (MyLogger != null)
            MyLogger.foo2();
    }

    void UpdateName()
    {
        if (NameLabel != null)
            NameLabel.text = MyLogger != null && MyLogger.IsSpawned ? MyLogger.IsHandled ? $"<< [Online] {MyLogger.MyName} >>" :
                "<< Awaiting Handle from server... >>>" :
                $"<< [Offline] {(LocalName == null ? "[No Local Name]" : LocalName)} >>";
    }
    void UpdateLoginState()
    {
        //Debug.Log("Updating Login State...");
        //LoginState oldLoginState = _loginState_cache;
        if (!MyLogger.IsSpawned)
        {
            LoginState = LoginState.Disconnected;
        }
        else if (MyLogger.IsAdmin)
        {
            LoginState = LoginState.Admin;
        }
        else if (!MyLogger.IsHandled)
        {
            LoginState = LoginState.UnHandled;
        }
        else if (MyLogger.IsGuest)
        {
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
            LoginTitle.text = MyLogger.IsSpawned ? MyLogger.IsAdmin ? $"This is your server: {MyLogger.ServerInstance.ServerName}" : $"Login to: {MyLogger.ServerInstance.ServerName}" : "Not connected to server";

        if (LoginStatus != null)
            LoginStatus.text = MyLogger.IsSpawned ? MyLogger.IsAdmin ? $"You do not need to be here..." : "Please provide your loginName and passWord for this server..." : "Please connect to a server first before logging in..."; 

        if (LogOutButton != null)
            LogOutButton.SetActive(MyLogger.IsSpawned && !MyLogger.IsGuest && !MyLogger.IsAdmin);

        if (Login_RegisterButton != null)
            Login_RegisterButton.SetActive(MyLogger.IsSpawned && !MyLogger.IsAdmin);
    }
    void UpdateMemberPanel()
    {

    }
    void SetupMemberPanel()
    {
        
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
            MasterPanel != null &&
            MemberPanel != null)// &&
            //MyLog != null)
        {
            RectTransform main = (RectTransform)MainPanel.transform;
            RectTransform master = (RectTransform)MasterPanel.transform;
            RectTransform member = (RectTransform)MemberPanel.transform;

            if (!MasterPanel.activeSelf &&
            MasterOptionsActive)
            {
                MasterPanel.SetActive(true);
                main.offsetMin = new Vector2(main.offsetMin.x + master.sizeDelta.x, main.offsetMin.y);
            }
                

            if (MasterPanel.activeSelf &&
            !MasterOptionsActive)
            {
                MasterPanel.SetActive(false);
                main.offsetMin = new Vector2(main.offsetMin.x - master.sizeDelta.x, main.offsetMin.y);
            }

            if (!MemberPanel.activeSelf &&
            MemberPanelActive)
            {
                MemberPanel.SetActive(true);
                main.offsetMax = new Vector2(main.offsetMax.x - member.sizeDelta.x, main.offsetMax.y);
            }


            if (MemberPanel.activeSelf &&
            !MemberPanelActive)
            {
                MemberPanel.SetActive(false);
                main.offsetMax = new Vector2(main.offsetMax.x + member.sizeDelta.x, main.offsetMax.y);
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
        if (MyLogger != null)
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
