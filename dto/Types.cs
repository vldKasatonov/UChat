namespace dto;

using System.Text.Json.Serialization;

public enum CommandType
{
    Login,
    Register
}

public enum Status
{
    Success,
    Error
}