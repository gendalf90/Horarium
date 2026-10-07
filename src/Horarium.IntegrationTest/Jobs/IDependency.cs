using System.Threading.Tasks;

namespace Horarium.IntegrationTest.Jobs
{
    public interface IDependency
    {
        Task Call(string param);
    }
}