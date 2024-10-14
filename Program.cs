using LinqToDB.Mapping;
using LinqToDB;
using System;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlCe;
using System.Linq;
using LinqToDB.DataProvider.SQLite;

namespace ProcessKillTest
{
    [Table(Name = "Payments")]
    public sealed class Payment
    {
        [Column(IsPrimaryKey = true)]
        [Column(DbType = "TEXT", Configuration = ProviderName.SQLite)]
        public Guid Id { get; set; }

        [Column(DbType = "nvarchar(255)", CanBeNull = true)]
        public string Comment { get; set; }

        [Column] public DateTime Date { get; set; }

        [Column(Name = "at_sum", DbType = "numeric(29,4)", CanBeNull = true, Configuration = ProviderName.SqlCe)]
        [Column(Name = "at_sum", DbType = "numeric(15,4)", CanBeNull = true, Configuration = ProviderName.SQLite)]
        public decimal Sum { get; set; }
    }

    internal class Program
    {
        private const string ConnectionStringFormat =
            "Persist Security Info = False; Data Source = {0}; File Mode = 'Read Write'; Max Database Size = 2048; SSCE:Default Lock Timeout=300000; Flush interval = 1";

        private const string fileName = "test.sdf";

        static void Main()
        {
            var connection = new SqlCeConnection(string.Format(ConnectionStringFormat, fileName));
            CreateDatabaseIfNeeded(connection);
            connection.Open();

            while (true)
            {
                Console.WriteLine("Input w [write to a database], r [check database record count] or q [exit]:");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.W:
                        WriteToDatabase(connection);
                        break;
                    case ConsoleKey.R:
                        CheckRecordsCount(connection);
                        break;
                    case ConsoleKey.Q:
                        return;
                        break;
                    default:
                        Console.WriteLine($"{Environment.NewLine}Wrong key!");
                        break;
                }
            }
        }


        private static void CreateDatabaseIfNeeded(SqlCeConnection connection)
        {
            using (var db = new DataConnection(SqlCeTools.GetDataProvider(), connection))
            {
                if (!File.Exists(fileName))
                {
                    SqlCeTools.CreateDatabase(fileName);
                    db.CreateTable<Payment>();
                }
            }
        }

        private static void WriteToDatabase(SqlCeConnection connection)
        {
            using (var db = new DataConnection(SqlCeTools.GetDataProvider(), connection))
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var recordToAdd = new Payment { Id = Guid.NewGuid(), Date = DateTime.Now, Sum = 100.0m, Comment = "Test" };
                        db.Insert(recordToAdd);
                    }

                    transaction.Commit(CommitMode.Immediate);
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    Console.WriteLine(e);
                    throw;
                }
            }
            //if you remove Process.Kill() then CheckRecordsCount() returns 5 records
            Process.GetCurrentProcess().Kill();
        }


        private static void CheckRecordsCount(SqlCeConnection connection)
        {
            using (var db = new DataConnection(SQLiteTools.GetDataProvider(), connection))
            {
                var recCount = db.GetTable<Payment>().Count();
                Console.WriteLine($"{Environment.NewLine}Records count in database: {recCount}");
                if (recCount != 5)
                    Console.WriteLine("!!!Missing records!!!");
            }
        }
    }
}
