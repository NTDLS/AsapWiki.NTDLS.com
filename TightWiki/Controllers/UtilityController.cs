﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using TightWiki.Controllers;
using TightWiki.Models.ViewModels.Utility;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class UtilityController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        : WikiControllerBase(signInManager, userManager)
    {
        [AllowAnonymous]
        [HttpGet("Notify")]
        public ActionResult Notify()
        {
            WikiContext.RequireViewPermission();

            var model = new NotifyViewModel()
            {
                SuccessMessage = GetQueryValue("SuccessMessage", string.Empty),
                ErrorMessage = GetQueryValue("ErrorMessage", string.Empty),
                RedirectURL = GetQueryValue("RedirectURL", string.Empty),
                RedirectTimeout = GetQueryValue("RedirectTimeout", 0)
            };

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost("ConfirmAction")]
        public ActionResult ConfirmAction(ConfirmActionViewModel model)
        {
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet("ConfirmAction")]
        public ActionResult ConfirmAction()
        {
            var model = new ConfirmActionViewModel
            {
                ControllerURL = GetQueryValue("controllerURL").EnsureNotNull(),
                YesRedirectURL = GetQueryValue("yesRedirectURL").EnsureNotNull(),
                NoRedirectURL = GetQueryValue("noRedirectURL").EnsureNotNull(),
                Message = GetQueryValue("message").EnsureNotNull(),
                Style = GetQueryValue("Style").EnsureNotNull()
            };

            return View(model);
        }
    }
}
