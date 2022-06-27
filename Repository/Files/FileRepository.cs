using API.Data;
using API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace API.Repository.Files
{
    public class FileRepository : IFileRepositorycs
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<FileRepository> _logger;
        private readonly IConfiguration _configuration;

        public FileRepository(ApplicationDBContext db, ILogger<FileRepository> logger, IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> DownloadFile(int id)
        {
            try
            {
                Models.File file = await _db.Files.FindAsync(id);

                if (file == null)
                {
                    return "nofile";
                }

                if (!file.Extension.Equals("zip"))
                {
                    return "nozip";
                }

                String newFolder = _configuration["ReturnPaths:Downloads"];

                if (!Directory.Exists(newFolder))
                {
                    Directory.CreateDirectory(newFolder);
                }

                string destino = Path.Combine(_configuration["ReturnPaths:Downloads"], file.Name +"."+ file.Extension); 

                WebClient webClient = new WebClient();
                webClient.DownloadFile(file.Path, destino);

                return "ok";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<List<Models.File>> GetFiles()
        {
            _logger.LogInformation("Get files list");

            try
            {
                List<Models.File> list = await _db.Files.ToListAsync();

                return list;
            }
            catch (System.Exception)
            {
                _logger.LogCritical("Not find files");

                return null;
            }
        }

        public async Task<string> PostFiles(List<IFormFile> files)
        {
            _logger.LogInformation("Upload files DB");

            List<Models.File> filesList = new List<Models.File>();

            try
            {
                if (files.Count() > 0)
                {
                    foreach (var file in files)
                    {
                        var filePath = _configuration["ReturnPaths:Uploads"] + file.FileName;

                        using (var stream = System.IO.File.Create(filePath))
                        {
                           await file.CopyToAsync(stream);
                        }

                        double size = file.Length;
                        size = size / 1000000;
                        size = Math.Round(size, 2);

                        Models.File fileEnd = new Models.File();

                        fileEnd.Extension = System.IO.Path.GetExtension(file.FileName).Substring(1);
                        fileEnd.Name = System.IO.Path.GetFileNameWithoutExtension(file.FileName);
                        fileEnd.Size = size;
                        fileEnd.Path = filePath;

                        filesList.Add(fileEnd);

                        var outFileName = fileEnd.Name + ".zip";
                        var fileNameToAdd = System.IO.Path.Combine(_configuration["ReturnPaths:Uploads"], file.FileName);
                        var zipFileName = System.IO.Path.Combine(_configuration["ReturnPaths:UploadsZip"], outFileName);

                        //Crear el archivo (si quieres puedes editar uno existente cambiando el modo a Update.
                        using (ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Create))
                        {
                            archive.CreateEntryFromFile(fileNameToAdd, System.IO.Path.GetFileName(fileNameToAdd));
                        }

                        Models.File fileEndZip = new Models.File();

                        fileEndZip.Extension = System.IO.Path.GetExtension(outFileName).Substring(1);
                        fileEndZip.Name = System.IO.Path.GetFileNameWithoutExtension(outFileName);
                        fileEndZip.Size = size;
                        fileEndZip.Path = _configuration["ReturnPaths:UploadsZip"]+ outFileName;

                        await _db.Files.AddAsync(fileEndZip);
                        _db.SaveChanges();
                    }

                    _db.Files.AddRange(filesList);
                    _db.SaveChanges();

                    _logger.LogInformation("Success add files DB");
                }

                _logger.LogInformation("No select files");
                return "ok";
            }
            catch (System.Exception ex)
            {

                _logger.LogCritical("Not add files DB");

                return ex.Message;
            }
        }
    }
}
