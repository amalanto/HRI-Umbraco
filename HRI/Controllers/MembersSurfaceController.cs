﻿using ComponentSpace.SAML2;
using HRI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;

namespace HRI.Controllers
{
    public class MembersSurfaceController : SurfaceController
    {
        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return Redirect("/");
        }

        public ActionResult SingleSignOn(IDictionary<string, string> attributes, string targetUrl, string partnerSP)
        {
            // Initiate single sign-on to the service provider (IdP-initiated SSO)]
            // by sending a SAML response containing a SAML assertion to the SP.
            
            // get the member id (was IWS number) from the database           
            string memberId = Services.MemberService.GetByUsername(User.Identity.Name).Properties.Where(p => p.Alias == "memberId").First().Value.ToString();

            // Create a dictionary of attributes to add to the SAML assertion
            Dictionary<string, string> attribs = new Dictionary<string, string>();

            // Attributes for MagnaCare
            attribs.Add("Member ID", memberId);
            attribs.Add("First Name", Services.MemberService.GetByUsername(User.Identity.Name).Properties.Where(p => p.Alias == "firstName").First().Value.ToString());
            attribs.Add("Last Name", Services.MemberService.GetByUsername(User.Identity.Name).Properties.Where(p => p.Alias == "lastName").First().Value.ToString());
            attribs.Add("Product", "PRIMARYSELECT");

            // Attributes for HealthX


                     
            // Send an IdP initiated SAML assertion
            SAMLIdentityProvider.InitiateSSO(
                Response,
                memberId,
                attribs,
                targetUrl,
                partnerSP);

            // Add the response to the ViewBag so we can access it on the front end if we need to
            ViewBag.Response = Response;
            // Return an empty response since we wait for the SAML consumer to send us the requested page
            return new EmptyResult();
        }        

        public ActionResult ActivateUser(string userName, string guid)
        {
            var m = Services.MemberService.GetByUsername(userName);

            bool regSuccess;
            string registerApiUrl = "http://" + Request.Url.Host + ":" + Request.Url.Port + "/umbraco/api/HriApi/RegisterUser?userName=" + userName;
            using(var client = new WebClient())
            {
                var result = client.UploadString(registerApiUrl, userName);
                regSuccess = Convert.ToBoolean(result);
            }

            if (regSuccess)
            {
                m.IsApproved = true;
                System.Web.Security.Roles.AddUserToRole(userName, "Registered");
                return Redirect("/for-members/login");
            }
            return Content("There was an error validating your account. Please Contact Health Republic New York immediately");            
        }

        /// <summary>
        /// HttpPost to change the user's email address
        /// </summary>
        /// <param name="model">Change Email model containing password and email address</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ChangeEmail(ChangeEmailViewModel model)
        {
            try
            {
                // Get the current user
                var user = Membership.GetUser();
                // Verify the password is correct
                if (model.Email == model.Email2)
                {                  
                    if (Membership.ValidateUser( User.Identity.Name, model.Password))
                    {
                        // Set the user's email address to the new supplied email address.
                        user.Email = model.Email;
                        // Update the User profile in the database
                        Membership.UpdateUser((System.Web.Security.MembershipUser)user);
                        // Set the success flag to true
                        TempData["IsSuccessful"] = true;
                    }
                    else // The password was incorrect
                    {
                        // Pass the view model back to the page to persist the data
                        TempData["ViewModel"] = model;
                        // Mark the post as unsuccessful
                        TempData["IsSuccessful"] = false;
                    }                    
                }
                else // Email and confirmation email didnt match
                {
                    // Pass the view model back to the page to persist the data
                    TempData["ViewModel"] = model;
                    // Mark the post as unsuccessful
                    TempData["IsSuccessful"] = false;
                }

                return RedirectToCurrentUmbracoPage();
            }
            catch(Exception ex)
            {
                TempData["IsSuccessful"] = false;
                return RedirectToCurrentUmbracoPage();
            }
        }

        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var user = Membership.GetUser();
            user.ChangePassword(model.OldPassword, model.NewPassword);
            // Update the User profile in the database
            Membership.UpdateUser((System.Web.Security.MembershipUser)user);
            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        public ActionResult ChangeUserName(ChangeUserNameViewModel model)
        {
            // TO-DO: Either extend the membership provider to support username changes
            // or create a new user and copy all the data as per 
            // http://stackoverflow.com/questions/1001491/is-it-possible-to-change-the-username-with-the-membership-api
            //
            var user = Membership.GetUser();
            var user2 = Services.MemberService.GetByUsername("kjfdg");
            //user.UserName = model.UserName;
            // Update the User profile in the database
            Membership.UpdateUser((System.Web.Security.MembershipUser)user);
            return RedirectToCurrentUmbracoPage();
        }

        [HttpGet]
        public ActionResult ResetPassword(string username, string guid)
        {
            // Verify the member exists
            var member = Membership.GetUser(username);            
            if(member == null)
                return Redirect("/");

            // Verify the user provided the correct guid
            if (Services.MemberService.GetByUsername(username).GetValue("guid").ToString() != guid.ToString())
            {
                return Redirect("/");
            }

            // Set the username and guid
            TempData["username"] = username;
            TempData["guid"] = guid;
            return Redirect("/for-members/reset-password/");                        
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            var member = Membership.GetUser("test");
            member.ResetPassword();
            return RedirectToCurrentUmbracoPage();
        }
    }
}