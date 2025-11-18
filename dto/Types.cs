namespace dto;

using System.Text.Json.Serialization;

public enum CommandType
{
    Authenticate,
}

public enum Status
{
    Success,
    Error
}