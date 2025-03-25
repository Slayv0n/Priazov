namespace DataBase.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Industry Industry { get; set; } = null!;
        public Region Region { get; set; } = null!;
        private uint _status;
        public uint Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value >= 0 && value <= 100)
                {
                    _status = value;
                }
            }
        }
    }
}
