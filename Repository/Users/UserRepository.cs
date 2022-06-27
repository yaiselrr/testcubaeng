using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Repository.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<UserRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly string registerNotificationHtmlBody = @"<table style=""max-width: 600px;padding: 10px;margin: 0 auto;border-collapse: collapse;"">
		
		
		<tr class=""mb-4"">
			<td>
				<div style=""color: #34495e;margin: 4% 10% 2%; text-align: justify;font-family: sans-serif;"">
					<h2 style=""color: #0099a8;margin: 0 0 7px;"">Estimada/o usuaria/o</h2>
					<p style=""margin: 2px; font-size: 15px;"">
						Se registró el alta de usuario, para poder continuar, por favor dar click en el siguiente enlace:
					</p>
					<p style =""margin: 2px; font-size: 15px;"">
                        Confirmación de registro
                    </p>
					<p style = ""margin: 2px; font-size: 15px;"">
                        @UrlLink
                    </p>
					<p style =""margin: 2px; font-size: 15px;"">
                        @Register
                    </p>
                </div>		
			</td>
		</tr>	
	</table>";

        public UserRepository(ApplicationDBContext db, ILogger<UserRepository> logger, IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<int> Register(User user, string password, string repeatPassword)
        {
            _logger.LogInformation("Begin Register");

            try
            {
                if (await UserExist(user.UserName))
                {
                    _logger.LogError("User exist.");

                    return -1;                    
                }

                if (await EmailExist(user.Email))
                {
                    _logger.LogError("Email exist.");

                    return -2;
                }

                if (!PasswordEquals(password, repeatPassword))
                {
                    _logger.LogError("Password no equals.");

                    return -3;
                }

                CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                await _db.AddAsync(user);
                await _db.SaveChangesAsync();               
                                
                if (await BuildEmail(user))
                {
                    _logger.LogInformation("Success Register");

                    return user.Id;
                }
                else
                {
                    return -4;
                }               
                
            }
            catch (System.Exception)
            {
                _logger.LogCritical("Not register");

                return -500;
            }
        }

        private async Task<bool> UserExist(string userName)
        {
            _logger.LogInformation("Validate user exist");

            if (await _db.Users.AnyAsync(u => u.UserName.ToLower().Equals(userName.ToLower())))
            {
                _logger.LogInformation("User exist");

                return true;
            }
            _logger.LogInformation("User no exist");

            return false;
        }

        private async Task<bool> EmailExist(string emailUser)
        {
            _logger.LogInformation("Validate email exist");

            if (await _db.Users.AnyAsync(u => u.Email.ToLower().Equals(emailUser.ToLower())))
            {
                _logger.LogInformation("Email exist");

                return true;
            }
            _logger.LogInformation("Email no exist");

            return false;
        }

        private bool PasswordEquals(string password, string repeatPassword)
        {
            _logger.LogInformation("Validate passwords equals");

            if (password.ToLower().Equals(repeatPassword.ToLower()))
            {
                _logger.LogInformation("Passwords equals");

                return true;
            }
            _logger.LogInformation("Passwords no equals");

            return false;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            _logger.LogInformation("Encoding password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private async Task<bool> SendEmailAsync(string fromAddress, string toAddress, string subject, string message)
        {
            _logger.LogInformation("Send mail to user");

            MailMessage mailMessage = new MailMessage(fromAddress, toAddress, subject, message);

            mailMessage.Body = message;
            mailMessage.IsBodyHtml = true;

            using (var client = new SmtpClient(_configuration["SMTP:Host"], int.Parse(_configuration["SMTP:Port"]))
            {
                Credentials = new NetworkCredential(_configuration["SMTP:Username"], _configuration["SMTP:Password"])
            })
            {
                await client.SendMailAsync(mailMessage);

                return true;
            }

        }

        private async Task<bool> BuildEmail(User user)
        {
            var unRealToken = "CfDJ8NS42a7j+69Lj8N3z0WunckgFH8HsHJ6+yvcZhYONPrpnn1SWXsfPwcOcjAsNdFEBJ+FXEP3Hd7cayuaUL0qv7zL4+JhtDwNKeiVCQYzSCkcZKYcOx7ZTyJK9ExmTcP5rIw9wsh8LcpWnQLRKRMDAmI2ztKQEdSeFJH0vTvKs2p99sPuAR5GQcRduae2PCCqMH7RNknbQJzXQ+csVrR/";

            var uriBuilder = new UriBuilder(_configuration["ReturnPaths:ConfirmEmail"]) + unRealToken + user.Id;

            var htmlUrl = registerNotificationHtmlBody.Replace("@UrlLink", uriBuilder.ToString()).Replace("@Register", new UriBuilder(_configuration["ReturnPaths:Register"]).ToString());
            var htmlUrlEnd = htmlUrl.Replace("@UrlLinkRegister", _configuration["ReturnPaths:Register"]);

            var urlString = htmlUrlEnd;

            var senderEmail = _configuration["ReturnPaths:SenderEmail"];

            if (!await SendEmailAsync(senderEmail, user.Email, "Correo de confirmación del registro de usuario", urlString))
            {
                _logger.LogError("Wrong send email");

                return false;
            }
            _logger.LogInformation("Success send email");

            return true;

        }

        public async Task<string> Login(string userName, string password)
        {
            _logger.LogInformation("Begin login user");

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName.ToLower().Equals(userName.ToLower()));

                if (user == null)
                {
                    _logger.LogError("No user.");

                    return "nouser";
                }

                if (user.ConfirmEmail == false)
                {
                    return "noconfirm";
                }

                if (!ValidatePasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    _logger.LogError("Wrong Password.");

                    return "wrongpassword";
                }
                else
                {
                    return CreateToken(user);
                }
            }
            catch (Exception)
            {

                _logger.LogCritical("No Login");

                return "nologin";
            }            
        }

        public bool ValidatePasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            _logger.LogInformation("Validate password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedhash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                for (int i = 0; i < computedhash.Length; i++)
                {
                    if (computedhash[i] != passwordHash[i])
                    {
                        _logger.LogError("Wrong password");

                        return false;
                    }
                }
                _logger.LogInformation("Success password");

                return true;
            }
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["Token:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = System.DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<string> ConfirmEmail(int id)
        {
            _logger.LogInformation("Begin confirm email");

            try
            {
                var user = await _db.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogError("User no exist");

                    return "nouser";
                }
                else
                {
                    user.ConfirmEmail = true;

                    _db.Users.Update(user);

                    await _db.SaveChangesAsync();

                    return "ok";
                }
            }
            catch (Exception)
            {

                _logger.LogCritical("Not confirm email");

                return "noconfirm";
            }
        }
    }
}
