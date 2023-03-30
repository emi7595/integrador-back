using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class QRController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public QRController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // --- FUNCTION THAT RETURNS THE CORRESPONDING ATTENDANCE CODE, BASED ON THE CURRENT TIME AND THE CLASS HOUR ---
    private int GetAttendanceCode(int currentClass, TimeSpan classHour, TimeSpan currentTime, DateTime date, bool start)
    {
        TimeSpan tenMinutes = TimeSpan.FromMinutes(10);
        // START OF CLASS
        if (start)
        {
            // Class is on time
            if (classHour >= currentTime.Subtract(tenMinutes) && classHour <= currentTime.Add(tenMinutes))
                return 0;
            // Class is late
            else
                return 1;
        }
        // END OF CLASS
        else
        {
            // Get current code of class
            SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT idCódigo FROM Asistencia WHERE idHorario=" + currentClass + "AND Fecha='" + date.ToString("yyyyMMdd") + "'", con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            int currentCode = -1;
            // Current code (registered at the beginning of the class)
            if (dt.Rows.Count > 0)
            {
                currentCode = (int)dt.Rows[0]["idCódigo"];
            }
            // Class ended soon
            if (currentTime < classHour.Subtract(tenMinutes))
            {
                // Class was not late (beginning)
                if (currentCode == 0)
                    return 2;
                // Class was also late (beginning)
                else
                    return 3;
            }
            // Class ended on time
            else
                return currentCode;
        }
    }


    // --- FUNCTION THAT RETURNS THE DESCRIPTION ASSOCIATED WITH AN ATTENDANCE CODE ---
    private string GetCodeMessage(int code)
    {
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT Descripción FROM Códigos WHERE idCódigo=" + code, con);
        DataTable dt = new DataTable();
        da.Fill(dt);
        return (string)dt.Rows[0]["Descripción"];
    }


    // --- FUNCTION THAT REGISTERS AN ATTENDANCE IN THE DATABASE AND RETURNS THE ATTENDANCE CODE ---
    private int SearchAttendance(int nomina, TimeSpan time, DateTime date, string day, bool start)
    {
        // Get the class that the professor is currently on
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT idHorario, Hora_Inicio, Hora_Final FROM Cursos JOIN Horarios ON Cursos.CRN=Horarios.CRN
        WHERE Nómina_Empleado=" + nomina + @" AND " + day + @" IS NOT NULL 
        AND ('" + time + "'>=DATEADD(minute, -10, Hora_Inicio) AND '" + time + "'<=Hora_Final)", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // The professor is currently on that class
        if (dt.Rows.Count > 0)
        {
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

            // There is no attendance for that day
            if ((int)dt.Rows[0]["Conteo"] == 0)
            {
                int code;
                // Beginning of the class
                if (start)
                    code = GetAttendanceCode(currentClass, startHour, time, date, true);
                // End of the class (return an error saying that entrance attendance must be registered first)
                else
                    return -2;
                // Register attendance in database
                con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                SqlCommand cmd = new SqlCommand("INSERT INTO Asistencia VALUES (" + currentClass + ", '" + date.ToString("yyyyMMdd") + "', " + code + ")", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                return code;
            }
            // End of the class - Update attendance code
            else if (!start)
            {
                int code = GetAttendanceCode(currentClass, endHour, time, date, false);
                con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                SqlCommand cmd = new SqlCommand("UPDATE Asistencia SET idCódigo=" + code + " WHERE idHorario=" + currentClass + "AND Fecha='" + date.ToString("yyyyMMdd") + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                return code;
            }
            // The initial attendance has already been taken
            else
            {
                return -1;
            }
        }
        // The professor isn't currently in a class
        else
        {
            return -3;
        }
    }

    // --- FUNCTION THAT REGISTERS THE ATTENDANCE FOR THE CURRENT DAY ---
    private int RegisterAttendance(Attendance attendance, bool start)
    {
        DateTime now = DateTime.Now; // Get current date
        int dayOfWeek = (int)now.DayOfWeek; // Get current day of week
        TimeSpan time = now.TimeOfDay; // Get current time
        DateTime date = now.Date; // Get current day (date)

        int code;

        switch (dayOfWeek)
        {
            case 0: // Sunday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "S1", start); break;
            case 1: // Monday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "M", start); break;
            case 2: // Tuesday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "T", start); break;
            case 3: // Wednesday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "W", start); break;
            case 4: // Thursday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "R", start); break;
            case 5: // Friday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "F", start); break;
            case 6: // Saturday
                code = SearchAttendance(attendance?.nomina ?? 0, time, date, "S", start); break;
            default:
                code = -1; break;
        }

        return code;
    }


    // --- API ROUTE: REGISTER ENTRANCE ATTENDANCE ---
    [HttpPost]
    [Route("RegisterEntrance")]
    public IActionResult RegisterEntrance(Attendance attendance)
    {
        // Register entrance in database
        int code = RegisterAttendance(attendance, true);
        // Return corresponding attendance code
        return Ok(new { code, message = (code == -1 ? "La asistencia inicial ya se ha registrado. Si desea registrar la salida, genere un código de salida." : code == -3 ? "No hay clases en este momento." : GetCodeMessage(code)) });
    }


    // --- API ROUTE: REGISTER DEPARTURE ATTENDANCE ---
    [HttpPost]
    [Route("RegisterDeparture")]
    public IActionResult RegisterDeparture(Attendance attendance)
    {
        // Register entrance in database
        int code = RegisterAttendance(attendance, false);
        // Return corresponding attendance code
        return Ok(new { code, message = (code == -2 ? "No se ha registrado la asistencia inicial." : code == -3 ? "No hay clases en este momento." : GetCodeMessage(code)) });
    }


    // --- API ROUTE: GET INFORMATION OF CURRENT COURSE ---
    [HttpGet]
    [Route("GetCourseData/{nomina}")]
    public string GetCourseData(int nomina)
    {
        DateTime now = DateTime.Now; // Get current date
        int dayOfWeek = (int)now.DayOfWeek; // Get current day of week
        TimeSpan time = now.TimeOfDay; // Get current time
        DateTime date = now.Date; // Get current day (date)

        string day = "";
        switch (dayOfWeek)
        {
            case 0: // Sunday
                day = "S1"; break;
            case 1: // Monday
                day = "M"; break;
            case 2: // Tuesday
                day = "T"; break;
            case 3: // Wednesday
                day = "W"; break;
            case 4: // Thursday
                day = "R"; break;
            case 5: // Friday
                day = "F"; break;
            case 6: // Saturday
                day = "S"; break;
        }

        // Get the information of the class that the professor is currently on
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT Materia, idHorario, Hora_Inicio, Hora_Final FROM Cursos JOIN Horarios ON Cursos.CRN=Horarios.CRN JOIN Materias ON Cursos.CVE_Materia=CVE
        WHERE Nómina_Empleado=" + nomina + @" AND " + day + @" IS NOT NULL 
        AND ('" + time + "'>=DATEADD(minute, -10, Hora_Inicio) AND '" + time + "'<=Hora_Final)", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // The professor is on class
        if (dt.Rows.Count > 0)
        {
            Course c = new Course();
            c.currentClass = (int)dt.Rows[0]["idHorario"];
            c.subjectName = (string)dt.Rows[0]["Materia"];
            c.startHour = (TimeSpan)dt.Rows[0]["Hora_Inicio"];
            c.endHour = (TimeSpan)dt.Rows[0]["Hora_Final"];
            return JsonConvert.SerializeObject(c);
        }
        // The professor doesn't have class at the present time
        else
        {
            return "-1";
        }
    }
}