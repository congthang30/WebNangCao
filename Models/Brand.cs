using System;
using System.Collections.Generic;

namespace NewWeb.Models;

public partial class Brand
{
    public int BrandId { get; set; }

    public string? NameBrand { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
