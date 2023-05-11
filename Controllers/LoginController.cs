using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using integrador_back.Models;
using System.Security.Cryptography;
using System.Text;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public LoginController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // --- FUNCTION THAT CHECKS IF A USER AND PASSWORD IS VALID (EXISTS IN DATABASE) ---
    private bool IsValidUser(string username, string password)
    {
        string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
        string query = "SELECT COUNT(*) FROM Usuarios WHERE Usuario = @Username AND Pin = @Password";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hashedBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                string hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", string.Empty);
                command.Parameters.AddWithValue("@Password", hashedPassword);
            }

            connection.Open();
            int count = (int)command.ExecuteScalar();
            connection.Close();

            return count > 0;
        }
    }

    // --- API ROUTE: CHECK USER CREDENTIALS AND PERFORM LOGIN ---
    [HttpPost(Name = "Login")]
    public IActionResult Login(Login login)
    {
        if (ModelState.IsValid)
        {
            // Check user credentials and perform login
            if (IsValidUser(login?.user ?? "", login?.pin ?? ""))
            {
                // Generate and return authentication token if login successful
                string token = "HOLA";
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    // Hash password
                    byte[] hashedBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(login?.pin ?? ""));
                    string hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", string.Empty);
                    // Search for user info to store in sessionStorage
                    SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                    SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Empleados JOIN Usuarios ON Nómina=Nómina_Empleado WHERE Usuario ='" + login?.user + "' AND Pin = '" + hashedPassword + "'", con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return Ok(new { token, nomina = dt.Rows[0]["Nómina"], nombre = dt.Rows[0]["Nombre_Empleado"], idRol = dt.Rows[0]["idRol"], idDepartamento = dt.Rows[0]["idDepartamento"], idEscuela = dt.Rows[0]["idEscuela"] });
                }
            }
            else
            {
                // Return error message if login failed
                return BadRequest(new { error = "Invalid username or password" });
            }
        }

        // Return error message if model state is invalid
        return BadRequest(ModelState);
    }
}