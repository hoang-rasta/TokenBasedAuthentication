using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ResourceClient
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await CreateAndConsumeByAccessRefreshToken.Run();
        }

    }
}
