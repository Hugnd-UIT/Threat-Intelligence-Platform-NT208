using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly UsersService _usersService;

        public UserController(UsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var UserList = await _usersService.GetAllUsersAsync();
            return Ok(UserList);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User NewUser)
        {
            if (string.IsNullOrWhiteSpace(NewUser.username) || string.IsNullOrWhiteSpace(NewUser.password))
            {
                return BadRequest(new 
                { 
                    Message = "Username and password cannot be empty!" 
                });
            }

            try
            {
                await _usersService.CreateUserAsync(NewUser);

                return Ok(new 
                { 
                    Message = "Account created successfully!" 
                });
            }
            catch (InvalidOperationException ExceptionInstance)
            {
                return StatusCode(409, new 
                { 
                    Message = ExceptionInstance.Message 
                });
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new 
                { 
                    Message = "Error creating User: " + ExceptionInstance.Message 
                });
            }
        }

        [HttpPut("{Key}")]
        public async Task<IActionResult> Update(string Key, [FromBody] User UpdateData)
        {
            if (string.IsNullOrWhiteSpace(UpdateData.username))
            {
                return BadRequest(new 
                { 
                    Message = "Username cannot be empty!" 
                });
            }

            try
            {
                await _usersService.UpdateUserAsync(Key, UpdateData);

                return Ok(new 
                { 
                    Message = "Account updated successfully!" 
                });
            }
            catch (InvalidOperationException ExceptionInstance)
            {
                return StatusCode(409, new 
                { 
                    Message = ExceptionInstance.Message 
                });
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new 
                { 
                    Message = "Error updating User: " + ExceptionInstance.Message 
                });
            }
        }

        [HttpDelete("{Key}")]
        public async Task<IActionResult> Delete(string Key)
        {
            try
            {
                var CurrentUsername = User.Identity?.Name;
                await _usersService.DeleteUserAsync(Key, CurrentUsername);

                return Ok(new 
                { 
                    Message = "Account deleted successfully!" 
                });
            }
            catch (KeyNotFoundException ExceptionInstance)
            {
                return NotFound(new 
                { 
                    Message = ExceptionInstance.Message 
                });
            }
            catch (InvalidOperationException ExceptionInstance)
            {
                return BadRequest(new 
                { 
                    Message = ExceptionInstance.Message 
                });
            }
            catch (Exception ExceptionInstance)
            {
                return StatusCode(500, new 
                { 
                    Message = "System error: " + ExceptionInstance.Message 
                });
            }
        }
    }
}