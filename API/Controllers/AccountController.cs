using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
    public class AccountController : BaseAPIController {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountController (DataContext context, ITokenService tokenService) {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost ("register")]
        public async Task<ActionResult<UserDTO>> Register (RegisterDTO registerDTO) {
            if (await UserExists (registerDTO.UserName))
                return BadRequest ("帳號已存在");

            var hmac = new HMACSHA512 ();

            var user = new AppUser {
                UserName = registerDTO.UserName.ToLower (),
                PasswordHash = hmac.ComputeHash (Encoding.UTF8.GetBytes (registerDTO.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add (user);
            await _context.SaveChangesAsync ();
            return new UserDTO {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists (string userName) {
            return await _context.Users.AnyAsync (x => x.UserName == userName.ToLower ());
        }

        [HttpPost ("login")]
        public async Task<ActionResult<UserDTO>> Login (LoginDTO loginDTO) {
            var user = await _context.Users
                .SingleOrDefaultAsync (x => x.UserName == loginDTO.UserName);

            if (user == null) return Unauthorized ("帳號未註冊");

            var hmac = new HMACSHA512 (user.PasswordSalt);
            var computedHash = hmac.ComputeHash (Encoding.UTF8.GetBytes (loginDTO.Password));
            for (int i = 0; i < computedHash.Length; i++) {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized ("密碼錯誤");
            }

            return new UserDTO {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }
    }
}