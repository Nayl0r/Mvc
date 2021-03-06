// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite.Controllers
{
    // Views returned by this controller use #ifdefs for defines specified in project.json
    // The intent of this controller is to verify that view compilation uses the app's compilation settings.
    public class ViewsConsumingCompilationOptionsController : Controller
    {
        public ViewResult Index()
        {
            return View();
        }
    }
}