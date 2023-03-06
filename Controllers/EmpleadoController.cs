using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class EmpleadoController : ControllerBase {
    public readonly IConfiguration _configuration;

    public EmpleadoController(IConfiguration configuration) {
        _configuration = configuration;
    }

    [HttpGet(Name = "GetEmpleados")]
    public string GetEmpleados() {
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Empleados", con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        List<Empleado> EmpleadoList = new List<Empleado>();
        Response r = new Response();
        if (dt.Rows.Count > 0) {
            for (int i=0; i<dt.Rows.Count; i++) {
                Empleado p = new Empleado();
                p.nomina = Convert.ToString(dt.Rows[i]["Nómina"]);
                p.nombre = Convert.ToString(dt.Rows[i]["Nombre_Empleado"]);
                p.idRol = Convert.ToString(dt.Rows[i]["idRol"]);
                EmpleadoList.Add(p);
            }
        }
        if(EmpleadoList.Count > 0) {
            return JsonConvert.SerializeObject(EmpleadoList);
        }
        else {
            r.statusCode = 100;
            r.errorMessage = "No data found";
            return JsonConvert.SerializeObject(r);
        }
    }
}