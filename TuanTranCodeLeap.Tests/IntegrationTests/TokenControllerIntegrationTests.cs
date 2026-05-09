using System.Net;
using Xunit;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TuanTranCodeLeap.Application.DTOs;

namespace TuanTranCodeLeap.Tests.IntegrationTests;

public class TokenControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TokenControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsUserInfo_ForValidRequest()
    {
        // Arrange
        var request = new RegisterDto
        {
            Email = "test.integration@example.com",
            Password = "Password123!",
            FirstName = "Integration",
            LastName = "Test"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/token/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfoDto>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        userInfo.Should().NotBeNull();
        userInfo!.Email.Should().Be(request.Email);
        userInfo.FirstName.Should().Be(request.FirstName);
        userInfo.LastName.Should().Be(request.LastName);
        userInfo.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task Login_ReturnsAuthResponse_ForValidCredentials()
    {
        // Arrange - register first
        var registerRequest = new RegisterDto
        {
            Email = "login.test@example.com",
            Password = "Password123!",
            FirstName = "Login",
            LastName = "Test"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        var registerResponse = await _client.PostAsync("/api/token/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - login
        var loginRequest = new LoginDto
        {
            Email = "login.test@example.com",
            Password = "Password123!"
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await _client.PostAsync("/api/token/login", loginContent);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await loginResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be(loginRequest.Email);
        authResponse.FirstName.Should().Be("Login");
        authResponse.LastName.Should().Be("Test");
        authResponse.Token.Should().NotBeNullOrEmpty();
        authResponse.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange - register first
        var registerRequest = new RegisterDto
        {
            Email = "login.fail@example.com",
            Password = "Password123!",
            FirstName = "Login",
            LastName = "Fail"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        await _client.PostAsync("/api/token/register", registerContent);

        // Act - login with wrong password
        var loginRequest = new LoginDto
        {
            Email = "login.fail@example.com",
            Password = "WrongPassword123!"
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await _client.PostAsync("/api/token/login", loginContent);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
