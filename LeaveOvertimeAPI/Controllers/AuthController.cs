using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.DTOs;
using LeaveOvertimeAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LeaveOvertimeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignupDto dto)
        {
            if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
                return Conflict("Ky email ekziston tashmë.");

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = dto.Password,
                Roles = dto.Roles
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Përdoruesi u krijua me sukses.",
                user = new { employee.FirstName, employee.LastName, employee.Email, employee.Roles }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == login.Email && e.Password == login.Password);

            if (employee == null)
                return Unauthorized("Email i pasaktë ose punonjësi nuk ekziston.");

            var token = GenerateJwtToken(employee);

            return Ok(new
            {
                Token = token,
                User = new { employee.FirstName, employee.LastName, employee.Roles }
            });
        }

        private string GenerateJwtToken(Employee employee)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("employeeId",              employee.Id.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
                    new Claim(ClaimTypes.Email,          employee.Email),
                    new Claim(ClaimTypes.Role,           employee.Roles.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryInMinutes"]!)),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}