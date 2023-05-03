using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class RepositionsController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public RepositionsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    // --- FUNCTION THAT GETS ALL REPOSITION REPORTS (PENDING OR ACCEPTED) ---
    private string GetRepositionReports(bool pending)
    {
        // Get all reposition reports (pending/accepted)
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT  Nombre_Empleado,
                Nómina,
                idReposición, 
                Materia, 
                CONCAT(TRIM(Cursos.Subject), '-', Cursos.CVE_Materia, '-', Cursos.Grupo) AS 'CVE_Materia', 
                FechaReposicion, 
                Reposiciones.Hora_Inicio, 
                Reposiciones.Salón, 
                Número_Evento, 
                Reposiciones.idHorario, 
                Reposiciones.idCódigo  
        FROM Reposiciones
            JOIN Horarios ON Reposiciones.idHorario=Horarios.idHorario
            JOIN Cursos ON (
                Cursos.CRN=Horarios.CRN
                AND Cursos.Subject=Horarios.Subject
                AND Cursos.CVE_Materia=Horarios.CVE_Materia
                AND Cursos.Grupo=Horarios.Grupo
                AND Cursos.Salón=Horarios.Salón)
            JOIN Empleados ON Cursos.Nómina_Empleado = Empleados.Nómina
            JOIN Materias ON (Cursos.CVE_Materia=CVE AND Cursos.Subject=Materias.Subject)
        WHERE Reposiciones.Salón IS " + (pending ? "NULL" : "NOT NULL") + " ORDER BY FechaReposicion DESC", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Create list of all reposition reports
        List<RepositionTable> repositions = new List<RepositionTable>();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                // Add info of a reposition to repositions list
                RepositionTable r = new RepositionTable();
                r.employeeName = Convert.ToString(dt.Rows[i]["Nombre_Empleado"]);
                r.nomina = Convert.ToInt32(dt.Rows[i]["Nómina"]);
                r.idReposition = Convert.ToInt32(dt.Rows[i]["idReposición"]);
                r.subjectName = Convert.ToString(dt.Rows[i]["Materia"]);
                r.subject_CVE = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                r.date = Convert.ToDateTime(dt.Rows[i]["FechaReposicion"]);
                r.startTime = Convert.ToString(dt.Rows[i]["Hora_Inicio"]);
                r.classroom = pending ? null : Convert.ToString(dt.Rows[i]["Salón"]);
                r.eventNum = pending ? null : Convert.ToInt32(dt.Rows[i]["Número_Evento"]);
                r.idSchedule = Convert.ToInt32(dt.Rows[i]["idHorario"]);
                r.idCode = Convert.ToInt32(dt.Rows[i]["idCódigo"]);

                repositions.Add(r);
            }

            return JsonConvert.SerializeObject(repositions);
        }
        // The are no reposition reports submitted by the professor
        else
            return "";
    }


    // --- API ROUTE: GET ALL REPOSITION REPORTS FROM A PROFESSOR ---
    [HttpGet]
    [Route("Professor/RepositionReports/{nomina}")]
    public string GetRepositionReports(int nomina)
    {
        // Get all reposition reports from the professor
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT  idReposición, 
                Materia, 
                CONCAT(TRIM(Cursos.Subject), '-', Cursos.CVE_Materia, '-', Cursos.Grupo) AS 'CVE_Materia', 
                FechaReposicion, 
                Reposiciones.Hora_Inicio, 
                Reposiciones.Salón, 
                Número_Evento, 
                Reposiciones.idHorario, 
                Reposiciones.idCódigo  
        FROM Reposiciones
            JOIN Horarios ON Reposiciones.idHorario=Horarios.idHorario
            JOIN Cursos ON (
                Cursos.CRN=Horarios.CRN
                AND Cursos.Subject=Horarios.Subject
                AND Cursos.CVE_Materia=Horarios.CVE_Materia
                AND Cursos.Grupo=Horarios.Grupo
                AND Cursos.Salón=Horarios.Salón)
            JOIN Materias ON (Cursos.CVE_Materia=CVE AND Cursos.Subject=Materias.Subject)
        WHERE Nómina_Empleado=" + nomina + " ORDER BY FechaReposicion DESC", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Create list of all reposition reports
        List<RepositionTable> repositions = new List<RepositionTable>();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                // Add info of an reposition to repositions list
                RepositionTable r = new RepositionTable();
                r.idReposition = Convert.ToInt32(dt.Rows[i]["idReposición"]);
                r.subjectName = Convert.ToString(dt.Rows[i]["Materia"]);
                r.subject_CVE = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                r.date = Convert.ToDateTime(dt.Rows[i]["FechaReposicion"]);
                r.startTime = Convert.ToString(dt.Rows[i]["Hora_Inicio"]);
                r.classroom = Convert.ToString(dt.Rows[i]["Salón"]);
                r.eventNum = dt.Rows[i]["Número_Evento"] != DBNull.Value ? Convert.ToInt32(dt.Rows[i]["Número_Evento"]) : null;
                r.idSchedule = Convert.ToInt32(dt.Rows[i]["idHorario"]);
                r.idCode = Convert.ToInt32(dt.Rows[i]["idCódigo"]);

                repositions.Add(r);
            }

            return JsonConvert.SerializeObject(repositions);
        }
        // The are no reposition reports submitted by the professor
        else
            return "";
    }


    // --- API ROUTE: CREATE NEW REPOSITION REPORT ---
    [HttpPost]
    [Route("CreateRepositionReport")]
    public IActionResult CreateReposition(RepositionModel reposition)
    {
        if (ModelState.IsValid)
        {
            // Register reposition report in database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            string sqlFormattedDate = reposition.date.HasValue ? reposition.date.Value.ToString("yyyyMMdd") : "";
            SqlCommand cmd = new SqlCommand(@"INSERT INTO Reposiciones (FechaReposicion, Hora_Inicio, idHorario, idCódigo) 
            VALUES ('" + sqlFormattedDate + "', '" + reposition.startTime + "', " + reposition.idSchedule + ", " + reposition.idCode + ")", con);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
            // Return confirmation message
            return Ok(new { message = "Reposición registrada correctamente." });
        }
        else
            return BadRequest(ModelState);
    }


    // --- API ROUTE: CREATE NEW EXTERNAL UNIT REPORT ---
    [HttpPost]
    [Route("CreateExternalUnitReport")]
    public IActionResult CreateExternalUnit(ExternalUnitModel externalUnit)
    {
        if (ModelState.IsValid)
        {
            string sqlFormattedDate = externalUnit.date.HasValue ? externalUnit.date.Value.ToString("yyyyMMdd") : "";

            // Register external unit report in database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlCommand cmd = new SqlCommand(@"INSERT INTO Reposiciones (FechaReposicion, Hora_Inicio, idHorario, idCódigo, Salón, Número_Evento) 
            VALUES ('" + sqlFormattedDate + "', '" + externalUnit.startTime + "', " + externalUnit.idSchedule + ", 6, '" + externalUnit.classroom + "', -1)", con);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            // Register attendance in database
            con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            cmd = new SqlCommand("INSERT INTO Asistencia VALUES (" + externalUnit.idSchedule + ", '" + sqlFormattedDate + "', 6)", con);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            // Return confirmation message
            return Ok(new { message = "Unidad externa registrada correctamente." });
        }
        else
            return BadRequest(ModelState);
    }


    // --- API ROUTE: GET CLASSES FROM PROFESSOR ---
    [HttpGet]
    [Route("Professor/GetClasses/{nomina}")]
    public string GetClasses(int nomina)
    {
        // Get all classes that the professor teaches
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT idHorario, CONCAT(CONCAT(TRIM(Cursos.Subject), '-', Cursos.CVE_Materia, '-', Cursos.Grupo), ' - ', Materia) AS 'Clase'
        FROM Cursos JOIN Horarios ON (
                Cursos.CRN=Horarios.CRN
                AND Cursos.Subject=Horarios.Subject
                AND Cursos.CVE_Materia=Horarios.CVE_Materia
                AND Cursos.Grupo=Horarios.Grupo
                AND Cursos.Salón=Horarios.Salón)
            JOIN Materias ON (Cursos.CVE_Materia=CVE AND Cursos.Subject=Materias.Subject)
        WHERE Nómina_Empleado=" + nomina, con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Create list of all class options
        List<ClassSelect> classes = new List<ClassSelect>();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                // Add info of a class to classes list
                ClassSelect c = new ClassSelect();
                c.idSchedule = Convert.ToInt32(dt.Rows[i]["idHorario"]);
                c.classOpt = Convert.ToString(dt.Rows[i]["Clase"]);

                classes.Add(c);
            }

            return JsonConvert.SerializeObject(classes);
        }
        // The professor has no classes
        else
            return "";
    }


    // --- API ROUTE: GET ALL PENDING REPOSITION REPORTS ---
    [HttpGet]
    [Route("Admin/GetPendingReposition")]
    public string GetPendingReposition()
    {
        return GetRepositionReports(true);
    }


    // --- API ROUTE: GET ALL ACCEPTED REPOSITION REPORTS ---
    [HttpGet]
    [Route("Admin/GetAcceptedReposition")]
    public string GetAcceptedReposition()
    {
        return GetRepositionReports(false);
    }


    // --- API ROUTE: ASSIGN CLASSROOM AND EVENT NUMBER TO A REPOSITION ---
    [HttpPut]
    [Route("AssignClassroomEvent")]
    public IActionResult AssignClassroomEvent(RepositionClassroom reposition)
    {
        if (ModelState.IsValid)
        {
            // Register classroom in database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlCommand cmd = new SqlCommand("UPDATE Reposiciones SET Salón='" + reposition.classroom + "', Número_Evento='" + reposition.numEvent + "' WHERE idReposición=" + reposition.idReposition, con);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
            // Return confirmation message
            return Ok(new { message = "Reposición actualizada correctamente." });
        }
        else
            return BadRequest(ModelState);
    }


    // --- API ROUTE: REGISTER ATTENDANCE FOR A REPOSITION ---
    [HttpPost]
    [Route("RegisterRepositionAttendance")]
    public IActionResult RegisterRepositionAttendance(RepositionAttendance reposition)
    {
        if (ModelState.IsValid)
        {
            // Obtain reposition data from database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Reposiciones WHERE idReposición=" + reposition.idReposition, con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            int idSchedule = Convert.ToInt32(dt.Rows[0]["idHorario"]);
            DateTime repositionDate = Convert.ToDateTime(dt.Rows[0]["FechaReposicion"]);
            TimeSpan startHour = TimeSpan.Parse(dt.Rows[0]["Hora_Inicio"].ToString() ?? "00:00");

            // Check if attendance is not registered yet
            con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            da = new SqlDataAdapter("SELECT COUNT(*) AS Conteo FROM Asistencia WHERE idHorario=" + idSchedule + " AND Fecha='" + repositionDate.ToString("yyyyMMdd") + "'", con);
            dt = new DataTable();
            da.Fill(dt);

            if ((int)dt.Rows[0]["Conteo"] == 0)
            {
                // Check that the reposition is in the correct date
                if (repositionDate.Date == DateTime.Now.Date)
                {
                    // Check that the reposition is in the correct hour
                    TimeSpan currentTime = DateTime.Now.TimeOfDay;
                    TimeSpan diff = currentTime - startHour;

                    // Reposition is on time
                    if (Math.Abs(diff.TotalMinutes) <= 10)
                    {
                        // Register attendance in database
                        con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                        SqlCommand cmd = new SqlCommand("INSERT INTO Asistencia VALUES (" + idSchedule + ", '" + repositionDate.ToString("yyyyMMdd") + "', " + reposition.code + ")", con);
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                        return Ok(new { message = "Asistencia registrada correctamente." });
                    }
                    // Reposition is not on time
                    else
                        return Ok(new { message = "La hora actual no coincide con la de la reposición. O bien, ha intentado registrar su asistencia muy tarde." });
                }
                // Reposition is in another date
                else
                    return Ok(new { message = "La fecha actual no coincide con la de la reposición." });
            }
            // Attendance is already registered
            else
                return Ok(new { message = "Ya se registró la asistencia para esta reposición." });
        }
        else
            return BadRequest(ModelState);
    }
}