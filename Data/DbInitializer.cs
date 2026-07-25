using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                "Admin",
                "User"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAdmin(UserManager<ApplicationUser> userManager)
        {
            string email = "admin@library.com";

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    FullName = "System Administrator",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.Now
                };

                await userManager.CreateAsync(
                    user,
                    "Admin@123");

                await userManager.AddToRoleAsync(
                    user,
                    "Admin");
            }
        }
        public static async Task SeedBooks(ApplicationDbContext context)
        {
            if (context.Books.Any())
                return;

            for (int i = 1; i <= 100; i++)
            {
                context.Books.Add(new Book
                {
                    Title = $"Book {i}",
                    ISBN = $"ISBN{i:00000}",
                    Quantity = 20,
                    AvailableQuantity = 20,
                    CategoryId = 1,
                    AuthorId = 1,
                    PublisherId = 1,
                    ImageUrl = "/images/books/no-image.png"
                });
            }

            await context.SaveChangesAsync();
        }
    }
}