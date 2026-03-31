using FluentValidation.AspNetCore;
using LeaveOvertimeAPI.Services;
using LeaveOvertimeAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Quartz;
using LeaveOvertimeAPI.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Konfigurimi i Database (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));

// Konfigurimi i Autentifikimit JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "Celesi_Sekret_Shume_I_Sigurt_123");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"]
    };
});

// Konfigurimi i Swagger me Suport per JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Leave & Overtime API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Vendos Token-in ne kete format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] { }
        }
    });
});

// Regjistrimi i Background Service
builder.Services.AddHostedService<LeaveStatusService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ReportExportService>();

// Quartz Cron Jobs
builder.Services.AddQuartz(q =>
{
    // Job 1: Urime Viti i Ri - 31 Dhjetor ne oren 09:00
    var greetingJobKey = new JobKey("NewYearGreetingJob");
    q.AddJob<NewYearGreetingJob>(opts => opts.WithIdentity(greetingJobKey));
    q.AddTrigger(opts => opts
        .ForJob(greetingJobKey)
        .WithIdentity("NewYearGreetingJob-trigger")
        .WithCronSchedule("0 0 9 31 12 ? *"));

    // Job 2: Kujtese Leje te Mbartura - 1 Mars ne oren 09:00
    var reminderJobKey = new JobKey("PreviousYearLeaveReminderJob");
    q.AddJob<PreviousYearLeaveReminderJob>(opts => opts.WithIdentity(reminderJobKey));
    q.AddTrigger(opts => opts
        .ForJob(reminderJobKey)
        .WithIdentity("PreviousYearLeaveReminderJob-trigger")
        .WithCronSchedule("0 0 9 1 3 ? *"));
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

// Regjistrimi i Controllers me IgnoreCycles per JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// Aktivizimi i Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Leave Overtime API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();