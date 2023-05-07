using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
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
            Console.WriteLine(attendance.date);
            string sqlFormattedDate = attendance.date.HasValue ? attendance.date.Value.ToString("yyyyMMdd") : "";
            // Check if there is an attendance code for that day
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlDataAdapter da = new SqlDataAdapter(@"
            SELECT COUNT(*) AS Conteo FROM Asistencia WHERE Asistencia.idHorario=" + attendance.idSchedule + "AND Fecha='" + sqlFormattedDate + "'", con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            // If there was no attendance code, add one to database
            if ((int)dt.Rows[0]["Conteo"] == 0)
            {
                con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                SqlCommand cmd = new SqlCommand("INSERT INTO Asistencia VALUES (" + attendance.idSchedule + ", '" + sqlFormattedDate + "', " + attendance.codeId + ")", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            // If there was an attendance code before, update it
            else
            {
                con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                SqlCommand cmd = new SqlCommand("UPDATE Asistencia SET idCódigo=" + attendance.codeId + " WHERE idHorario=" + attendance.idSchedule + "AND Fecha='" + sqlFormattedDate + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }


            // Return confirmation message
            return Ok(new { message = "Asistencia actualizada correctamente." });
        }
        else
            return BadRequest(ModelState);
    }
}