using DocumentFormat.OpenXml.Spreadsheet;
using LibraryManagement.Models;
using LibraryManagement.Services.Interfaces;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _email;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            IEmailService email)
        {
            _userManager = userManager;
            _email = email;
        }

        public async Task<IActionResult> Index(string? search, string? role, string? status)
        {
            var users = await _userManager.Users.ToListAsync();

            var model = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    Role = roles.FirstOrDefault() ?? "User"
                });
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                model = model.Where(x =>
                    x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.UserName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Role
            if (!string.IsNullOrWhiteSpace(role))
            {
                model = model.Where(x => x.Role == role).ToList();
            }

            // Status
            switch (status)
            {
                case "Active":
                    model = model.Where(x => !x.IsLocked).ToList();
                    break;

                case "Locked":
                    model = model.Where(x => x.IsLocked).ToList();
                    break;

                case "Confirmed":
                    model = model.Where(x => x.EmailConfirmed).ToList();
                    break;

                case "NotConfirmed":
                    model = model.Where(x => !x.EmailConfirmed).ToList();
                    break;
            }

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // Không cho tự xóa
            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["Error"] =
                    "You cannot delete your own account.";

                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Không cho xóa Admin cuối cùng
            if (roles.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                if (admins.Count <= 1)
                {
                    TempData["Error"] =
                        "Cannot delete the last administrator.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    string.Join("<br>",
                    result.Errors.Select(x => x.Description));

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "User deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserDetailViewModel
            {
                User = user,
                Role = roles.FirstOrDefault() ?? "User"
            };

            return View(model);
        }


        public IActionResult Create()
        {
               ViewBag.Roles = new List<string>
            {
                "Admin",
                "User",
                "Warehouse"
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new List<string>
                    {
                        "Admin",
                        "User",
                        "Warehouse"
                    };
                return View(model);
            }

            var exist = await _userManager.FindByEmailAsync(model.Email);

            if (exist != null)
            {
                ModelState.AddModelError("", "Email already exists.");

                ViewBag.Roles = new List<string>
                    {
                        "Admin",
                        "User",
                        "Warehouse"
                    };

                return View(model);
            }

            string username = GenerateUsername(model.FullName);

            string password = GeneratePassword();

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = username,
                EmailConfirmed = true,
                CreatedDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, password);
            TempData["Success"] = $"Created. Password = {password}";
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    TempData["Error"] += error.Code + " : " + error.Description + "<br>";
                }

                return RedirectToAction(nameof(Index));
            }
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                await _email.SendAsync(
                        user.Email!,
                        "Library Management - Account Created",

                    $@"

                    <div style='font-family:Segoe UI;padding:30px'>

                    <h2 style='color:#0d6efd'>
                    📚 Library Management
                    </h2>

                    <hr/>

                    <p>Hello <b>{user.FullName}</b>,</p>

                    <p>Your account has been created.</p>

                    <table>

                    <tr>
                    <td><b>Username</b></td>
                    <td style='padding-left:15px'>
                    {username}
                    </td>
                    </tr>

                    <tr>
                    <td><b>Password</b></td>
                    <td style='padding-left:15px;color:red'>
                    {password}
                    </td>
                    </tr>

                    <tr>
                    <td><b>Role</b></td>
                    <td style='padding-left:15px'>
                    {model.Role}
                    </td>
                    </tr>

                    </table>

                    <br/>

                    <p>Please login and change your password.</p>

                    <hr/>

                    <small>Library Management System</small>

                    </div>

                    ");

                TempData["Success"] = "User created successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.Roles = new List<string>
                {
                    "Admin",
                    "User",
                    "Warehouse"
                };
            return View(model);
        }


        public async Task<IActionResult> Lock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            // Không cho tự khóa
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot lock your own account.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                if (admins.Count <= 1)
                {
                    TempData["Error"] =
                        "Cannot lock the last administrator.";

                    return RedirectToAction(nameof(Index));
                }
            }

            user.LockoutEnd = DateTimeOffset.Now.AddYears(100);

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "User locked successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.LockoutEnd = null;

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "User unlocked successfully.";

            return RedirectToAction(nameof(Index));
        }

        private string RemoveDiacritics(string text)
        {
            text = text.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();

            foreach (char c in text)
            {
                var unicode = CharUnicodeInfo.GetUnicodeCategory(c);

                if (unicode != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString()
                     .Normalize(NormalizationForm.FormC)
                     .Replace('đ', 'd')
                     .Replace('Đ', 'D');
        }

        private string GenerateUsername(string fullName)
        {
            string username = RemoveDiacritics(fullName);

            username = username.ToLower();

            username = Regex.Replace(username, @"\s+", "");

            username += Random.Shared.Next(100, 999);

            return username;
        }
        private string GeneratePassword(int length = 10)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digit = "0123456789";
            const string special = "@#$%&*!";

            var random = new Random();

            var password = new List<char>
    {
        upper[random.Next(upper.Length)],
        lower[random.Next(lower.Length)],
        digit[random.Next(digit.Length)],
        special[random.Next(special.Length)]
    };

            string all = upper + lower + digit + special;

            while (password.Count < length)
            {
                password.Add(all[random.Next(all.Length)]);
            }

            // Shuffle
            password = password
                .OrderBy(x => random.Next())
                .ToList();

            return new string(password.ToArray());
        }

        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            string newPassword = GeneratePassword();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                user,
                token,
                newPassword);

            if (result.Succeeded)
            {
                  await _email.SendAsync(
                        user.Email!,
                        "Library Management - Password Reset",

                    $@"

                    <div style='font-family:Segoe UI;padding:30px'>

                    <h2 style='color:#0d6efd'>
                    📚 Library Management
                    </h2>

                    <hr/>

                    <p>Hello <b>{user.FullName}</b>,</p>

                    <p>
                    Your password has been reset successfully.
                    </p>

                    <table style='border-collapse:collapse'>

                    <tr>

                    <td><b>Username</b></td>

                    <td style='padding-left:15px'>
                    {user.UserName}
                    </td>

                    </tr>

                    <tr>

                    <td><b>Password</b></td>

                    <td style='padding-left:15px;color:red'>

                    {newPassword}

                    </td>

                    </tr>

                    </table>

                    <br/>

                    <p>

                    Please login and change your password immediately.

                    </p>

                    <hr/>

                    <small>

                    Library Management System

                    </small>

                    </div>

                    ");

                TempData["Success"] =
                    "Password has been reset and emailed.";
            }
            else
            {
                TempData["Error"] =
                    string.Join("<br>", result.Errors.Select(x => x.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> LockSelected(string ids)
        {
            if (string.IsNullOrEmpty(ids))
                return RedirectToAction(nameof(Index));

            foreach (var id in ids.Split(','))
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                    continue;

                user.LockoutEnd = DateTimeOffset.Now.AddYears(100);

                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "Users locked successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UnlockSelected(string ids)
        {
            if (string.IsNullOrEmpty(ids))
                return RedirectToAction(nameof(Index));

            foreach (var id in ids.Split(','))
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                    continue;

                user.LockoutEnd = null;

                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "Users unlocked successfully.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> DeleteSelected(string ids)
        {
            if (string.IsNullOrEmpty(ids))
                return RedirectToAction(nameof(Index));

            foreach (var id in ids.Split(','))
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                    continue;

                await _userManager.DeleteAsync(user);
            }

            TempData["Success"] = "Users deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ResetPasswordSelected(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return RedirectToAction(nameof(Index));

            foreach (var id in ids.Split(','))
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                    continue;

                string newPassword = GeneratePassword();

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var result = await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    newPassword);

                if (result.Succeeded)
                {
                    await _email.SendAsync(
                        user.Email!,
                        "Password Reset",
                        $"<h3>Your new password:</h3><h2>{newPassword}</h2>");
                }
            }

            TempData["Success"] =
                "Passwords have been reset.";

            return RedirectToAction(nameof(Index));
        }

    }
}