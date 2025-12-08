namespace dto;

public enum CommandType
{
    Login,
    Register,
    SendMessage,
    DeleteForMe,
    DeleteForAll,
    EditMessage,
    Reconnect
}

public enum Status
{
    Success,
    Error
}