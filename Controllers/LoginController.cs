using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase {
    public readonly IConfiguration _configuration;

    public LoginController(IConfiguration configuration) {
        _configuration = configuration;
    }

    private bool IsValidUser(string username, string password)
    {
        string connectionString = _configuration.GetConnectionString("UDEMAppCon").ToString();
        string query = "SELECT COUNT(*) FROM Usuarios WHERE Usuario = @Username AND Pin = @Password";
        
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password);
            
            connection.Open();
            int count = (int)command.ExecuteScalar();
            connection.Close();
            
            return count > 0;
        }
    }  

    // [HttpPost(Name = "Login")]
    // public string Login(Login login) {
    //     SqlConnection con = new SqlConnection(_configuration.GetConnectionString("UDEMAppCon").ToString());
    //     SqlDataAdapter da = new SqlDataAdapter("SELECT Rol FROM Profesor JOIN Login ON Nomina_Profesor=Nomina WHERE Login='" + login.user + "' AND Pin=" + login.pin, con);
    //     DataTable dt = new DataTable();
    //     da.Fill(dt);
    //     Response r = new Response();
    //     if (dt.Rows.Count > 0) {
    //         return Convert.ToString(dt.Rows[0]["Rol"]);
    //     }
    //     else {
    //         r.statusCode = 100;
    //         r.errorMessage = "Inicio de sesión incorrecto";
    //         return JsonConvert.SerializeObject(r);
    //     }
    // }


    [HttpPost(Name = "Login")]
    public IActionResult Login(Login login)
    {
        if (ModelState.IsValid)
        {
            // Check user credentials and perform login
            if (IsValidUser(login.user, login.pin))
            {
                // Generate and return authentication token if login successful
                string token = "HOLA";
                // Search for user info to store in sessionStorage
                SqlConnection con = new SqlConnection(_configuration.GetConnectionString("UDEMAppCon").ToString());
                SqlDataAdapter da = new SqlDataAdapter("SELECT Nómina, Nombre_Empleado, idRol FROM Empleados JOIN Usuarios ON Nómina=Nómina_Empleado WHERE Usuario ='" + login.user + "' AND Pin = '" + login.pin + "'", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return Ok(new { token, nomina = dt.Rows[0]["Nómina"], nombre = dt.Rows[0]["Nombre_Empleado"], idRol = dt.Rows[0]["idRol"] });
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


    // [HttpGet(Name = "GetLogins")]
    // public string GetLogins() {
    //     SqlConnection con = new SqlConnection(_configuration.GetConnectionString("UDEMAppCon").ToString());
    //     SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Logins", con);
    //     DataTable dt = new DataTable();
    //     da.Fill(dt);
    //     List<Login> LoginList = new List<Login>();
    //     Response r = new Response();
    //     if (dt.Rows.Count > 0) {
    //         for (int i=0; i<dt.Rows.Count; i++) {
    //             Login p = new Login();
    //             p.user = Convert.ToString(dt.Rows[i]["Login"]);
    //             p.pin = Convert.ToString(dt.Rows[i]["Pin"]);
    //             p.nomina = Convert.ToString(dt.Rows[i]["Nómina_Empleado"]);
    //             LoginList.Add(p);
    //         }
    //     }
    //     if(LoginList.Count > 0) {
    //         return JsonConvert.SerializeObject(LoginList);
    //     }
    //     else {
    //         r.statusCode = 100;
    //         r.errorMessage = "No data found";
    //         return JsonConvert.SerializeObject(r);
    //     }
    // }

}