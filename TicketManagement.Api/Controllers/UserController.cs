using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManagement.Api.DTOs;
using TicketManagement.Data.Models;
using TicketManagement.Data.Repositories;
using Serilog;

namespace TicketManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("signup")]
        public async Task<ActionResult<LoginResponseDto>> Register(UserRegisterDto userRegisterDto)
        {
            Log.Information("Attempting to register new user with email: {Email}", userRegisterDto.Email);

            // Check if user already exists
            if (await _userRepository.UserExistsAsync(userRegisterDto.Email))
            {
                Log.Warning("Registration failed: Email {Email} already exists", userRegisterDto.Email);
                return BadRequest(new LoginResponseDto 
                { 
                    Success = false, 
                    Message = "Email already registered" 
                });
            }

            // Create new user
            var user = new User
            {
                Name = userRegisterDto.Name,
                Email = userRegisterDto.Email,
                Password = userRegisterDto.Password,
                IsAdmin = false // Normal user
            };

            var createdUser = await _userRepository.CreateUserAsync(user);
            Log.Information("Successfully registered new user {UserId} with email {Email}", createdUser.UserId, createdUser.Email);

            return Ok(new LoginResponseDto
            {
                Success = true,
                Message = "User registered successfully",
                User = new UserDto
                {
                    UserId = createdUser.UserId,
                    Name = createdUser.Name,
                    Email = createdUser.Email,
                    IsAdmin = createdUser.IsAdmin
                }
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(UserLoginDto userLoginDto)
        {
            Log.Information("Login attempt for user {Email}", userLoginDto.Email);

            // Authenticate user
            var user = await _userRepository.AuthenticateUserAsync(userLoginDto.Email, userLoginDto.Password);

            if (user == null)
            {
                Log.Warning("Failed login attempt for user {Email}", userLoginDto.Email);
                return Unauthorized(new LoginResponseDto 
                { 
                    Success = false, 
                    Message = "Invalid email or password" 
                });
            }

            Log.Information("Successful login for user {UserId} with email {Email}", user.UserId, user.Email);
            return Ok(new LoginResponseDto
            {
                Success = true,
                Message = "Login successful",
                User = new UserDto
                {
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                }
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            Log.Information("Retrieving all users");
            var users = await _userRepository.GetAllUsersAsync();
            Log.Information("Retrieved {Count} users", users.Count());
            
            return Ok(users.Select(u => new UserDto
            {
                UserId = u.UserId,
                Name = u.Name,
                Email = u.Email,
                IsAdmin = u.IsAdmin
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            
            if (user == null)
                return NotFound();

            return Ok(new UserDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                IsAdmin = user.IsAdmin
            });
        }
    }
}