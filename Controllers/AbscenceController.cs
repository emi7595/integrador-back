using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class AbscenceController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public AbscenceController(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    // --- FUNCTION THAT GETS ALL REPOSITION REPORTS (PENDING OR ACCEPTED) ---
    private string GetRepositionReports(bool pending)
    {
        // Get all abscence reports from the professor
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT  idReposición, Materia, CONCAT(TRIM(Cursos.Subject), '-', Cursos.CVE_Materia, '-', Cursos.Grupo) AS 'CVE_Materia', 
                FechaReposicion, Reposiciones.Hora_Inicio, Reposiciones.Salón, Número_Evento, Reposiciones.idHorario, Reposiciones.idCódigo  
        FROM Reposiciones
            JOIN Horarios ON Reposiciones.idHorario=Horarios.idHorario
            JOIN Cursos ON (
                Cursos.CRN=Horarios.CRN
                AND Cursos.Subject=Horarios.Subject
                AND Cursos.CVE_Materia=Horarios.CVE_Materia
                AND Cursos.Grupo=Horarios.Grupo
                AND Cursos.Salón=Horarios.Salón)
            JOIN Materias ON (Cursos.CVE_Materia=CVE AND Cursos.Subject=Materias.Subject)
        WHERE Reposiciones.Salón IS " + (pending ? "NULL" : "NOT NULL"), con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Create list of all abscence reports
        List<AbscenceTable> abscences = new List<AbscenceTable>();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                // Add info of an abscence to abscences list
                AbscenceTable a = new AbscenceTable();
                a.idReposition = Convert.ToInt32(dt.Rows[i]["idReposición"]);
                a.subjectName = Convert.ToString(dt.Rows[i]["Materia"]);
                a.subject_CVE = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                a.date = Convert.ToDateTime(dt.Rows[i]["FechaReposicion"]);
                a.startTime = Convert.ToString(dt.Rows[i]["Hora_Inicio"]);
                a.classroom = pending ? null : Convert.ToString(dt.Rows[i]["Salón"]);
                a.eventNum = pending ? null : Convert.ToInt32(dt.Rows[i]["Número_Evento"]);
                a.idSchedule = Convert.ToInt32(dt.Rows[i]["idHorario"]);
                a.idCode = Convert.ToInt32(dt.Rows[i]["idCódigo"]);

                abscences.Add(a);
            }

            return JsonConvert.SerializeObject(abscences);
        }
        // The are no abscence reports submitted by the professor
        else
            return "";
    }


    // --- API ROUTE: GET ALL ABSCENCE REPORTS FROM A PROFESSOR ---
    [HttpGet]
    [Route("Professor/AbscenceReports/{nomina}")]
    public string GetPendingAbsence(int nomina)
    {
        // Get all abscence reports from the professor
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
        WHERE Nómina_Empleado=" + nomina, con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Create list of all abscence reports
        List<AbscenceTable> abscences = new List<AbscenceTable>();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                // Add info of an abscence to abscences list
                AbscenceTable a = new AbscenceTable();
                a.idReposition = Convert.ToInt32(dt.Rows[i]["idReposición"]);
                a.subjectName = Convert.ToString(dt.Rows[i]["Materia"]);
                a.subject_CVE = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                a.date = Convert.ToDateTime(dt.Rows[i]["FechaReposicion"]);
                a.startTime = Convert.ToString(dt.Rows[i]["Hora_Inicio"]);
                a.classroom = Convert.ToString(dt.Rows[i]["Salón"]);
                a.eventNum = Convert.ToInt32(dt.Rows[i]["Número_Evento"]);
                a.idSchedule = Convert.ToInt32(dt.Rows[i]["idHorario"]);
                a.idCode = Convert.ToInt32(dt.Rows[i]["idCódigo"]);

                abscences.Add(a);
            }

            return JsonConvert.SerializeObject(abscences);
        }
        // The are no abscence reports submitted by the professor
        else
            return "";
    }


    // --- API ROUTE: CREATE NEW REPOSITION REPORT ---
    [HttpPost]
    [Route("CreateRepositionReport")]
    public IActionResult CreateReposition(AbscenceModel abscence)
    {
        if (ModelState.IsValid)
        {
            // Register reposition report in database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            string sqlFormattedDate = abscence.date.HasValue ? abscence.date.Value.ToString("yyyyMMdd") : "";
            SqlCommand cmd = new SqlCommand(@"INSERT INTO Reposiciones (FechaReposicion, Hora_Inicio, idHorario, idCódigo) 
            VALUES ('" + sqlFormattedDate + "', '" + abscence.startTime + "', " + abscence.idSchedule + ", " + abscence.idCode + ")", con);
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
    public IActionResult CreateExternalUnit(ExternalUnitModel abscence)
    {
        if (ModelState.IsValid)
        {
            string sqlFormattedDate = abscence.date.HasValue ? abscence.date.Value.ToString("yyyyMMdd") : "";

            // Register reposition report in database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlCommand cmd = new SqlCommand(@"INSERT INTO Reposiciones (FechaReposicion, Hora_Inicio, idHorario, idCódigo, Salón, Número_Evento) 
            VALUES ('" + sqlFormattedDate + "', '" + abscence.startTime + "', " + abscence.idSchedule + ", 6, '" + abscence.classroom + "', -1)", con);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            // Register attendance in database
            con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            cmd = new SqlCommand("INSERT INTO Asistencia VALUES (" + abscence.idSchedule + ", '" + sqlFormattedDate + "', 6)", con);
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
        // Get all abscence reports from the professor
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


    // --- API ROUTE: GET ALL PENDING ABSENCE REPORTS ---
    [HttpGet]
    [Route("Admin/GetPendingAbscence")]
    public string GetPendingAbscence()
    {
        return GetRepositionReports(true);
    }


    // --- API ROUTE: GET ALL ACCEPTED ABSENCE REPORTS ---
    [HttpGet]
    [Route("Admin/GetAcceptedAbscence")]
    public string GetAcceptedAbscence()
    {
        return GetRepositionReports(false);
    }


    // --- API ROUTE: ASSIGN CLASSROOM AND EVENT NUMBER TO A REPOSITION ---
    [HttpPut]
    [Route("AssignClassroomEvent")]
    public IActionResult AssignClassroomEvent(AbscenceClassroom abscence)
    {
        if (ModelState.IsValid)
        {
            // Register classroom in database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlCommand cmd = new SqlCommand("UPDATE Reposiciones SET Salón='" + abscence.classroom + "', Número_Evento='" + abscence.numEvent + "' WHERE idReposición=" + abscence.idReposition, con);
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
    public IActionResult RegisterRepositionAttendance(AbscenceAttendance abscence)
    {
        if (ModelState.IsValid)
        {
            // Obtain reposition data from database
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Reposiciones WHERE idReposición=" + abscence.idReposition, con);
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
                        SqlCommand cmd = new SqlCommand("INSERT INTO Asistencia VALUES (" + idSchedule + ", '" + repositionDate.ToString("yyyyMMdd") + "', " + abscence.code + ")", con);
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                        return Ok(new { message = "Asistencia registrada correctamente." });
                    }
                    // Reposition is not on time
                    else
                    {
                        return Ok(new { message = "La hora actual no coincide con la de la reposición. O bien, ha intentado registrar su asistencia muy tarde." });
                    }

                }
                // Reposition is in another date
                else
                {
                    return Ok(new { message = "La fecha actual no coincide con la de la reposición." });
                }
            }
            else
            {
                return Ok(new { message = "Ya se registró la asistencia para esta reposición." });
            }
        }
        else
            return BadRequest(ModelState);
    }
}