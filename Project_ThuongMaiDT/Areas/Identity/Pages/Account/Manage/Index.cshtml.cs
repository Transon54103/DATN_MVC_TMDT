// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TMDT.DataAccess.Data;
using TMDT.Models;

namespace Project_ThuongMaiDT.Areas.Identity.Pages.Account.Manage
{
public class IndexModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public IndexModel(
        SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public string Username { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string StreetAddress { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    //public string PostalCode { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
            public string Username { get; set; }
            public string Name { get; set; }
            public string StreetAddress { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            //public string PostalCode { get; set; }
        }

    // Load user data
    private async Task LoadAsync(ApplicationUser user)
    {

        Input = new InputModel
        {
            PhoneNumber = user.PhoneNumber,
            Username = user.UserName,
            Name = user.Name,
            StreetAddress = user.StreetAddress,
            City = user.City,
            State = user.State,
            //PostalCode = user.PostalCode,
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _signInManager.UserManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_signInManager.UserManager.GetUserId(User)}'.");
        }

        var appUser = user as ApplicationUser;  // Cast IdentityUser to ApplicationUser
        if (appUser == null)
        {
            return NotFound("User is not an ApplicationUser.");
        }

        await LoadAsync(appUser);
        return Page();
    }

        public async Task<IActionResult> OnPostAsync()
        {
            var dbContext = HttpContext.RequestServices.GetService<ApplicationDbContext>();
            var appUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            var appUse = appUser as ApplicationUser;
            if (appUser == null)
            {
                return NotFound($"Không tìm thấy user {User.Identity.Name}");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(appUse);
                return Page();
            }

            // Gán dữ liệu từ Input model vào appUser
            appUse.Name = Input.Name;
            appUse.PhoneNumber = Input.PhoneNumber;
            appUse.StreetAddress = Input.StreetAddress;
            appUse.City = Input.City;
            appUse.State = Input.State;
            //appUse.PostalCode = Input.PostalCode;

            // Lưu lại vào database
            await dbContext.SaveChangesAsync();

            // Refresh lại SignIn để cập nhật claims
            await _signInManager.RefreshSignInAsync(appUser);

            StatusMessage = "Thông tin đã được cập nhật";
            return RedirectToPage();
        }
    }

}
