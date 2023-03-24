using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class InformationController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public InformationController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // --- API ROUTE: GET ALL CLASSES THAT A PROFESSOR IS TEACHING ---
    [HttpGet]
    [Route("Professor/GetClasses/{nomina}")]
    public string GetClasses(int nomina)
    {
        // Get all classes associated to the professor
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT Cursos.CRN, CONCAT(TRIM(Cursos.Subject), '-', Cursos.CVE_Materia, '-', Cursos.Grupo) AS 'CVE_Materia', Materia, Cursos.Salón, Hora_Inicio, Hora_Final, S1, M, T, W, R, F, S
        FROM Empleados 
            JOIN Cursos ON Nómina=Nómina_Empleado 
            JOIN Materias ON (CVE=CVE_Materia AND Materias.Subject=Cursos.Subject)
            JOIN Horarios ON (Cursos.CRN=Horarios.CRN AND Cursos.Subject=Horarios.Subject AND Cursos.CVE_Materia=Horarios.CVE_Materia AND Cursos.Grupo=Horarios.Grupo AND Cursos.Salón=Horarios.Salón)
        WHERE Nómina=" + nomina, con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        
        // Create list of all classes
        List<ProfessorClasses> classes = new List<ProfessorClasses>();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                // Add info of a class to classes list
                ProfessorClasses p = new ProfessorClasses();
                p.CRN = Convert.ToString(dt.Rows[i]["CRN"]);
                p.subject_CVE = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                p.subjectName = Convert.ToString(dt.Rows[i]["Materia"]);
                p.classroom = Convert.ToString(dt.Rows[i]["Salón"]);
                p.startTime = (TimeSpan)dt.Rows[i]["Hora_Inicio"];
                p.endTime = (TimeSpan)dt.Rows[i]["Hora_Final"];
                p.S1 = Convert.ToString(dt.Rows[i]["S1"]);
                p.M = Convert.ToString(dt.Rows[i]["M"]);
                p.T = Convert.ToString(dt.Rows[i]["T"]);
                p.W = Convert.ToString(dt.Rows[i]["W"]);
                p.R = Convert.ToString(dt.Rows[i]["R"]);
                p.F = Convert.ToString(dt.Rows[i]["F"]);
                p.S = Convert.ToString(dt.Rows[i]["S"]);

                classes.Add(p);
            }

            return JsonConvert.SerializeObject(classes);
        }
        // The professor has no classes
        else
        {
            return "El profesor no tiene ninguna clase";
        }
    }
}