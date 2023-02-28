using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfessorController : ControllerBase {
    public readonly IConfiguration _configuration;

    public ProfessorController(IConfiguration configuration) {
        _configuration = configuration;
    }

    [HttpGet(Name = "GetProfessors")]
    public string GetProfessors() {
        SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ProfessorAppCon").ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Profesor", con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        List<Professor> professorList = new List<Professor>();
        Response r = new Response();
        if (dt.Rows.Count > 0) {
            for (int i=0; i<dt.Rows.Count; i++) {
                Professor p = new Professor();
                p.nomina = Convert.ToString(dt.Rows[i]["Nomina"]);
                p.nombre = Convert.ToString(dt.Rows[i]["Nombre_Profesor"]);
                p.rol = Convert.ToString(dt.Rows[i]["Rol"]);
                professorList.Add(p);
            }
        }
        if(professorList.Count > 0) {
            return JsonConvert.SerializeObject(professorList);
        }
        else {
            r.statusCode = 100;
            r.errorMessage = "No data found";
            return JsonConvert.SerializeObject(r);
        }
    }

    [HttpPost(Name = "Login")]
    public string Login(Login login) {
        SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ProfessorAppCon").ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT Rol FROM Profesor JOIN Usuario ON Nomina_Profesor=Nomina WHERE Usuario='" + login.user + "' AND Pin=" + login.pin, con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        Response r = new Response();
        if (dt.Rows.Count > 0) {
            return Convert.ToString(dt.Rows[0]["Rol"]);
        }
        else {
            r.statusCode = 100;
            r.errorMessage = "Inicio de sesión incorrecto";
            return JsonConvert.SerializeObject(r);
        }
    }
}