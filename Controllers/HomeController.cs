using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using events_admin.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace events_admin.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _clientFactory;

    public HomeController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }
    
    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    // 1. Cargar la vista visual del Login
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home"); // Si ya está logueado, al dashboard
        }

        return View();
    }

    // 2. Procesar el formulario enviado por el usuario
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Por favor, completa todos los campos.";
            return View();
        }

        try
        {
            var client = _clientFactory.CreateClient();

            // Petición POST con el formato JSON que tu API espera
            var loginData = new { correo = email, contrasena = password };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            // URL real de tu API en C#
            var response = await client.PostAsync("https://service.quasar.andrescortes.dev/api/auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Credenciales incorrectas o usuario inactivo.";
                return View();
            }

            // Leer respuesta de la API (AuthResponseDto)
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            string tokenString = root.GetProperty("token").GetString()!;

            // 3. Decodificar el JWT en el servidor para leer el Rol y Nombre
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            // Extraer los claims estándares de .NET
            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://xmlsoap.org")?.Value;
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://microsoft.com")?.Value;
            var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://xmlsoap.orgidentifier")?.Value;

            // 4. Crear la identidad local del contenedor MVC
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nameClaim ?? email),
                new Claim(ClaimTypes.Role, roleClaim ?? "Staff"),
                new Claim(ClaimTypes.NameIdentifier, idClaim ?? ""),
                new Claim("JWToken",
                    tokenString) // Guardamos el JWT crudo para enviarlo luego en las cabeceras de los servicios
            };

            var claimsIdentity = new ClaimsIdentity(claims, "TeatrosCookieAuth");
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            // Iniciar sesión en el navegador (Inyecta la Cookie cifrada)
            await HttpContext.SignInAsync("TeatrosCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception)
        {
            ViewBag.Error = "Error de comunicación con el servidor de autenticación.";
            return View();
        }
    }

    // 3. Endpoint para cerrar sesión
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("TeatrosCookieAuth");
        return RedirectToAction("Login");
    }
}