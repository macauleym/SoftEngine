using SoftEngine.Models;

namespace SoftEngine.Interfaces.Core.Importing
{
    public interface ILoadMesh
    {
        Task<Mesh[]> LoadJsonFileAsync(string fileName);
    }
}
