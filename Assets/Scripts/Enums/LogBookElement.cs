
/*public enum Token
{
    //LogSeparator = '&',
    CodonOpen = '<',
    CodonClose = '>',
    Label = '@',
    Value = '#',
    Terminate = '/',
    ValueSplit = ':',
}*/

/*
 
<@Log>
	<#UserName>Torqk<\UserName>
	<#TimeStamp>1000<\TimeStamp>
	<#ID_User>123<\ID_User>
	<#ID_Client>456<\ID_Client>
	<#Message>Hello World!<\Message>
<\Log>

<@Log>
	<@UserName>
		<#>Torqk<\>
	<\UserName>
	<@TimeStamp>
		<#>1000<\>
	<\TimeStamp>
	<@IDs>
		<#>123<:
		>456<:
		>789<\>
	<\IDs>
	<@Message>
		<#>Hello World!<\>
	<\Message>
<\Log>
 
 */

/*public static readonly Dictionary<string, char> Tokens = new Dictionary<string, char>()
    {
        { "CodonOpen"   , '<' },
        { "CodonClose"  , '>' },
        { "Label"       , '@' },
        { "Value"       , '#' },
        { "Terminate"   , '/' },
        { "ValueSplit"  , ':' },
    };*/

public enum LogBookElement
{
    Registry,
    Book,
    Server,
    Log,
    Response,
    Registration,
    Credential,
    TimeStamp,
    Password,
    Message,
    Name,
    Code_Response,
    
    Profile_User,
    Profile_Server,

    ID_User,
    ID_Client,
    ID_Server,
}
