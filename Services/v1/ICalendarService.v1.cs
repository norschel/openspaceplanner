using System.Threading.Tasks;

namespace openspace.Services
{
    public interface ICalendarServiceV1
    {
        Task<string> GetSessionsAsync();

        Task<string> GetSessionsAsync(params int[] ids);
    }
}
