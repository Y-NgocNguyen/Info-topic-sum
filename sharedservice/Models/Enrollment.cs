namespace sharedservice.Models
{
    public partial class Enrollment
    {
        public int Id { get; set; }
        public int CouresId { get; set; }
        public string UserId { get; set; }
        public DateTime EnrolledDate { get; set; }

        /* public virtual Course Coures { get; set; } = null!;*/
    }
}