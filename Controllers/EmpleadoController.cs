using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class EmpleadoController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public EmpleadoController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // --- API ROUTE: GET ALL EMPLOYEES FROM DATABASE (ONLY FOR TESTING PURPOSES) ---
    [HttpGet(Name = "GetEmpleados")]
    public string GetEmpleados()
    {
        // Connection with database and get data from query
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Empleados", con); // Query
        DataTable dt = new DataTable();
        da.Fill(dt);
        // Fill database data into a list of employees
        List<Empleado> EmpleadoList = new List<Empleado>();
        Response r = new Response();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Empleado p = new Empleado();
                p.nomina = Convert.ToString(dt.Rows[i]["Nómina"]);
                p.nombre = Convert.ToString(dt.Rows[i]["Nombre_Empleado"]);
                p.idRol = Convert.ToString(dt.Rows[i]["idRol"]);
                EmpleadoList.Add(p);
            }
        }
        // Return employee list
        if (EmpleadoList.Count > 0)
        {
            return JsonConvert.SerializeObject(EmpleadoList);
        }
        // Return empty response if there are not employees in database
        else
        {
            r.statusCode = 100;
            r.errorMessage = "No data found";
            return JsonConvert.SerializeObject(r);
        }
    }
}