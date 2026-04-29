using System.ServiceProcess;

namespace Contoso.FileProcessor
{
    internal static class Program
    {
        private static void Main()
        {
            ServiceBase.Run(new ServiceBase[] { new FileProcessorService() });
        }
    }
}
