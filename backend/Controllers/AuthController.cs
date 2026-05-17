using Microsoft.AspNetCore.Mvc;
using backend.Services;
using System;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest LoginRequestData)
        {
            try 
            {
                var Token = await _authService.LoginAsync(LoginRequestData.Username, LoginRequestData.Password);

                if (Token == null)
                {
                    return StatusCode(401, new 
                    { 
                        Message = "Invalid username or password!" 
                    });
                }

                return Ok(new 
                { 
                    Token = Token 
                });
            }
            catch (UnauthorizedAccessException ExceptionInstance)
            {
                return StatusCode(403, new 
                { 
                    Message = ExceptionInstance.Message 
                });
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new 
                { 
                    Message = $"System error: {ExceptionInstance.Message}" 
                });
            }
        }
    }

    public class LoginRequest 
    { 
        public string Username 
        { 
            get; 
            set; 
        } 
        
        public string Password 
        { 
            get; 
            set; 
        } 
    }
}