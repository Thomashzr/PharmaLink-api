using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PharmaLink.DTOs.Auth;
using PharmaLink.Services;
using PharmaLink.Tests.Helpers;

namespace PharmaLink.Tests.Unit;

public class AuthServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();

    public AuthServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "SuperSecretKeyForTestingAuthService2026Minimum32Chars!"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task RegisterAsync_UsuarioNuevo_DebeRegistrarYRetornarToken()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var service = new AuthService(context, _configuration, _loggerMock.Object);
            var dto = new RegistroDTO("nuevoUsuario", "Password123!");

            // Act
            var result = await service.RegisterAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("nuevoUsuario", result.Usuario);
            Assert.Equal("Usuario", result.Rol);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
        }
    }

    [Fact]
    public async Task RegisterAsync_UsuarioExistente_DebeRetornarNull()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var service = new AuthService(context, _configuration, _loggerMock.Object);
            var dto = new RegistroDTO("usuarioDuplicado", "Password123!");
            await service.RegisterAsync(dto);

            // Act
            var result = await service.RegisterAsync(dto);

            // Assert
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task LoginAsync_CredencialesValidas_DebeRetornarToken()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var service = new AuthService(context, _configuration, _loggerMock.Object);
            var registroDto = new RegistroDTO("loginUser", "Password123!");
            await service.RegisterAsync(registroDto);

            var loginDto = new LoginDTO("loginUser", "Password123!");

            // Act
            var result = await service.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("loginUser", result.Usuario);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
        }
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecto_DebeRetornarNull()
    {
        // Arrange
        var (context, connection) = TestDbContextFactory.CreateContext();
        await using (connection)
        await using (context)
        {
            var service = new AuthService(context, _configuration, _loggerMock.Object);
            var registroDto = new RegistroDTO("loginUserBad", "Password123!");
            await service.RegisterAsync(registroDto);

            var loginDto = new LoginDTO("loginUserBad", "ContraseñaEquivocada");

            // Act
            var result = await service.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }
    }
}
