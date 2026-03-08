using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Domain.Entities;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly SaleAppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthenticationService(SaleAppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<SignUpResponse> SignUpAsync(SignUpRequest request)
    {
        try
        {
            // Check if username already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (existingUser != null)
            {
                return new SignUpResponse
                {
                    Success = false,
                    Message = "Username already exists"
                };
            }

            // Check if email already exists (if provided)
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingEmail != null)
                {
                    return new SignUpResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }
            }

            // Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user
            var newUser = new User
            {
                Username = request.Username,
                Password = hashedPassword,
                Email = request.Email ?? string.Empty,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return new SignUpResponse
            {
                Success = true,
                Message = "User registered successfully",
                UserId = newUser.UserId
            };
        }
        catch (Exception ex)
        {
            return new SignUpResponse
            {
                Success = false,
                Message = $"Error during registration: {ex.Message}"
            };
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            // Find user by username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "User account is inactive"
                };
            }

            // Generate JWT token
            var token = await GenerateJwtToken(user);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            return new LoginResponse
            {
                Success = false,
                Message = $"Error during login: {ex.Message}"
            };
        }
    }

    private async Task<string> GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var issuer = jwtSettings["Issuer"] ?? "SaleApp";
        var audience = jwtSettings["Audience"] ?? "SaleAppUsers";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        // Load user roles from database
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.RoleName)
            .ToListAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Add role claims
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
