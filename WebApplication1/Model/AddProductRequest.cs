using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model;

public class AddProductRequest
{
    public int idProduct { get; set; }
    public int idWarehouse { get; set; }
    public int amount { get; set; }
    public DateTime createdAt { get; set; }
}