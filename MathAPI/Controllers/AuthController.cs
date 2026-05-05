using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Firebase.Auth;
using MathAPI.Models;
using MathAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace MathAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        
 private readonly IConfiguration _configuration;
        private readonly FirebaseAuthProvider _auth;
        private readonly byte[] _key;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;

            var firebaseApiKey = _configuration["FirebaseMathApp"];
            var jwtKey = _configuration["MathAppJwtKey"];

            _auth = new FirebaseAuthProvider(new FirebaseConfig(firebaseApiKey));
            _key = Encoding.ASCII.GetBytes(jwtKey);
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(LoginModel login)
        {
            try
            {
                await _auth.CreateUserWithEmailAndPasswordAsync(login.Email, login.Password);

                var fbAuthLink = await _auth.SignInWithEmailAndPasswordAsync(login.Email, login.Password);
                string currentUserId = fbAuthLink.User.LocalId;
                string currentUserEmail = fbAuthLink.User.Email;

                if (currentUserId != null)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, currentUserId),
                        new Claim(ClaimTypes.Email, currentUserEmail),
                        new Claim("UserId", currentUserId)
                    };

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(claims),
                        Expires = DateTime.UtcNow.AddDays(1),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var tokenString = tokenHandler.WriteToken(token);

                    return Ok(new AuthResponse(tokenString,currentUserId));
                }
            }
            catch (FirebaseAuthException ex)
            {
                var firebaseEx = JsonConvert.DeserializeObject<FirebaseErrorModel>(ex.ResponseData);
                return Unauthorized(firebaseEx.error.code + " - " + firebaseEx.error.message);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
            return BadRequest("Something went wrong");

        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginModel login)
        {
            try
            {
                var fbAuthLink = await _auth.SignInWithEmailAndPasswordAsync(login.Email, login.Password);
                string currentUserId = fbAuthLink.User.LocalId;
                string currentUserEmail = fbAuthLink.User.Email;

                if (currentUserId != null)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, currentUserId),
                        new Claim(ClaimTypes.Email, currentUserEmail),
                        new Claim("UserId", currentUserId)
                    };

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(claims),
                        Expires = DateTime.UtcNow.AddDays(1),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var tokenString = tokenHandler.WriteToken(token);

                    return Ok(new AuthResponse(tokenString,currentUserId));
                }
            }
            catch (FirebaseAuthException ex)
            {
                var firebaseEx = JsonConvert.DeserializeObject<FirebaseErrorModel>(ex.ResponseData);
                AuthLogger.Instance.LogError(firebaseEx.error.message + " - User: " + login.Email + " - IP: " + HttpContext.Connection.RemoteIpAddress
                    + " - Browser: " + Request.Headers.UserAgent);
                return Unauthorized(firebaseEx.error.code + " - " + firebaseEx.error.message);
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }

            return BadRequest("Something went wrong");
        }

        [HttpPost("Logout")]
        public IActionResult LogOut()
        {            
            return Ok();
        }
    }
}