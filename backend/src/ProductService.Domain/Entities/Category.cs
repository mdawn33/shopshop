namespace ProductService.Domain.Entities;

/// <summary>
/// Represents a hierarchical product category.
/// Root categories have <see cref="ParentCategoryId"/> set to null.
/// </summary>
public class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Null for root categories; references the parent category otherwise.
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Category? ParentCategory { get; set; }

    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
