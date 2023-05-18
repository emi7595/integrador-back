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
        string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("GetRepositionReports", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@pending", pending);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

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
            }
        }
    }


    // --- API ROUTE: GET ALL REPOSITION REPORTS FROM A PROFESSOR ---
    [HttpGet]
    [Route("Professor/RepositionReports/{nomina}")]
    public string GetRepositionReports(int nomina)
    {
        // Get all reposition reports from the professor
        string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("GetRepositionReportsProfessor", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@nomina", nomina);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

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
            }
        }
    }


    // --- API ROUTE: CREATE NEW REPOSITION REPORT ---
    [HttpPost]
    [Route("CreateRepositionReport")]
    public IActionResult CreateReposition(RepositionModel reposition)
    {
        if (ModelState.IsValid)
        {
            // Register reposition report in database
            string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
            string sqlFormattedDate = reposition.date.HasValue ? reposition.date.Value.ToString("yyyyMMdd") : "";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("InsertRepositionReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@fecha", sqlFormattedDate);
                    command.Parameters.AddWithValue("@startTime", reposition.startTime);
                    command.Parameters.AddWithValue("@idSchedule", reposition.idSchedule);
                    command.Parameters.AddWithValue("@idCode", reposition.idCode);

                    command.ExecuteNonQuery();

                    // Return confirmation message
                    return Ok(new { message = "Reposición registrada correctamente." });
                }
            }
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
            string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("InsertExternalUnitReport", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@date", sqlFormattedDate);
                    command.Parameters.AddWithValue("@startTime", externalUnit.startTime);
                    command.Parameters.AddWithValue("@idSchedule", externalUnit.idSchedule);
                    command.Parameters.AddWithValue("@classroom", externalUnit.classroom);

                    command.ExecuteNonQuery();

                    // Register attendance in database
                    using (SqlCommand cmd = new SqlCommand("InsertAttendance"))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@idHorario", externalUnit.idSchedule);
                        cmd.Parameters.AddWithValue("@fecha", sqlFormattedDate);
                        cmd.Parameters.AddWithValue("@idCódigo", 6);

                        cmd.ExecuteNonQuery();

                        // Return confirmation message
                        return Ok(new { message = "Unidad externa registrada correctamente." });
                    }
                }
            }
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
        string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("GetClassesSelect", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@nomina", nomina);

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

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
            }
        }
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
            string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("AssignClassroomEvent", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@classroom", reposition.classroom);
                    command.Parameters.AddWithValue("@event", reposition.numEvent);
                    command.Parameters.AddWithValue("@idReposition", reposition.idReposition);

                    command.ExecuteNonQuery();

                    // Return confirmation message
                    return Ok(new { message = "Reposición actualizada correctamente." });
                }
            }
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
            string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("GetRepositionInformation", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@idReposition", reposition.idReposition);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        int idSchedule = Convert.ToInt32(dt.Rows[0]["idHorario"]);
                        DateTime repositionDate = Convert.ToDateTime(dt.Rows[0]["FechaReposicion"]);
                        TimeSpan startHour = TimeSpan.Parse(dt.Rows[0]["Hora_Inicio"].ToString() ?? "00:00");

                        // Check if attendance is not registered yet
                        using (SqlCommand cmd = new SqlCommand("CheckAttendanceDay", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@idHorario", idSchedule);
                            cmd.Parameters.AddWithValue("@fecha", repositionDate.ToString("yyyyMMdd"));

                            using (SqlDataAdapter adapter2 = new SqlDataAdapter(cmd))
                            {
                                DataTable dt2 = new DataTable();
                                adapter2.Fill(dt2);

                                if ((int)dt2.Rows[0]["Conteo"] == 0)
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
                                            using (SqlCommand cmd2 = new SqlCommand("InsertAttendance", connection))
                                            {
                                                cmd2.CommandType = CommandType.StoredProcedure;
                                                cmd2.Parameters.AddWithValue("@idHorario", idSchedule);
                                                cmd2.Parameters.AddWithValue("@fecha", repositionDate.ToString("yyyyMMdd"));
                                                cmd2.Parameters.AddWithValue("@idCódigo", reposition.idCode);

                                                cmd2.ExecuteNonQuery();

                                                return Ok(new { message = "Asistencia registrada correctamente." });
                                            }
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
                        }
                    }
                }
            }
        }
        else
            return BadRequest(ModelState);
    }
}