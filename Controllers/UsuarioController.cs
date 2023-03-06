using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class UsuarioController : ControllerBase {
    public readonly IConfiguration _configuration;

    public UsuarioController(IConfiguration configuration) {
        _configuration = configuration;
    }

    [HttpGet(Name = "GetUsuarios")]
    public string GetUsuarios() {
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Usuarios", con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        List<Usuario> UsuarioList = new List<Usuario>();
        Response r = new Response();
        if (dt.Rows.Count > 0) {
            for (int i=0; i<dt.Rows.Count; i++) {
                Usuario p = new Usuario();
                p.user = Convert.ToString(dt.Rows[i]["Usuario"]);
                p.pin = Convert.ToString(dt.Rows[i]["Pin"]);
                p.nomina = Convert.ToString(dt.Rows[i]["Nómina_Empleado"]);
                UsuarioList.Add(p);
            }
        }
        if(UsuarioList.Count > 0) {
            return JsonConvert.SerializeObject(UsuarioList);
        }
        else {
            r.statusCode = 100;
            r.errorMessage = "No data found";
            return JsonConvert.SerializeObject(r);
        }
    }

    // [HttpPost(Name = "Login")]
    // public string Login(Login login) {
    //     SqlConnection con = new SqlConnection(_configuration.GetConnectionString("UDEMAppCon").ToString());
    //     SqlDataAdapter da = new SqlDataAdapter("SELECT Rol FROM Profesor JOIN Usuario ON Nomina_Profesor=Nomina WHERE Usuario='" + login.user + "' AND Pin=" + login.pin, con);
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
}