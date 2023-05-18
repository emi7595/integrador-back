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
        string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("GetProfessorClasses", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@nomina", nomina);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

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
        }
    }


    // --- API ROUTE: GET ALL CLASSES IN UDEM ---
    [HttpGet]
    [Route("Admin/GetClasses")]
    public string GetClasses()
    {
        // Get all classes in UDEM
        string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("GetUDEMClasses", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Create list of all Classes
                    List<Class> classes = new List<Class>();
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            // Add info of a class to classes list
                            Class c = new Class();
                            c.nomina = Convert.ToString(dt.Rows[i]["Nómina"]);
                            c.employeeName = Convert.ToString(dt.Rows[i]["Nombre_Empleado"]);
                            c.subjectName = Convert.ToString(dt.Rows[i]["Materia"]);
                            c.CRN = Convert.ToString(dt.Rows[i]["CRN"]);
                            c.subject_CVE = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                            c.schedule = Convert.ToString(dt.Rows[i]["Horario"]);
                            c.days = Convert.ToString(dt.Rows[i]["Días"]);
                            c.classroom = Convert.ToString(dt.Rows[i]["Salón"]);

                            classes.Add(c);
                        }

                        return JsonConvert.SerializeObject(classes);
                    }
                    // The are no classes
                    else
                    {
                        return "No hay ninguna clase";
                    }
                }
            }
        }
    }


    // --- API ROUTE: SEARCH A SPECIFIC CLASS ---
    [HttpGet]
    [Route("Admin/SearchClass/{term}")]
    public string SearchClass(string term)
    {
        // Search by employee name, CRN, course, employee payroll or classroom
        string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("SearchClass", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@term", term);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Create list of all Classes
                    List<Class> classes = new List<Class>();
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            // Add info of a class to classes list
                            Class c = new Class();
                            c.nomina = Convert.ToString(dt.Rows[i]["Nómina"]);
                            c.employeeName = Convert.ToString(dt.Rows[i]["Nombre_Empleado"]);
                            c.subjectName = Convert.ToString(dt.Rows[i]["Materia"]);
                            c.CRN = Convert.ToString(dt.Rows[i]["CRN"]);
                            c.subject_CVE = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                            c.schedule = Convert.ToString(dt.Rows[i]["Horario"]);
                            c.days = Convert.ToString(dt.Rows[i]["Días"]);
                            c.classroom = Convert.ToString(dt.Rows[i]["Salón"]);

                            classes.Add(c);
                        }

                        return JsonConvert.SerializeObject(classes);
                    }
                    // The are no classes
                    else
                    {
                        return "No hay ninguna clase";
                    }
                }
            }
        }
    }
}