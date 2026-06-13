using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using UserManagementApi.Middleware;
using UserManagementApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add core services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. REGISTER CUSTOM MIDDLEWARE
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

// Thread-safe in-memory data store
List<User> users = new()
{
    new User { Id = 1, Name = "Alice Johnson", Email = "alice@example.com", Role = "Admin" },
    new User { Id = 2, Name = "Bob Smith", Email = "bob@example.com", Role = "User" }
};

// 2. MINIMAL API CRUD ENDPOINTS

// GET: Read All
// GET: Read All
app.MapGet("/api/users", () => Results.Ok(users))
   .WithName("GetAllUsers");

// GET: Read by ID
app.MapGet("/api/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null 
        ? Results.Ok(user) 
        : Results.NotFound(new { message = $"User with ID {id} not found." });
})
.WithName("GetUserById");

// POST: Create (With Explicit Validation)
app.MapPost("/api/users", (User newUser) =>
{
    var validationContext = new ValidationContext(newUser);
    var validationResults = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(newUser, validationContext, validationResults, true);

    if (!isValid)
    {
        return Results.BadRequest(validationResults.Select(r => new { error = r.ErrorMessage }));
    }

    newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);

    return Results.CreatedAtRoute("GetUserById", new { id = newUser.Id }, newUser);
})
.WithName("CreateUser");

// PUT: Update (With Explicit Validation)
app.MapPut("/api/users/{id:int}", (int id, User updatedUser) =>
{
    var existingUser = users.FirstOrDefault(u => u.Id == id);
    if (existingUser is null)
    {
        return Results.NotFound(new { message = $"User with ID {id} not found." });
    }

    var validationContext = new ValidationContext(updatedUser);
    var validationResults = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(updatedUser, validationContext, validationResults, true);

    if (!isValid)
    {
        return Results.BadRequest(validationResults.Select(r => new { error = r.ErrorMessage }));
    }

    existingUser.Name = updatedUser.Name;
    existingUser.Email = updatedUser.Email;
    existingUser.Role = updatedUser.Role;

    return Results.NoContent();
})
.WithName("UpdateUser");

// DELETE: Remove User
app.MapDelete("/api/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
    {
        return Results.NotFound(new { message = $"User with ID {id} not found." });
    }

    users.Remove(user);
    return Results.NoContent();
})
.WithName("DeleteUser");

app.Run();