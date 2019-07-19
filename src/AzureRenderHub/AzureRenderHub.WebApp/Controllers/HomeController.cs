// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Client;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : ViewBaseController
    {
        public HomeController(ITokenAcquisition tokenAcquisition) : base (tokenAcquisition)
        {

        }

        [HttpGet]
        public IActionResult Index()
        {
            return Redirect("Environments");
        }

        [HttpGet]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";
            return View();
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
