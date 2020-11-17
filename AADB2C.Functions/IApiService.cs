using System.Threading.Tasks;

namespace AADB2C.Functions
{
    public interface IApiService
    {
        Task<string> CallSecureApiAsync(string accessToken);
    }
}
