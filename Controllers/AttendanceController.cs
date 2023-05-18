using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class AttendanceController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public AttendanceController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // --- API ROUTE: UPDATE AN ATTENDANCE CODE FOR A SPECIFIC DAY ---
    [HttpPut]
    [Route("EditAttendance")]
    public IActionResult EditAttendance(UpdateAttendance attendance)
    {
        if (ModelState.IsValid)
        {
            string sqlFormattedDate = attendance.date.HasValue ? attendance.date.Value.ToString("yyyyMMdd") : "";
            // Check if there is an attendance code for that day
            string? connectionString = _configuration?.GetConnectionString("UDEMAppCon")?.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("CheckAttendanceDay", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@idHorario", attendance.idSchedule);
                    command.Parameters.AddWithValue("@fecha", sqlFormattedDate);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // If there was no attendance code, add one to database
                        if ((int)dt.Rows[0]["Conteo"] == 0)
                        {
                            using (SqlCommand cmd = new SqlCommand("InsertAttendance", connection))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@idHorario", attendance.idSchedule);
                                cmd.Parameters.AddWithValue("@fecha", sqlFormattedDate);
                                cmd.Parameters.AddWithValue("@idCódigo", attendance.codeId);

                                cmd.ExecuteNonQuery();
                            }
                        }
                        // If there was an attendance code before, update it
                        else
                        {
                            using (SqlCommand cmd = new SqlCommand("UpdateAttendance", connection))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@idHorario", attendance.idSchedule);
                                cmd.Parameters.AddWithValue("@fecha", sqlFormattedDate);
                                cmd.Parameters.AddWithValue("@idCódigo", attendance.codeId);

                                cmd.ExecuteNonQuery();
                            }
                        }


                        // Return confirmation message
                        return Ok(new { message = "Asistencia actualizada correctamente." });
                    }
                }
            }
        }
        else
            return BadRequest(ModelState);
    }
}