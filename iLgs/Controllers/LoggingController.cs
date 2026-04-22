using iLgs.Controllers;
using iLgs.Models;
using iLgs.Services.Logs;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

public class LoggingController : BaseController
{
    //private readonly AppManEntities _db;
    private readonly ILoggingService _loggingService;

    public LoggingController()
    {
        //_db = db;
        _loggingService = new LoggingService();
    }

    // ============================
    // GET: /Logging/Index
    // Displays all logs
    // ============================
    [HttpGet]
    public async Task<ActionResult> Index()
    {
        try
        {
            // Retrieve all logs (latest first)
            var logs = await Task.Run(() =>
                _db.ErrorLogs
                   .OrderByDescending(l => l.InsertedDt)
                   .ToList()
            );

            return View(logs);
        }
        catch (Exception ex)
        {
            await _loggingService.LogError(ex);
            return new HttpStatusCodeResult(500, "Error retrieving logs.");
        }
    }

    // ============================
    // GET: /Logging/TestLogging
    // Just a sample log generator
    // ============================
    [HttpGet]
    public ActionResult TestLogging()
    {
        try
        {
            _loggingService.LogInformation("TestLogging action executed.");

            int x = 0;
            int y = 1 / x; // Will throw DivideByZeroException
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex);
            return new HttpStatusCodeResult(500, "An error occurred and was logged.");
        }

        return Content("Test log added successfully.");
    }

    public async Task<ActionResult> Read([DataSourceRequest] DataSourceRequest request)
    {
        var data = await Task.Run(() =>
                _db.ErrorLogs
                   .OrderByDescending(l => l.InsertedDt)
                   .AsQueryable()
            );

        var result = new JsonNetResult
        {
            Data = data.ToDataSourceResult(request),
            JsonRequestBehavior = JsonRequestBehavior.AllowGet,
            Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        };
        return result;
    }
}
