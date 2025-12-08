namespace dto;

public enum CommandType
{
    Login,
    Register,
    SendMessage,
    DeleteForMe,
    DeleteForAll,
    Reconnect,
    EditMessage,
    CreateChat
}

public enum Status
{
    Success,
    Error
}