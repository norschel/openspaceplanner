using System.Threading.Tasks;

namespace openspace.Services
{
    public interface ICalendarServiceV2
    {
        Task<string> GetSessionsAsync();

        Task<string> GetSessionsAsync(params int[] ids);
    }
}
