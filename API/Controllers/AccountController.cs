using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace API.Controllers
{
    public class AccountController(DataContext context,ITokenServices tokenServices) : BaseApiController
    {
        [HttpPost("register")]// account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if(await UserExists(registerDto.UserName))
                return BadRequest("UserName is Already taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = registerDto.UserName.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return new UserDto
            {
                UserName = user.UserName,
                Token = tokenServices.CreateToken(user)
            };

        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto )
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.UserName == loginDto.UserName.ToLower());
            if (user == null) return Unauthorized("Invalid UserName");

            using var hamc = new HMACSHA512(user.PasswordSalt);

            var computedHash = hamc.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if(user.PasswordHash[i] != computedHash[i])
                    return Unauthorized("Invalid Password.");
            }

            return new UserDto
            {
                UserName = user.UserName,
                Token = tokenServices.CreateToken(user)
            };
        }
        private async Task<bool> UserExists(string userName)
        {
            return await context.Users.AnyAsync(user => user.UserName.ToLower() == userName.ToLower());
        }
    }
}
