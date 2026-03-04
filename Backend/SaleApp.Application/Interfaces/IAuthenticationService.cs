using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface IAuthenticationService
{
    Task<SignUpResponse> SignUpAsync(SignUpRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
