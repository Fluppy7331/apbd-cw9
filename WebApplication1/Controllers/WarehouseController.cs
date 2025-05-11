using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TutorialWebApp.Exceptions;
using WebApplication1.Model;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IDbService _productService;
        public WarehouseController(IDbService productService)
        {
            _productService = productService;
        }
        [HttpPost("add-product")]
        public async Task<IActionResult> AddProductToWarehouse([FromBody] AddProductRequest request)
        {
            if (request.idProduct <= 0 || request.idWarehouse <= 0 || request.amount <= 0)
            {
                return BadRequest("IdProduct, IdWarehouse oraz Amount musi byc większe niż 0.");
            }
            if (request.createdAt == default)
            {
                return BadRequest("Pole 'createdAt' musi być poprawnie wypełnione.");
            }
            
            try
            {
                int newProductId = await _productService.AddProductToWarehouse(request);
                return CreatedAtAction(nameof(AddProductToWarehouse), new { id = newProductId });
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (ConflictException e)
            {
                return Conflict(e.Message);
            }
        }
    }
}
