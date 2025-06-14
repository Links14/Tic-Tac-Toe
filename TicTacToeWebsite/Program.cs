var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDefaultFiles();  // looks for index.html by default
app.UseStaticFiles();   // serves CSS, JS, images, etc.

app.UseRouting();

app.Run();
