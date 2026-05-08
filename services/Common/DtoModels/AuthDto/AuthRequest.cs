namespace Common.DtoModels.AuthDto;

public class AuthRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}