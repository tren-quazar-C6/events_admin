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
    public async Task<IActionResult> Login(string correo, string clave)
    {
        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(clave))
        {
            ViewBag.Error = "Por favor, completa todos los campos.";
            return View();
        }

        try
        {
            var client = _clientFactory.CreateClient();

            // Petición POST con el formato JSON que tu API espera
            var loginData = new { email = correo, password = clave };
            var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");

            // URL real de tu API en C#
            var response = await client.PostAsync("http://localhost:5114/api/auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Credenciales incorrectas o usuario inactivo.";
                return View();
            }

            // Leer respuesta de la API
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // CORRECCIÓN PRINCIPAL: Tu API en Postman devuelve "success", no "isSuccess"
            if (!root.GetProperty("success").GetBoolean())
            {
                ViewBag.Error = "Credenciales incorrectas.";
                return View();
            }

            var dataNode = root.GetProperty("data");
            string tokenString = dataNode.GetProperty("token").GetString()!;

            // Decodificar el JWT en el servidor
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            // Extraer los claims con sus nombres correctos del JWT
            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var idClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            // Crear la identidad local del contenedor MVC
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nameClaim ?? correo),
                new Claim(ClaimTypes.Role, roleClaim ?? "Staff"),
                new Claim(ClaimTypes.NameIdentifier, idClaim ?? ""),
                new Claim("JWToken", tokenString) // Para usarlo en futuras peticiones al API
            };

            var claimsIdentity = new ClaimsIdentity(claims, "TeatrosCookieAuth");
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            // Iniciar sesión en el navegador (Inyecta la Cookie cifrada)
            await HttpContext.SignInAsync("TeatrosCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception e)
        {
            // NOTA: Si sigue fallando, cambia temporalmente esta línea por:
            // ViewBag.Error = $"Error: {e.Message}";
            // Así sabrás exactamente qué línea del try está rompiendo el flujo.
            ViewBag.Error = "Error de comunicación con el servidor de autenticación.";
            return View();
        }
    }


    // 3. Endpoint para cerrar sesión
    [HttpPost]
    [ValidateAntiForgeryToken] // 🔒 Esta anotación valida el token que pusiste en el HTML
    public async Task<IActionResult> Logout()
    {
        // Destruye la cookie y limpia los claims del servidor y navegador
        await HttpContext.SignOutAsync("TeatrosCookieAuth");

        // Redirige a la vista de Login (Asegúrate de que la acción GET se llame 'Login')
        return RedirectToAction("Login", "Home");
    }
    
    [HttpGet]
    public IActionResult AccessDenied()
    {
        // Opción A: Mandarlo a una vista personalizada (Debes crear AccessDenied.cshtml)
        // return View();

        // Opción B: Redirección automática a su página de inicio informando el problema
        TempData["ErrorMessage"] = "No tienes permisos para acceder a esta sección.";
        return RedirectToAction("Index", "Home");
    }

}