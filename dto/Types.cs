namespace dto;

public enum CommandType
{
    Login,
    Register,
    SendMessage,
    DeleteForMe,
    DeleteForAll,
    Reconnect
}

public enum Status
{
    Success,
    Error
}