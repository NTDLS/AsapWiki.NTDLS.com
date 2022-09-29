﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Repository;

namespace AsapWikiCom.Controllers
{
    [Authorize]
    public class UserController : ControllerHelperBase
    {
        [AllowAnonymous]
        public ActionResult Login()
        {
            Configure();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(AsapWiki.Shared.Models.FormLogin user)
        {
            if (ModelState.IsValid)
            {
                if (PerformLogin(user.EmailAddress, user.Password))
                {
                    if (Request.QueryString["ReturnUrl"] != null && Request.QueryString["ReturnUrl"] != "/")
                    {
                        return Redirect(Request.QueryString["ReturnUrl"]);
                    }
                    else
                    {
                        return RedirectToAction("Content", "Wiki", "Home");
                    }
                }
                ModelState.AddModelError("", "invalid Username or Password");
            }
            return View();
        }

        [AllowAnonymous]
        public ActionResult Forgot()
        {
            Configure();
            return View();
        }

        [AllowAnonymous]
        public ActionResult Reset()
        {
            Configure();
            return View();
        }

        [AllowAnonymous]
        public ActionResult Signup()
        {
            if (ConfigurationEntryRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return new HttpUnauthorizedResult();
            }

            Configure();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Signup(AsapWiki.Shared.Models.FormSignup user)
        {
            if (ConfigurationEntryRepository.Get("Membership", "Allow Signup", false) == false)
            {
                return new HttpUnauthorizedResult();
            }

            Configure();
            return View();
        }

        [Authorize]
        public ActionResult UserProfile()
        {
            Configure();
            return View();
        }

    }
}
