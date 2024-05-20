using BackendCarWash.Model;
using BackendCarWash.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;



namespace BackendCarWash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUser _user;
        public UserController(IUser user)
        {
            _user = user;
        }
        
        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var users = await _user.GetAll();
            if (users == null)
            {
                return NotFound();
            }
            return Ok(users);
        }
        [HttpGet("{Id}")]
        public async Task<ActionResult<User>> GetByID(int Id)
        {
            var users = await _user.GetByID(Id);
            if (users == null)
            {
                return NotFound();
            }
            return Ok(users);
        }
        [HttpPost]
        public async Task<ActionResult<User>> AddUser(User user)
        {
            if (user == null)
            {
                if (await _user.CheckEmailExistAsync(user.Email))
                {
                    return BadRequest();
                }
                return BadRequest(new { Message = "Email alredy exists..!" });
            }
            var add = await _user.AddUser(user);
            return Ok(new
            {
                Message = "Registration Successfull"
            });
            if (user == null)
            {
                return BadRequest();
            }
        }
        [HttpPut("{Id}")]
        public async Task<ActionResult> UpdateUser(int Id, User user)
        {
            var users = await _user.UpdateUser(Id, user);
            return CreatedAtAction(nameof(GetByID), new { id = users.ID }, users);
        }
        [HttpDelete("{Id}")]
        public async Task<ActionResult> DeleteUser(int Id)
        {
            await _user.DeleteUser(Id);
            return Ok();
        }
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] Login login)
        {
            if (login == null)
            {
                return BadRequest();
            }
            var u = await _user.Login(login);
            if (u == null)
            {
                return NotFound(new { Message = "User not found" });
            }
            /*return Ok(new { Message = "Login success" })*/;
            string Token = CreateJwtToken(u);
            return Ok(new
            {
                Token,
                Message = "Login Successfull"
            });

        }



        
        [HttpGet("EmailService")] public IActionResult SendEmail(string name, string receiver)
        {
            string body = "Thanks " + name + "!\n\n Your email id " + receiver + " is succesfully registered with" +
            "CarWashExpress \n\n Regards,\n CarWashExpress Ltd.\n Contact us: carwash240130@gmail.com";
            var email = new MimeMessage(); email.From.Add(MailboxAddress.Parse("carwash240130@gmail.com"));
            email.To.Add(MailboxAddress.Parse(receiver));
            email.Subject = "Registration comfirmation mail-CarWashExpress";
            email.Body = new TextPart(TextFormat.Plain) { Text = body };
            using var smtp = new SmtpClient(); smtp.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls); smtp.Authenticate("carwash240130@gmail.com", "olshcfchzcuoylcw"); //email and password
            smtp.Send(email);
            smtp.Disconnect(true); return Ok("Mail Sended ");
        }


        private async Task<ActionResult> CheckEmailExistAsync(string Email)
        {
            var check = await _user.CheckEmailExistAsync(Email);
            return Ok();
        }
        //jwt token create
        private string CreateJwtToken(User jwt)
        {
            var jwtTokenhandler = new JwtSecurityTokenHandler();
            var Key = Encoding.ASCII.GetBytes("veryverysecret.....");
            var identity = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Role,jwt.Role),
                    new Claim(ClaimTypes.Name, $"{jwt.FirstName} {jwt.LastName}")
                });
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256);
            var Tokendescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };
            var token = jwtTokenhandler.CreateToken(Tokendescriptor);
            return jwtTokenhandler.WriteToken(token);

        }
    }
}
