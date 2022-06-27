using API.Data;
using API.Models;
using API.Models.Dto;
using API.Repository.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileRepositorycs _fileRepositorycs;
        protected ResponseDto _response;

        private readonly ApplicationDBContext _db;
        //private readonly ILogger<FileRepository> _logger;
        private readonly IConfiguration _configuration;

        public FilesController(IFileRepositorycs fileRepositorycs, ApplicationDBContext db, IConfiguration configuration)
        {
            _fileRepositorycs = fileRepositorycs;
            _response = new ResponseDto();
            _db = db;
            _configuration = configuration;
        }

        [HttpGet("GetFiles")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFiles()
        {
            var response = await _fileRepositorycs.GetFiles();

            if (response.Count < 1)
            {
                _response.DisplayMessage = "No find files.";
                _response.Result = response;

                return Ok(_response);
            }

            _response.DisplayMessage = "Success get files.";
            _response.Result = response;

            return Ok(_response);
        }

        [HttpPost("PostFiles")]
        [Authorize]
        public async Task<ActionResult> PostFiles(List<IFormFile> files) 
        {
            var response = await _fileRepositorycs.PostFiles(files);

            if (response == "ok")
            {
                _response.DisplayMessage = "Success uploads files.";
                _response.Result = response;

                return Ok(_response);
            }
            else
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Wrong uploads files.";

                return BadRequest(_response);

            }
            
        }

        [HttpPost("DownloadFile")]
        [AllowAnonymous]
        public async Task<ActionResult> DownloadFile(DownloadFileDto model)
        {
            var response = await _fileRepositorycs.DownloadFile(model.Id);

            if (response == "ok")
            {
                _response.DisplayMessage = "Success download file.";
                _response.Result = response;

                return Ok(_response);
            }
            if (response == "nozip")
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Wrong download file no zip.";
                _response.Result = response;

                return BadRequest(_response);
            }
            else
            {
                _response.IsSuccess = false;
                _response.DisplayMessage = "Wrong download file.";

                return BadRequest(_response);

            }

        }

    }
}
