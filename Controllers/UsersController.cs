using API.Models;
using API.Models.Dto;
using API.Repository.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        protected ResponseDto _response;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _response = new ResponseDto();
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(UserRegisterDto model)
        {
            var response = await _userRepository.Register(new User { UserName = model.UserName, Email = model.Email }, model.Password, model.RepeatPassword);

            if (response == -1)
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "User exist.";

                return BadRequest(_response);
            }

            if (response == -2)
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Email exist.";

                return BadRequest(_response);
            }

            if (response == -3)
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Password no equals.";

                return BadRequest(_response);
            }

            if (response == -4)
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Wrong send email.";

                return BadRequest(_response);
            }

            _response.DisplayMessage = "Success create user.";
            _response.Result = response;

            return Ok(_response);
        }

        [HttpPost("ConfirmEmail")]
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(ConfirmEmailDto model)
        {
            var response = await _userRepository.ConfirmEmail(model.Id);

            if (response == "nouser")
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "No User Exist";
                return BadRequest(_response);
            }
            else
            {
                _response.DisplayMessage = "Success confirm email";
                _response.Result = response;

                return Ok(_response);
            }
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login(UserLoginDto model) 
        { 
            var response = await _userRepository.Login(model.UserName, model.Password);

            if (response == "nouser")
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "No User";
                return BadRequest(_response);
            }

            if (response == "wrongpassword")
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Wrong password";
                return BadRequest(_response);
            }

            if (response == "noconfirm")
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "no confirm user";
                return BadRequest(_response);
            }

            _response.DisplayMessage = "Success login";
            _response.Result = response;

            return Ok(_response);
        }  
    }
}
