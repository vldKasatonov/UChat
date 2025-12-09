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
    CreateChat,
    SearchUser,
    GetChats,
    GetHistory
}

public enum Status
{
    Success,
    Error
}