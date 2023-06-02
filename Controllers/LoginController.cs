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
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("Login", connection))
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] hashedBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                    string hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", string.Empty);

                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@usuario", username);
                    command.Parameters.AddWithValue("@pin", hashedPassword);

                    int count = (int)command.ExecuteScalar();

                    return count > 0;
                }

            }
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
                string token = "UDEMApp2023";
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    // Hash password
                    byte[] hashedBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(login?.pin ?? ""));
                    string hashedPassword = BitConverter.ToString(hashedBytes).Replace("-", string.Empty);

                    string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlCommand command = new SqlCommand("GetEmployeeInformation", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("@usuario", login?.user);
                            command.Parameters.AddWithValue("@pin", hashedPassword);

                            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                            {
                                DataTable dt = new DataTable();
                                adapter.Fill(dt);

                                return Ok(new
                                {
                                    token,
                                    nomina = dt.Rows[0]["Nómina"],
                                    nombre = dt.Rows[0]["Nombre_Empleado"],
                                    idRol = dt.Rows[0]["idRol"],
                                    idDepartamento = dt.Rows[0]["idDepartamento"],
                                    idEscuela = dt.Rows[0]["idEscuela"],
                                    idVicerrectoria = dt.Rows[0]["idVicerrectoria"]
                                });
                            }
                        }
                    }
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