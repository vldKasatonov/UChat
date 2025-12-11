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
    UpdatePinStatus,
    LeaveChat,
    UpdateUserStatus
}

public enum Status
{
    Success,
    Error
}