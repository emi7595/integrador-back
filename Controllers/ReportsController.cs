using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using integrador_back.Models;

namespace integrador_back.Controllers;

[ApiController]
[Route("[controller]")]
public class ReportsController : ControllerBase
{
    public readonly IConfiguration _configuration;

    public ReportsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // --- API ROUTE: Descripción ---
    [HttpGet]
    [Route("Professor/GetScheduleDetail/{idHorario}")]
    public string GetScheduleDetail(int idHorario)
    {
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT idHorario, S1, M, T, W, R, F, S FROM Horarios WHERE idHorario=" + idHorario, con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Get specific code of each day
        List<ScheduleDetail> scheduleDetails = new List<ScheduleDetail>();
        if (dt.Rows.Count > 0)
        {
            // Get days in which the class takes place
            List<System.DayOfWeek> days = new List<System.DayOfWeek>();
            if (Convert.ToString(dt.Rows[0]["S1"]) != "") { days.Add(DayOfWeek.Sunday); }
            if (Convert.ToString(dt.Rows[0]["M"]) != "") { days.Add(DayOfWeek.Monday); }
            if (Convert.ToString(dt.Rows[0]["T"]) != "") { days.Add(DayOfWeek.Tuesday); }
            if (Convert.ToString(dt.Rows[0]["W"]) != "") { days.Add(DayOfWeek.Wednesday); }
            if (Convert.ToString(dt.Rows[0]["R"]) != "") { days.Add(DayOfWeek.Thursday); }
            if (Convert.ToString(dt.Rows[0]["F"]) != "") { days.Add(DayOfWeek.Friday); }
            if (Convert.ToString(dt.Rows[0]["S"]) != "") { days.Add(DayOfWeek.Saturday); }

            // Get attendance code for each day
            DateTime semesterBegin = new DateTime(2023, 1, 16);
            DateTime curDate = DateTime.Now; // fecha actual

            while (semesterBegin <= curDate)
            {
                if (days.Contains(semesterBegin.DayOfWeek))
                {
                    con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                    da = new SqlDataAdapter(@"
                    SELECT Asistencia.idCódigo, Descripción FROM Asistencia JOIN Códigos ON Asistencia.idCódigo=Códigos.idCódigo
                    WHERE idHorario=" + idHorario + "AND Fecha='" + semesterBegin.ToString("yyyy-MM-dd") + "'", con);
                    dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        ScheduleDetail sD = new ScheduleDetail();
                        sD.codeId = Convert.ToInt16(dt.Rows[0]["idCódigo"]);
                        sD.codeDescription = Convert.ToString(dt.Rows[0]["Descripción"]);
                        sD.date = semesterBegin.Date;
                        scheduleDetails.Add(sD);
                    }
                    else // Falta
                    {
                        ScheduleDetail sD = new ScheduleDetail();
                        sD.codeId = 4;
                        sD.codeDescription = "Falta";
                        sD.date = semesterBegin.Date;
                        scheduleDetails.Add(sD);
                    }
                }
                semesterBegin = semesterBegin.AddDays(1);
            }
            
            return JsonConvert.SerializeObject(scheduleDetails);
        }
        // The professor has no classes
        else
        {
            return "El horario no es válido";
        }
    }


    // --- API ROUTE: Descripción ---
    [HttpGet]
    [Route("Professor/GetAttendanceAverage/{nomina}")]
    public string GetAttendanceAverage(int nomina)
    {
        // Get all classes
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT Nómina, Nombre_Empleado, Materia, Cursos.CRN, CONCAT(TRIM(Cursos.Subject), '-', Cursos.CVE_Materia, '-', Cursos.Grupo) AS 'CVE_Materia', idHorario, S1, M, T, W, R, F, S
        FROM Empleados 
            JOIN Cursos ON Nómina=Nómina_Empleado 
            JOIN Materias ON (CVE=CVE_Materia AND Materias.Subject=Cursos.Subject)
            JOIN Horarios ON (Cursos.CRN=Horarios.CRN AND Cursos.Subject=Horarios.Subject AND Cursos.CVE_Materia=Horarios.CVE_Materia AND Cursos.Grupo=Horarios.Grupo AND Cursos.Salón=Horarios.Salón)
        WHERE Nómina=" + nomina, con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Check attendance average for each class
        List<ProfessorAttendanceAvg> classes = new List<ProfessorAttendanceAvg>();
        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                ProfessorAttendanceAvg p = new ProfessorAttendanceAvg();
                p.nomina = Convert.ToString(dt.Rows[i]["Nómina"]);
                p.nombreEmpleado = Convert.ToString(dt.Rows[i]["Nombre_Empleado"]);
                p.materia = Convert.ToString(dt.Rows[i]["Materia"]);
                p.CRN = Convert.ToString(dt.Rows[i]["CRN"]);
                p.CVE_Materia = Convert.ToString(dt.Rows[i]["CVE_Materia"]);
                p.idHorario = Convert.ToString(dt.Rows[i]["idHorario"]);
                int numberSessions = 0;
                int numberMovements = 0;
                int[] codes = new int[11];

                // Get days in which the class takes place
                List<System.DayOfWeek> days = new List<System.DayOfWeek>();
                if (Convert.ToString(dt.Rows[i]["S1"]) != "") { days.Add(DayOfWeek.Sunday); }
                if (Convert.ToString(dt.Rows[i]["M"]) != "") { days.Add(DayOfWeek.Monday); }
                if (Convert.ToString(dt.Rows[i]["T"]) != "") { days.Add(DayOfWeek.Tuesday); }
                if (Convert.ToString(dt.Rows[i]["W"]) != "") { days.Add(DayOfWeek.Wednesday); }
                if (Convert.ToString(dt.Rows[i]["R"]) != "") { days.Add(DayOfWeek.Thursday); }
                if (Convert.ToString(dt.Rows[i]["F"]) != "") { days.Add(DayOfWeek.Friday); }
                if (Convert.ToString(dt.Rows[i]["S"]) != "") { days.Add(DayOfWeek.Saturday); }

                // Get attendance code for each day
                DateTime semesterBegin = new DateTime(2023, 1, 16);
                DateTime curDate = DateTime.Now; // fecha actual

                while (semesterBegin <= curDate)
                {
                    if (days.Contains(semesterBegin.DayOfWeek))
                    {
                        con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
                        SqlDataAdapter da2 = new SqlDataAdapter(@"
                        SELECT idCódigo FROM Asistencia WHERE idHorario=" + p.idHorario + @"
                        AND Fecha='" + semesterBegin.ToString("yyyy-MM-dd") + "'", con);
                        DataTable dt2 = new DataTable();
                        da2.Fill(dt2);

                        if (dt2.Rows.Count > 0)
                        {
                            if (Convert.ToInt16(dt2.Rows[0]["idCódigo"]) >= 1 && Convert.ToInt16(dt2.Rows[0]["idCódigo"]) <= 3)
                            {
                                numberMovements++;
                                numberSessions++;
                            }
                            else if (Convert.ToInt16(dt2.Rows[0]["idCódigo"]) == 0)
                            {
                                numberSessions++;
                            }

                            // Add info for graphic
                            codes[Convert.ToInt16(dt2.Rows[0]["idCódigo"])]++;
                        }
                        else // Falta
                        {
                            numberMovements++;
                            numberSessions++;
                            codes[4]++;
                        }
                    }
                    semesterBegin = semesterBegin.AddDays(1);
                }

                p.average = Math.Round(100 - (numberMovements * 1.0 / numberSessions * 100), 1);
                p.codes = codes;

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


    // --- API ROUTE: Descripción ---
    [HttpGet]
    [Route("Director/GetDepartmentAverage/{idDepartamento}")]
    public string GetDepartmentAverage(int idDepartamento)
    {
        // Get professors in department
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT Nómina, Nombre_Departamento FROM Empleados JOIN Departamentos ON Empleados.idDepartamento=Departamentos.idDepartamento
        WHERE idRol=1 AND Departamentos.idDepartamento=" + idDepartamento, con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Check attendance average for each professor
        if (dt.Rows.Count > 0)
        {
            List<DepartmentAvg> departmentAvgs = new List<DepartmentAvg>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                List<ProfessorAttendanceAvg>? professorAvg = JsonConvert.DeserializeObject<List<ProfessorAttendanceAvg>>(GetAttendanceAverage(Convert.ToInt16(dt.Rows[i]["Nómina"])));
                if (professorAvg != null)
                {
                    DepartmentAvg d = new DepartmentAvg();
                    d.nomina = professorAvg[0].nomina;
                    d.nombreEmpleado = professorAvg[0].nombreEmpleado;
                    d.departmentName = Convert.ToString(dt.Rows[0]["Nombre_Departamento"]);
                    double avgAux = 0;
                    int[] codes = new int[11];
                    foreach (var item in professorAvg)
                    {
                        avgAux += item.average == null ? 0 : (double)item.average;
                        for (int j=0; j<11; j++)
                            codes[j] += item?.codes?[j] ?? 0;
                    }
                    d.average = avgAux / professorAvg.Count;
                    d.codes = codes;
                    departmentAvgs.Add(d);
                }
            }

            return JsonConvert.SerializeObject(departmentAvgs);
        }  
        // There are no professors in the department
        else
        {
            return "No hay profesores en el departamento";
        }
    }


    // --- API ROUTE: Descripción ---
    [HttpGet]
    [Route("Vicerrector/GetSchoolAverage/{idEscuela}")]
    public string GetSchoolAverage(int idEscuela)
    {
        // Get all departments in school
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter(@"
        SELECT idDepartamento, Nombre_Departamento, Nombre_Escuela 
        FROM Departamentos JOIN Escuelas ON Departamentos.idEscuela=Escuelas.idEscuela
        WHERE Departamentos.idEscuela=" + idEscuela, con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Check attendance average for each department
        if (dt.Rows.Count > 0)
        {
            List<SchoolAvg> schoolAvgs = new List<SchoolAvg>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string res = GetDepartmentAverage(Convert.ToInt16(dt.Rows[i]["idDepartamento"]));
                if (res != "No hay profesores en el departamento")
                {
                    List<DepartmentAvg>? departmentAvg = JsonConvert.DeserializeObject<List<DepartmentAvg>>(res);
                    if (departmentAvg != null)
                    {
                        SchoolAvg s = new SchoolAvg();
                        s.schoolName = Convert.ToString(dt.Rows[0]["Nombre_Escuela"]);
                        s.departmentId = Convert.ToInt16(dt.Rows[0]["idDepartamento"]);
                        s.departmentName = departmentAvg[0].departmentName;
                        double avgAux = 0;
                        int[] codes = new int[11];
                        foreach (var item in departmentAvg)
                        {
                            avgAux += item.average == null ? 0 : (double)item.average;
                            for (int j=0; j<11; j++)
                                codes[j] += item?.codes?[j] ?? 0;
                        }
                        s.average = avgAux / departmentAvg.Count;
                        s.codes = codes;
                        schoolAvgs.Add(s);
                    }
                }
                else // There are no professors in the department
                {
                    SchoolAvg s = new SchoolAvg();
                    s.schoolName = Convert.ToString(dt.Rows[0]["Nombre_Escuela"]);
                    s.departmentId = Convert.ToInt16(dt.Rows[0]["idDepartamento"]);
                    s.departmentName = Convert.ToString(dt.Rows[i]["Nombre_Departamento"]);
                    s.average = -1;
                    if (s.codes != null)
                        for (int j=0; j<11; j++) { s.codes[j] = 0; }
                    schoolAvgs.Add(s);
                }          
            }

            return JsonConvert.SerializeObject(schoolAvgs);
        }  
        // There are no departments in the school
        else
        {
            return "La escuela no tiene departamentos";
        }
    }


    // --- API ROUTE: Descripción ---
    [HttpGet]
    [Route("Rector/GetUDEMAverage")]
    public string GetUDEMAverage()
    {
        // Get all schools
        SqlConnection con = new SqlConnection(_configuration?.GetConnectionString("UDEMAppCon")?.ToString());
        SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Escuelas", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        // Check attendance average for each school
        if (dt.Rows.Count > 0)
        {
            List<UDEMAvg> UDEMAvg = new List<UDEMAvg>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                List<SchoolAvg>? schoolAvg = JsonConvert.DeserializeObject<List<SchoolAvg>>(GetSchoolAverage(Convert.ToInt16(dt.Rows[i]["idEscuela"])));
                if (schoolAvg != null)
                {
                    UDEMAvg u = new UDEMAvg();
                    u.schoolId = Convert.ToInt16(dt.Rows[i]["idEscuela"]);
                    u.schoolName = Convert.ToString(dt.Rows[i]["Nombre_Escuela"]);
                    double avgAux = 0;
                    int[] codes = new int[11];
                    int available = 0;
                    foreach (var item in schoolAvg)
                    {
                        if (item.average != -1)
                        {
                            available++;
                            avgAux += item.average == null ? 0 : (double)item.average;
                            for (int j=0; j<11; j++)
                                codes[j] += item?.codes?[j] ?? 0;
                        }
                    }
                    u.average = available > 0 ? avgAux / available : -1;
                    u.codes = codes;
                    UDEMAvg.Add(u);
                }       
            }

            return JsonConvert.SerializeObject(UDEMAvg);
        }  
        // There are no schools available
        else
        {
            return "No hay escuelas";
        }
    }
}