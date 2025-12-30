using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project_Backend.Models.Entities
{
    [Table("store")]
    public class Store
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    }
}
