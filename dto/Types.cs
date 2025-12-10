namespace dto;

public enum CommandType
{
    Login,
    Register,
    SendMessage,
    DeleteForAll,
    Reconnect,
    EditMessage,
    CreateChat,
    SearchUser,
    GetChats,
    GetHistory,
    UpdatePinStatus
}

public enum Status
{
    Success,
    Error
}