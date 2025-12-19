namespace Common.DtoModels.AuthDto;

public class AuthResponse
{
    public string AccessToken { get; set; } = default!;
    public string TokenType { get; set; } = default!;
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }
    public string? IdToken { get; set; }
}