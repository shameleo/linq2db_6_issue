using LinqToDB;
using LinqToDB.Data;
using System.Linq.Expressions;

namespace l2db_bug
{
    class Db : DataConnection
    {
        public Db(string path) :
            base("SQLite", $@"Data Source = {path}; foreign keys = true; Version = 3;")
        { }
    }

    public class Service
    {
        public int IdContract { get; set; }
    }

    class Contract
    {
        public int Id {get; set; }
    }

    class ServiceProjection
    {
        public int IdContract {get;set;}
    }

    class Program
    {
        static void Main(string[] args)
        {
            string dbPath = "db.db";

            using (File.Create(dbPath)) { }

            using (var db = new Db(dbPath))
            {
                db.CreateTable<Service>();
                db.CreateTable<Contract>();


                var query = (
                    from servProj in (
                        from serv in db.GetTable<Service>()
                        // minimal reproduction, makes more sense in real code
                        //select new ServiceProjection { IdContract = serv.IdContract }     // WORKS
                        select ReportExpressions.ToServiceProjection(serv)                  // FAILS
                    )
                    join contract in db.GetTable<Contract>() on servProj.IdContract equals contract.Id
                    select new
                    {
                        Contract = contract,
                        Service = servProj
                    }
                );

                // An unhandled exception of type 'LinqToDB.Linq.LinqException' occurred in linq2db.dll: 'Translation error: 
                // 'Could not compare 'servProj => servProj.IdContract' with contract => contract.Id''
                query.ToList();
            }
        }
    }

    static class ReportExpressions
    {
        [ExpressionMethod(nameof(ToServiceProjectionExpr))]
        public static ServiceProjection ToServiceProjection(Service serv)
            => throw new NotImplementedException();

        static Expression<Func<Service, ServiceProjection>> ToServiceProjectionExpr()
            => (serv) => new ServiceProjection { IdContract = serv.IdContract };
    }
}
