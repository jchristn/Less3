namespace Less3.Database
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enumeration containing the supported database types.
    /// </summary>
    public enum DatabaseTypeEnum
    {
        /// <summary>
        /// SQLite.
        /// </summary>
        [EnumMember(Value = "Sqlite")]
        Sqlite,

        /// <summary>
        /// Microsoft SQL Server.
        /// </summary>
        [EnumMember(Value = "SqlServer")]
        SqlServer,

        /// <summary>
        /// MySQL.
        /// </summary>
        [EnumMember(Value = "Mysql")]
        Mysql,

        /// <summary>
        /// PostgreSQL.
        /// </summary>
        [EnumMember(Value = "Postgresql")]
        Postgresql
    }
}
