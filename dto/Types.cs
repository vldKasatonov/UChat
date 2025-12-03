namespace dto;

public enum CommandType
{
    Authenticate,
    Register,
    SendMessage,
    DeleteForMe,
    DeleteForAll
}

public enum Status
{
    Success,
    Error
}