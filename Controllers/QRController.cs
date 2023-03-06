using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class QRController : ControllerBase {
    public readonly IConfiguration _configuration;

    public QRController(IConfiguration configuration) {
        _configuration = configuration;
    }

    // Function that returns the corresponding attendance code, based on the current time and the class hour
    private int GetAttendanceCode(int currentClass, TimeSpan classHour, TimeSpan currentTime, DateTime date, bool start) {
        TimeSpan tenMinutes = TimeSpan.FromMinutes(10);
        // Start of class
        if (start) {
            // Class is on time
            if (classHour >= currentTime.Subtract(tenMinutes) && classHour <= currentTime.Add(tenMinutes))
                return 0;
            // Class is late
            else
                return 1;
        }
        // End of class
        else {
            // Get current code of class
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT idCódigo FROM Asistencia WHERE idHorario=" + currentClass + "AND Fecha='" + date.ToString("yyyyMMdd") + "'", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            int currentCode = 1; // No se tomó asistencia al inicio de la clase
            // Se tomó asistencia al inicio de la clase
            if (dt.Rows.Count > 0) {
                currentCode = (int)dt.Rows[0]["idCódigo"];
            }
            // Class ended soon
            if (currentTime < classHour.Subtract(tenMinutes)) {
                // Class was not late
                if (currentCode == 0)
                    return 2;
                // Class was also late
                else
                    return 3;
            }
            // Class ended on time
            else
                return currentCode;
        }
    }

    // Function that returns the description associated with an attendance code
    private string GetCodeMessage(int code) {
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT Descripción FROM Códigos WHERE idCódigo=" + code, con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        return (string)dt.Rows[0]["Descripción"];
    }

    // Function that registers an attendance on a certain date
    private int SearchAttendance(int nomina, TimeSpan time, DateTime date, string day, bool start) {
        // Get the class that the professor is currently on
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT idHorario, Hora_Inicio, Hora_Final FROM Cursos JOIN Horarios ON Cursos.CRN=Horarios.CRN
        WHERE Nómina_Empleado=" + nomina + @" AND " + day + @" IS NOT NULL 
        AND ('" + time + "'>=DATEADD(minute, -10, Hora_Inicio) AND '" + time + "'<=Hora_Final)", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Get data of current class
        int currentClass = (int)dt.Rows[0]["idHorario"];
        TimeSpan startHour = (TimeSpan)dt.Rows[0]["Hora_Inicio"];
        TimeSpan endHour = (TimeSpan)dt.Rows[0]["Hora_Final"];

        // Check if there's an attendance field for this class and date
        con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        da = new SqlDataAdapter(@"
        SELECT COUNT(*) AS Conteo FROM Asistencia JOIN Horarios ON Asistencia.idHorario=Horarios.idHorario
        WHERE Asistencia.idHorario=" + currentClass + "AND Fecha='" + date.ToString("yyyyMMdd") + "'", con);
        dt = new DataTable();
        da.Fill(dt);

        // Aún no hay asistencia, independientemente de si inició o terminó la clase
        if ((int)dt.Rows[0]["Conteo"] == 0) {
            int code;
            if (start)
                code = GetAttendanceCode(currentClass, startHour, time, date, true);
            else
                return -2;
            // Check corresponding code based on time and register it in the database
            con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlCommand cmd = new SqlCommand("INSERT INTO Asistencia VALUES (" + currentClass + ", '" + date.ToString("yyyyMMdd") +"', " + code + ")", con);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
            return code;
        }
        // Se está terminando la clase
        else if(!start) {
            int code = GetAttendanceCode(currentClass, endHour, time, date, false);
            con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlCommand cmd = new SqlCommand("UPDATE Asistencia SET idCódigo=" + code + " WHERE idHorario=" + currentClass, con);                
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
            return code;
        }
        else {
            return -1;
        }
    }

    private int RegisterAttendance(Attendance attendance, bool start) {
        // Get current date
        DateTime now = DateTime.Now;

        // Get current day of week
        int dayOfWeek = (int)now.DayOfWeek;

        // Get current time
        TimeSpan time = now.TimeOfDay;

        // Get current day (date)
        DateTime date = now.Date;

        int code;

        switch(dayOfWeek) {
            case 0: // Sunday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "S1", start);
                break;
            case 1: // Monday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "M", start);
                break;
            case 2: // Tuesday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "T", start);
                break;
            case 3: // Wednesday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "W", start);
                break;
            case 4: // Thursday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "R", start);
                break;
            case 5: // Friday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "F", start);
                break;
            case 6: // Saturday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "S", start);
                break;
            default:
                code = -1;
                break;
        }

        return code;
    }

    [HttpPost]
    [Route("RegisterEntrance")]
    public IActionResult RegisterEntrance(Attendance attendance) {
        int code = RegisterAttendance(attendance, true);
        return Ok(new { code, message = (code == -1 ? "La asistencia inicial ya se ha registrado. Si desea registrar la salida, genere un código de salida." : GetCodeMessage(code)) });
    }

    [HttpPost]
    [Route("RegisterDeparture")]
    public IActionResult RegisterDeparture(Attendance attendance) {
        int code = RegisterAttendance(attendance, false);
        return Ok(new { code, message = (code == -2 ? "No se ha registrado la asistencia inicial." : GetCodeMessage(code)) });
    }

    [HttpGet]
    [Route("GetCourseData/{nomina}")]
    public string GetCourseData(int nomina) {
        // Get current date
        DateTime now = DateTime.Now;

        // Get current day of week
        int dayOfWeek = (int)now.DayOfWeek;

        // Get current time
        TimeSpan time = now.TimeOfDay;

        // Get current day (date)
        DateTime date = now.Date;

        string day = "";
        switch(dayOfWeek) {
            case 0: // Sunday
                day = "S1";
                break;
            case 1: // Monday
                day = "M";
                break;
            case 2: // Tuesday
                day = "T";
                break;
            case 3: // Wednesday
                day = "W";
                break;
            case 4: // Thursday
                day = "R";
                break;
            case 5: // Friday
                day = "F";
                break;
            case 6: // Saturday
                day = "S";
                break;
        }

        // Get the class that the professor is currently on
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT idHorario, Hora_Inicio, Hora_Final FROM Cursos JOIN Horarios ON Cursos.CRN=Horarios.CRN
        WHERE Nómina_Empleado=" + nomina + @" AND " + day + @" IS NOT NULL 
        AND ('" + time + "'>=DATEADD(minute, -10, Hora_Inicio) AND '" + time + "'<=Hora_Final)", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // The professor is on class
        if (dt.Rows.Count > 0) {
            Course c = new Course();
            c.currentClass = (int)dt.Rows[0]["idHorario"];
            c.startHour = (TimeSpan)dt.Rows[0]["Hora_Inicio"];
            c.endHour = (TimeSpan)dt.Rows[0]["Hora_Final"];
            return JsonConvert.SerializeObject(c);
        }
        // The professor doesn't have class at the present time
        else {
            return "-1";
        }
    }
}