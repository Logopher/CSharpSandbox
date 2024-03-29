﻿using Microsoft.AspNetCore.Mvc;
using CSharpSandbox.Mvc.Models;
using System.Diagnostics;
using CSharpSandbox.Common;
using NLog;

namespace CSharpSandbox.Mvc.Controllers
{
    public class HomeController : Controller
    {
        static readonly Logger CurrentLogger = LogManager.GetCurrentClassLogger();

        public HomeController()
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}