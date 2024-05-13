using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminLTE.MVC.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AdminLTE.MVC.Controllers
{
    public class UserRolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private IPasswordHasher<ApplicationUser> _passwordHasher;

        public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IPasswordHasher<ApplicationUser> passwordHasher)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _passwordHasher = passwordHasher;
        }
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();
            foreach (ApplicationUser user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.FirstName = user.FirstName;
                thisViewModel.LastName = user.LastName;
                thisViewModel.Roles = await GetUserRoles(user);
                userRolesViewModel.Add(thisViewModel);
            }
            return View(userRolesViewModel);
        }
        public async Task<IActionResult> Manage(string userId)
        {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }
            ViewBag.UserName = user.UserName;
            var model = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    userRolesViewModel.Selected = true;
                }
                else
                {
                    userRolesViewModel.Selected = false;
                }
                model.Add(userRolesViewModel);
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View();
            }
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }
            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }
            return RedirectToAction("Index");
        }
        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }

        public async Task<IActionResult> AddOrEditUser(string userId)
        {
            var model = new UserViewModel();
            if (string.IsNullOrEmpty(userId))
            {
                return View(model);
            }
            else
            {
                ApplicationUser user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    model.UserId = user.Id;
                    model.Email = user.Email;
                    model.LastName = user.LastName;
                    model.FirstName = user.FirstName;
                    return View(model);
                }
                else
                    return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddOrEditUser(string id, [Bind("UserId,LastName,FirstName,Email,Password,PhoneNumber")] UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(id))
                {
                    ApplicationUser user = new ApplicationUser()
                    {
                        LastName = model.LastName,
                        FirstName = model.FirstName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                    };
                    user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                    IdentityResult result = await _userManager.CreateAsync(user, model.Password);

                    /*if (result.Succeeded)
                    {
                        var token = await userManager.GenerateEmailConfirmationTokenAsync(appUser);
                        var confirmationLink = Url.Action("ConfirmEmail", "Email", new { token, email = user.Email }, Request.Scheme);
                        EmailHelper emailHelper = new EmailHelper();
                        bool emailResponse = emailHelper.SendEmail(user.Email, confirmationLink);

                        if (emailResponse)
                            return RedirectToAction("Index");
                        else
                        {
                            // log email failed 
                        }
                    }*/

                    if (result.Succeeded)
                        return RedirectToAction("Index");
                    else
                    {
                        foreach (IdentityError error in result.Errors)
                            ModelState.AddModelError("", error.Description);
                    }
                }
                else
                {
                    ApplicationUser user = await _userManager.FindByIdAsync(model.UserId);
                    //user.TwoFactorEnabled = true;
                    if (user != null)
                    {
                        if (!string.IsNullOrEmpty(model.Email))
                            user.Email = model.Email;
                        else
                            ModelState.AddModelError("", "Email cannot be empty");

                        if (!string.IsNullOrEmpty(model.Password))
                            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                        else
                            ModelState.AddModelError("", "Password cannot be empty");

                        if (!string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Password))
                        {
                            user.LastName = model.LastName;
                            user.FirstName = model.FirstName;
                            user.Email = model.Email;
                            user.PhoneNumber = model.PhoneNumber;
                            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

                            IdentityResult result = await _userManager.UpdateAsync(user);
                            if (result.Succeeded)
                                return RedirectToAction("Index");
                            else
                            {
                                ModelState.AddModelError("", result.Errors.ToString());
                                return View(user);
                            }
                        }
                    }
                    else
                        ModelState.AddModelError("", "User Not Found");
                }
            }
            return View(model);
        }


    }
}