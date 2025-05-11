using WebApplication1.Model;

namespace WebApplication1.Services;

public interface IDbService
{
    Task<int> AddProductToWarehouse(AddProductRequest request);
}