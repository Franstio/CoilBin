namespace CoilBin.RackApi.Models
{
    public class EmployeeModel
    {
        public string badgeId { get; set; }
        public string username { get; set; } = null!;
        public bool isActive { get; set; } = false;

    }
}
