namespace DrHouse.SqlServer
{
    public class TablePermission
    {
        public string TableName { get; set; }

        public Permission Permission { get; set; }

        public bool HasPermission { get; set; }
    }
}
