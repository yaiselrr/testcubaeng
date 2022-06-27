using API.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Repository.Files
{
    public interface IFileRepositorycs
    {
        Task<List<File>> GetFiles();
        Task<string> PostFiles(List<IFormFile> files);
        Task<string> DownloadFile(int id);
    }
}
