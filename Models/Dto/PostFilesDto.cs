using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace API.Models.Dto
{
    public class PostFilesDto
    {
        public List<IFormFile> Files { get; set; }
    }
}
