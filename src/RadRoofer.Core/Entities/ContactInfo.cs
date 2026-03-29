using System.ComponentModel.DataAnnotations;

namespace RadRoofer.Core.Entities;

public class ContactInfo : BaseEntity
{
    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Email { get; set; }
}
