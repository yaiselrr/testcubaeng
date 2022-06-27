using API.Models;
using System.Threading.Tasks;

namespace API.Repository.Users
{
    public interface IUserRepository
    {
        Task<int> Register(User user, string password, string repeatPassword);
        Task<string> Login(string userName, string password);
        Task<string> ConfirmEmail(int id);
    }
}
