using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.ResourceServer.Models;

namespace WebApi.ResourceServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Secures all endpoints in this controller
    public class ProductsController : ControllerBase
    {
        // In-memory list to store products
        private static readonly List<Product> Products = new List<Product>
        {
            new Product { Id = 1, Name = "Product A", Price = 10.0M, Description = "Test Product A" },
            new Product { Id = 2, Name = "Product B", Price = 20.0M, Description = "Test Product B"  },
            new Product { Id = 3, Name = "Product C", Price = 30.0M, Description = "Test Product C"  }
        };

        private static int _nextId = 4; // To auto-increment product IDs

        // Retrieves all products.
        [HttpGet("GetAll")]
        public ActionResult<List<Product>> GetAllProduct()
        {
            return Ok(Products);
        }

        // Retrieves a specific product by ID.
        [HttpGet("GetById/{id}", Name = "GetProductById")]
        public ActionResult<Product> GetProductById(int id)
        {
            var product = Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            return Ok(product);
        }

        // Creates a new product.
        [HttpPost("Add")]
        public IActionResult AddProduct([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            product.Id = _nextId++;
            Products.Add(product);

            return CreatedAtRoute("GetProductById", new { id = product.Id }, product);
        }

        // Updates an existing product. Only accessible by Admins.
        [HttpPut("Update/{id}")]
        public IActionResult UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingProduct = Products.FirstOrDefault(p => p.Id == id);
            if (existingProduct == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;

            return NoContent();
        }

        // Deletes a product by ID. Only accessible by Admins.
        [HttpDelete("Delete/{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            Products.Remove(product);

            return NoContent();
        }
    }
}
