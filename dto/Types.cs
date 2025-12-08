namespace dto;

public enum CommandType
{
    Login,
    Register,
    SendMessage,
    DeleteForMe,
    DeleteForAll,
    Reconnect,
    CreateChat
}

public enum Status
{
    Success,
    Error
}